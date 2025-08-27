using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using testing1.Models;
using testing1.Helpers;

namespace testing1.Services
{
    public class NetModeMonitor
    {
        private readonly DispatcherTimer _netModeTimer;
        private readonly List<DeviceInfo> _devicesToMonitor;
        private readonly object _lockObject = new object();

        // Track devices that need NetMode checking when they come online
        private readonly HashSet<string> _pendingNetModeCheck = new HashSet<string>();

        public event EventHandler<NetModeChangedEventArgs> NetModeChanged;

        public NetModeMonitor()
        {
            _devicesToMonitor = new List<DeviceInfo>();
            _netModeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // Reduced from 5 to 2 seconds for faster response
            };
            _netModeTimer.Tick += OnNetModeTimerTick;
        }

        public void StartMonitoring() => _netModeTimer.Start();
        public void StopMonitoring() => _netModeTimer.Stop();

        public void AddDevice(DeviceInfo device)
        {
            if (device == null || string.IsNullOrEmpty(device.IP)) return;

            lock (_lockObject)
            {
                if (!_devicesToMonitor.Contains(device))
                {
                    _devicesToMonitor.Add(device);

                    // If device is currently connected, check its NetMode immediately
                    if (device.Status == DeviceStatus.Connected)
                    {
                        _pendingNetModeCheck.Add(device.IP);
                    }
                }
            }
        }

        public void RemoveDevice(DeviceInfo device)
        {
            if (device == null || string.IsNullOrEmpty(device.IP)) return;

            lock (_lockObject)
            {
                _devicesToMonitor.RemoveAll(d => d == device);
                _pendingNetModeCheck.Remove(device.IP);
            }
        }

        public void ClearDevices()
        {
            lock (_lockObject)
            {
                _devicesToMonitor.Clear();
                _pendingNetModeCheck.Clear();
            }
        }

        // ✅ UPDATED: Call this method when a device status changes to Connected
        public async void OnDeviceConnected(DeviceInfo device)
        {
            if (device == null || string.IsNullOrEmpty(device.IP)) return;

            lock (_lockObject)
            {
                _pendingNetModeCheck.Add(device.IP);
            }

            // ✅ NEW: Check immediately instead of waiting for timer
            await CheckSingleDeviceNetModeAsync(device);
        }

        // Call this method when a device status changes to Offline
        public void OnDeviceDisconnected(DeviceInfo device)
        {
            if (device == null || string.IsNullOrEmpty(device.IP)) return;

            lock (_lockObject)
            {
                _pendingNetModeCheck.Remove(device.IP);

                // Update the device's connection status to Not Connected
                var deviceToUpdate = _devicesToMonitor.FirstOrDefault(d => d.IP == device.IP);
                if (deviceToUpdate != null)
                {
                    var oldConnectionStatus = deviceToUpdate.ConnectionStatus;
                    deviceToUpdate.ConnectionStatus = ConnectionStatus.NotConnected;

                    NetModeChanged?.Invoke(this, new NetModeChangedEventArgs
                    {
                        Device = deviceToUpdate,
                        OldConnectionStatus = oldConnectionStatus,
                        NewConnectionStatus = ConnectionStatus.NotConnected
                    });
                }
            }
        }

        // ✅ NEW: Immediate single device NetMode check
        private async Task CheckSingleDeviceNetModeAsync(DeviceInfo device)
        {
            try
            {
                // Add a small delay to allow the device network to fully stabilize
                await Task.Delay(1000);

                var netMode = await CheckDeviceNetModeAsync(device.IP);

                lock (_lockObject)
                {
                    var deviceToUpdate = _devicesToMonitor.FirstOrDefault(d => d.IP == device.IP);
                    if (deviceToUpdate != null)
                    {
                        var oldConnectionStatus = deviceToUpdate.ConnectionStatus;
                        deviceToUpdate.ConnectionStatus = netMode;

                        // Remove from pending checks since we got a response
                        _pendingNetModeCheck.Remove(device.IP);

                        // Fire event if connection status changed
                        if (oldConnectionStatus != netMode)
                        {
                            NetModeChanged?.Invoke(this, new NetModeChangedEventArgs
                            {
                                Device = deviceToUpdate,
                                OldConnectionStatus = oldConnectionStatus,
                                NewConnectionStatus = netMode
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking NetMode immediately for {device.IP}: {ex.Message}");
                // Keep in pending list for retry
            }
        }

        private async void OnNetModeTimerTick(object sender, EventArgs e)
        {
            await CheckNetModesAsync();
        }

        private async Task CheckNetModesAsync()
        {
            List<DeviceInfo> devicesToCheck;
            HashSet<string> pendingChecks;

            lock (_lockObject)
            {
                // Only check devices that are connected and have pending NetMode checks
                devicesToCheck = _devicesToMonitor
                    .Where(d => d.Status == DeviceStatus.Connected && _pendingNetModeCheck.Contains(d.IP))
                    .ToList();

                pendingChecks = new HashSet<string>(_pendingNetModeCheck);
            }

            foreach (var device in devicesToCheck)
            {
                try
                {
                    var netMode = await CheckDeviceNetModeAsync(device.IP);

                    lock (_lockObject)
                    {
                        var oldConnectionStatus = device.ConnectionStatus;
                        device.ConnectionStatus = netMode;

                        // Remove from pending checks since we got a response
                        _pendingNetModeCheck.Remove(device.IP);

                        // Fire event if connection status changed
                        if (oldConnectionStatus != netMode)
                        {
                            NetModeChanged?.Invoke(this, new NetModeChangedEventArgs
                            {
                                Device = device,
                                OldConnectionStatus = oldConnectionStatus,
                                NewConnectionStatus = netMode
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't remove from pending checks - we'll try again next time
                    System.Diagnostics.Debug.WriteLine($"Error checking NetMode for {device.IP}: {ex.Message}");
                }
            }
        }

        // ✅ UPDATED: Improved with retry logic
        private async Task<ConnectionStatus> CheckDeviceNetModeAsync(string ipAddress)
        {
            return await Task.Run(() =>
            {
                TcpClientHelper tcpHelper = null;
                try
                {
                    tcpHelper = new TcpClientHelper(ipAddress);

                    // ✅ NEW: Add retry logic for better reliability after network mode change
                    bool connected = false;
                    for (int retry = 0; retry < 3; retry++)
                    {
                        if (tcpHelper.Connect(ipAddress))
                        {
                            connected = true;
                            break;
                        }
                        Task.Delay(500).Wait(); // Wait 500ms between retries
                    }

                    if (!connected)
                    {
                        return ConnectionStatus.NotConnected;
                    }

                    // Send GETGEN command to get NetMode
                    tcpHelper.SendCommand("GETGEN");
                    var data = tcpHelper.ReadResponse(8);

                    if (data.Length >= 8)
                    {
                        int netMode = BitConverter.ToInt32(data, 0);

                        return netMode switch
                        {
                            0 => ConnectionStatus.Ethernet,
                            1 => ConnectionStatus.WiFi,
                            _ => ConnectionStatus.NotConnected
                        };
                    }
                    else
                    {
                        return ConnectionStatus.NotConnected;
                    }
                }
                catch (Exception)
                {
                    return ConnectionStatus.NotConnected;
                }
                finally
                {
                    tcpHelper?.Disconnect();
                }
            });
        }
    }

    public class NetModeChangedEventArgs : EventArgs
    {
        public DeviceInfo Device { get; set; }
        public ConnectionStatus OldConnectionStatus { get; set; }
        public ConnectionStatus NewConnectionStatus { get; set; }
    }
}