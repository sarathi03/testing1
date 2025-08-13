using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using testing1.Models;

namespace testing1.Services
{
    public class DeviceStatusMonitor
    {
        private readonly DispatcherTimer _monitorTimer;
        private readonly ConcurrentBag<DeviceInfo> _devicesToMonitor;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrency;

        public event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;

        public DeviceStatusMonitor(int maxConcurrency = 50)
        {
            _maxConcurrency = maxConcurrency;
            _devicesToMonitor = new ConcurrentBag<DeviceInfo>();
            _semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);

            _monitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Ping check every 1s
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

            // Check if device already exists to avoid duplicates
            var existingDevice = _devicesToMonitor.FirstOrDefault(d => d.IP == device.IP);
            if (existingDevice == null)
            {
                _devicesToMonitor.Add(device);
            }
        }

        public void RemoveDevice(DeviceInfo device)
        {
            if (device == null || string.IsNullOrEmpty(device.IP)) return;

            // Convert to list, filter, and recreate the bag
            var remainingDevices = _devicesToMonitor.Where(d => d.IP != device.IP).ToList();

            // Clear and repopulate (ConcurrentBag doesn't have direct remove)
            while (_devicesToMonitor.TryTake(out _)) { }

            foreach (var remainingDevice in remainingDevices)
            {
                _devicesToMonitor.Add(remainingDevice);
            }
        }

        public void ClearDevices()
        {
            while (_devicesToMonitor.TryTake(out _)) { }
        }

        private async void OnMonitorTimerTick(object sender, EventArgs e)
        {
            // Stop the timer to prevent overlapping checks
            _monitorTimer.Stop();

            try
            {
                await CheckDeviceStatusesAsync();
            }
            finally
            {
                // Restart the timer
                _monitorTimer.Start();
            }
        }

        private async Task CheckDeviceStatusesAsync()
        {
            var devicesToCheck = _devicesToMonitor.ToList();

            if (!devicesToCheck.Any()) return;

            // Process devices in parallel with controlled concurrency
            var tasks = devicesToCheck.Select(device => CheckSingleDeviceAsync(device));

            await Task.WhenAll(tasks);
        }

        private async Task CheckSingleDeviceAsync(DeviceInfo device)
        {
            await _semaphore.WaitAsync();
            try
            {
                var newStatus = await CheckDeviceStatusAsync(device.IP);

                if (device.Status != newStatus)
                {
                    var oldStatus = device.Status;
                    device.Status = newStatus;
                    device.LastSeen = DateTime.Now;

                    // Invoke on UI thread if needed
                    if (DeviceStatusChanged != null)
                    {
                        // Use Dispatcher.BeginInvoke to ensure thread safety for UI updates
                        if (System.Windows.Application.Current?.Dispatcher != null)
                        {
                            await System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                                {
                                    Device = device,
                                    OldStatus = oldStatus,
                                    NewStatus = newStatus
                                });
                            }));
                        }
                        else
                        {
                            DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                            {
                                Device = device,
                                OldStatus = oldStatus,
                                NewStatus = newStatus
                            });
                        }
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // ✅ Optimized IP Ping check with shorter timeout for faster results
        private async Task<DeviceStatus> CheckDeviceStatusAsync(string ipAddress)
        {
            try
            {
                using (var ping = new Ping())
                {
                    // Reduced timeout from 1000ms to 500ms for faster response
                    var reply = await ping.SendPingAsync(ipAddress, 500);
                    return reply.Status == IPStatus.Success ? DeviceStatus.Connected : DeviceStatus.Offline;
                }
            }
            catch
            {
                return DeviceStatus.Offline;
            }
        }

        // ✅ Discovery logic with parallel processing for multiple devices
        public async Task<Dictionary<string, bool>> CheckMultipleModbusDevicesAsync(IEnumerable<string> ipAddresses)
        {
            var results = new ConcurrentDictionary<string, bool>();
            var tasks = ipAddresses.Select(async ip =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    var isModbus = await IsModbusDeviceOn502Async(ip);
                    results[ip] = isModbus;
                }
                finally
                {
                    _semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return new Dictionary<string, bool>(results);
        }

        // ✅ Discovery logic — to be used *once* before monitoring
        public async Task<bool> IsModbusDeviceOn502Async(string ipAddress)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    // Set a reasonable timeout for connection attempts
                    var connectTask = client.ConnectAsync(ipAddress, 502);
                    var timeoutTask = Task.Delay(2000); // 2 second timeout

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == connectTask && !connectTask.IsFaulted)
                    {
                        return true; // Connection success
                    }
                    else
                    {
                        return false; // Timeout or connection failed
                    }
                }
            }
            catch
            {
                return false; // Port 502 not open
            }
        }

        // ✅ Cleanup method to dispose resources
        public void Dispose()
        {
            _monitorTimer?.Stop();
            _semaphore?.Dispose();
        }
    }

    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public DeviceInfo Device { get; set; }
        public DeviceStatus OldStatus { get; set; }
        public DeviceStatus NewStatus { get; set; }
    }
}