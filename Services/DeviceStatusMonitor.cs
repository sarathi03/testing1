using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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

        public event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;

        public DeviceStatusMonitor()
        {
            _devicesToMonitor = new List<DeviceInfo>();
            _monitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3) // Check every 1 second
            };
            _monitorTimer.Tick += OnMonitorTimerTick;
        }

        public void StartMonitoring()
        {
            _monitorTimer.Start();
        }

        public void StopMonitoring()
        {
            _monitorTimer.Stop();
        }

        public void AddDevice(DeviceInfo device)
        {
            if (device == null || string.IsNullOrEmpty(device.IP)) return;

            lock (_lockObject)
            {
                // Only add if not already present (by reference)
                if (!_devicesToMonitor.Contains(device))
                    _devicesToMonitor.Add(device);
            }
        }

        public void RemoveDevice(DeviceInfo device)
        {
            if (device == null || string.IsNullOrEmpty(device.IP)) return;

            lock (_lockObject)
            {
                _devicesToMonitor.RemoveAll(d => d == device);
            }
        }

        public void ClearDevices()
        {
            lock (_lockObject)
            {
                _devicesToMonitor.Clear();
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
                var newStatus = await CheckDeviceStatusAsync(device.IP);
                if (device.Status != newStatus)
                {
                    var oldStatus = device.Status;
                    device.Status = newStatus;
                    device.LastSeen = DateTime.Now;
                    DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                    {
                        Device = device,
                        OldStatus = oldStatus,
                        NewStatus = newStatus
                    });
                }
            }
        }

        private async Task<DeviceStatus> CheckDeviceStatusAsync(string ipAddress)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(ipAddress, 2000); // 2 second timeout
                    if (reply.Status == IPStatus.Success)
                    {
                        if (await CheckModbusPortsAsync(ipAddress))
                        {
                            return DeviceStatus.Connected; // Green
                        }
                        else
                        {
                            return DeviceStatus.NotConnected; // Red
                        }
                    }
                    else
                    {
                        return DeviceStatus.Offline; // Red
                    }
                }
            }
            catch
            {
                return DeviceStatus.Offline; // Red
            }
        }

        private async Task<bool> CheckModbusPortsAsync(string ipAddress)
        {
            var ports = new[] { 502, 1502 };
            foreach (var port in ports)
            {
                try
                {
                    using (var client = new System.Net.Sockets.TcpClient())
                    {
                        var connectTask = client.ConnectAsync(ipAddress, port);
                        var timeoutTask = Task.Delay(1000); // 1 second timeout
                        var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                        if (completedTask == connectTask && !connectTask.IsFaulted)
                        {
                            return true; // Port is open
                        }
                    }
                }
                catch
                {
                    // Continue to next port
                }
            }
            return false; // No Modbus ports are open
        }
    }

    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public DeviceInfo Device { get; set; }
        public DeviceStatus OldStatus { get; set; }
        public DeviceStatus NewStatus { get; set; }
    }
} 