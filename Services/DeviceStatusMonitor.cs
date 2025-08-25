using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Threading;
using testing1.Models;

namespace testing1.Services
{
    public class DeviceStatusMonitor
    {
        private readonly DispatcherTimer _monitorTimer;
        private readonly List<DeviceInfo> _devicesToMonitor;
        private readonly object _lockObject = new object();

        // ✅ Tracks consecutive failures to avoid instant false "Offline"
        private readonly Dictionary<string, int> _failureCounts = new Dictionary<string, int>();
        private const int FailureThreshold = 2; // Number of failed pings before marking Offline

        public event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;

        public DeviceStatusMonitor()
        {
            _devicesToMonitor = new List<DeviceInfo>();
            _monitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Ping check every 1s
            };
            _monitorTimer.Tick += OnMonitorTimerTick;
        }

        public void StartMonitoring() => _monitorTimer.Start();
        public void StopMonitoring() => _monitorTimer.Stop();

        public void AddDevice(DeviceInfo device)
        {
            if (device == null || string.IsNullOrEmpty(device.IP)) return;

            lock (_lockObject)
            {
                if (!_devicesToMonitor.Contains(device))
                    _devicesToMonitor.Add(device);

                if (!_failureCounts.ContainsKey(device.IP))
                    _failureCounts[device.IP] = 0;
            }
        }

        public void RemoveDevice(DeviceInfo device)
        {
            if (device == null || string.IsNullOrEmpty(device.IP)) return;

            lock (_lockObject)
            {
                _devicesToMonitor.RemoveAll(d => d == device);
                _failureCounts.Remove(device.IP);
            }
        }

        public void ClearDevices()
        {
            lock (_lockObject)
            {
                _devicesToMonitor.Clear();
                _failureCounts.Clear();
            }
        }

        private async void OnMonitorTimerTick(object sender, EventArgs e)
        {
            await CheckDeviceStatusesAsync();
        }

        private async Task CheckDeviceStatusesAsync()
        {
            List<DeviceInfo> devicesToCheck;
            lock (_lockObject)
            {
                devicesToCheck = _devicesToMonitor.ToList();
            }

            foreach (var device in devicesToCheck)
            {
                // Check if device still exists in our monitoring list (in case it was removed)
                bool deviceStillExists;
                lock (_lockObject)
                {
                    deviceStillExists = _devicesToMonitor.Contains(device) && _failureCounts.ContainsKey(device.IP);
                }

                if (!deviceStillExists)
                {
                    // Device was removed during monitoring, skip it
                    continue;
                }

                bool pingSuccess = await CheckDeviceStatusAsync(device.IP) == DeviceStatus.Connected;

                lock (_lockObject)
                {
                    // Double-check device still exists after async operation
                    if (!_devicesToMonitor.Contains(device) || !_failureCounts.ContainsKey(device.IP))
                    {
                        continue; // Device was removed during ping, skip
                    }

                    if (pingSuccess)
                    {
                        _failureCounts[device.IP] = 0; // reset failure count
                        if (device.Status != DeviceStatus.Connected)
                        {
                            var oldStatus = device.Status;
                            device.Status = DeviceStatus.Connected;
                            device.LastSeen = DateTime.Now;

                            DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                            {
                                Device = device,
                                OldStatus = oldStatus,
                                NewStatus = DeviceStatus.Connected
                            });
                        }
                    }
                    else
                    {
                        // Safely increment failure count
                        if (_failureCounts.ContainsKey(device.IP))
                        {
                            _failureCounts[device.IP]++;

                            // Change to Offline only if failures cross threshold
                            if (_failureCounts[device.IP] >= FailureThreshold &&
                                device.Status != DeviceStatus.Offline)
                            {
                                var oldStatus = device.Status;
                                device.Status = DeviceStatus.Offline;
                                device.LastSeen = DateTime.Now;

                                DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                                {
                                    Device = device,
                                    OldStatus = oldStatus,
                                    NewStatus = DeviceStatus.Offline
                                });
                            }
                        }
                    }
                }
            }
        }

        private async Task<DeviceStatus> CheckDeviceStatusAsync(string ipAddress)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(ipAddress, 1000);
                    return reply.Status == IPStatus.Success ? DeviceStatus.Connected : DeviceStatus.Offline;
                }
            }
            catch
            {
                return DeviceStatus.Offline;
            }
        }

        public async Task<bool> IsModbusDeviceOn502Async(string ipAddress)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(ipAddress, 502);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public DeviceInfo Device { get; set; }
        public DeviceStatus OldStatus { get; set; }
        public DeviceStatus NewStatus { get; set; }
    }
}