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

        public event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;

        public DeviceStatusMonitor()
        {
            _devicesToMonitor = new List<DeviceInfo>();
            _monitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Ping check every 3s
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

        // ✅ Pure IP Ping check only
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

        // ✅ Discovery logic — to be used *once* before monitoring
        public async Task<bool> IsModbusDeviceOn502Async(string ipAddress)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(ipAddress, 502);
                    return true; // Connection success
                }
            }
            catch
            {
                return false; // Port 502 not open
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
