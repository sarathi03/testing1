using testing1;
using testing1.Helper;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using testing1.Commands;
using System.Windows.Media;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Threading;
using System;
using System.Collections.Generic;
using testing1.Models;
using testing1.Services;

namespace testing1.ViewModels
{

    public class DeviceModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _ipAddress;
        private string _port;
        private string _macAddress;
        private Brush _statusColor;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string IPAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    OnPropertyChanged(nameof(IPAddress));
                }
            }
        }

        public string Port
        {
            get => _port;
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChanged(nameof(Port));
                }
            }
        }

        public string MacAddress
        {
            get => _macAddress;
            set
            {
                if (_macAddress != value)
                {
                    _macAddress = value;
                    OnPropertyChanged(nameof(MacAddress));
                }
            }
        }

        public Brush StatusColor
        {
            get => _statusColor;
            set
            {
                if (_statusColor != value)
                {
                    _statusColor = value;
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    public class DiscoveryViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _networkMonitorTimer;
        private DeviceConfigManager _configManager;

        // New properties for device table
        public ObservableCollection<DeviceModel> AvailableDevices { get; set; } = new ObservableCollection<DeviceModel>();

        private bool _selectAllDevices;
        public bool SelectAllDevices
        {
            get => _selectAllDevices;
            set
            {
                if (_selectAllDevices != value)
                {
                    _selectAllDevices = value;
                    OnPropertyChanged(nameof(SelectAllDevices));
                    ToggleAllDevices(value);
                }
            }
        }

        // Scanning state properties
        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (_isScanning != value)
                {
                    _isScanning = value;
                    OnPropertyChanged(nameof(IsScanning));
                    // Refresh command can execute state
                    ((RelayCommand)ScanSubnetCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private string _scanProgressText;
        public string ScanProgressText
        {
            get => _scanProgressText;
            set
            {
                if (_scanProgressText != value)
                {
                    _scanProgressText = value;
                    OnPropertyChanged(nameof(ScanProgressText));
                }
            }
        }

        // Network info properties
        private string _currentIP;
        public string CurrentIP
        {
            get => _currentIP;
            set
            {
                if (_currentIP != value)
                {
                    _currentIP = value;
                    OnPropertyChanged(nameof(CurrentIP));
                }
            }
        }

        private string _gateway;
        public string Gateway
        {
            get => _gateway;
            set
            {
                if (_gateway != value)
                {
                    _gateway = value;
                    OnPropertyChanged(nameof(Gateway));
                }
            }
        }

        // Static IP and Subnet properties (these won't change during operations)
        private string _ethernetIP;
        public string EthernetIP
        {
            get => _ethernetIP;
            set
            {
                if (_ethernetIP != value)
                {
                    _ethernetIP = value;
                    OnPropertyChanged(nameof(EthernetIP));
                }
            }
        }

        private string _ethernetSubnet;
        public string EthernetSubnet
        {
            get => _ethernetSubnet;
            set
            {
                if (_ethernetSubnet != value)
                {
                    _ethernetSubnet = value;
                    OnPropertyChanged(nameof(EthernetSubnet));
                }
            }
        }

        private string _wiFiIP;
        public string WiFiIP
        {
            get => _wiFiIP;
            set
            {
                if (_wiFiIP != value)
                {
                    _wiFiIP = value;
                    OnPropertyChanged(nameof(WiFiIP));
                }
            }
        }

        private string _wiFiSubnet;
        public string WiFiSubnet
        {
            get => _wiFiSubnet;
            set
            {
                if (_wiFiSubnet != value)
                {
                    _wiFiSubnet = value;
                    OnPropertyChanged(nameof(WiFiSubnet));
                }
            }
        }

        // Existing properties (can still be used for status messages)
        private string _infoText;
        public string InfoText
        {
            get => _infoText;
            set
            {
                if (_infoText != value)
                {
                    _infoText = value;
                    OnPropertyChanged(nameof(InfoText));
                }
            }
        }

        // Commands (removed PingDeviceCommand)
        public ICommand ScanSubnetCommand { get; }
        public ICommand AddAllDevicesCommand { get; }
        public ICommand AddSelectedDevicesCommand { get; }
        public ICommand OpenConfigDirectoryCommand { get; }

        public DiscoveryViewModel()
        {
            _configManager = new DeviceConfigManager();

            ScanSubnetCommand = new RelayCommand(async () => await ScanSubnetAsync(), () => !IsScanning);
            AddAllDevicesCommand = new RelayCommand(AddAllDevices);
            AddSelectedDevicesCommand = new RelayCommand(AddSelectedDevices);
            OpenConfigDirectoryCommand = new RelayCommand(() => _configManager.OpenConfigDirectory());

            InitializeNetworkInfo();
            StartNetworkMonitoring();
        }

        private void StartNetworkMonitoring()
        {
            // Subscribe to network change events
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;

            // Also use a timer for additional monitoring (every 3 seconds)
            _networkMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _networkMonitorTimer.Tick += OnNetworkMonitorTimer_Tick;
            _networkMonitorTimer.Start();
        }

        private void OnNetworkAddressChanged(object sender, EventArgs e)
        {
            // This event fires when network addresses change
            System.Diagnostics.Debug.WriteLine("Network address changed detected");
            RefreshNetworkInfo();
        }

        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            // This event fires when network availability changes
            System.Diagnostics.Debug.WriteLine($"Network availability changed: {e.IsAvailable}");
            RefreshNetworkInfo();
        }

        private void OnNetworkMonitorTimer_Tick(object sender, EventArgs e)
        {
            // Periodic check every 3 seconds for real-time updates
            RefreshNetworkInfo();
        }

        private void RefreshNetworkInfo()
        {
            try
            {
                // Run network info update on UI thread
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    InitializeNetworkInfo();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing network info: {ex.Message}");
            }
        }

        private void InitializeNetworkInfo()
        {
            var subnets = NetworkHelper.GetAllLocalSubnets();

            // Get all network interfaces (including disconnected ones)
            var allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces().ToList();
            var activeNetworkInterfaces = allNetworkInterfaces
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .ToList();

            // Initialize default values
            EthernetIP = "No Connection";
            EthernetSubnet = "No Connection";
            WiFiIP = "No Connection";
            WiFiSubnet = "No Connection";

            // Check if we have any Ethernet or WiFi interfaces available (even if disconnected)
            bool hasEthernetInterface = allNetworkInterfaces.Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet);
            bool hasWiFiInterface = allNetworkInterfaces.Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);

            if (subnets.Any())
            {
                var firstSubnet = subnets.First();
                CurrentIP = firstSubnet.ip;
                // Assuming gateway is typically .1 in the subnet
                var ipParts = firstSubnet.ip.Split('.');
                Gateway = $"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}.1";

                // Simple separation: Check each subnet against active network interfaces only
                foreach (var subnet in subnets)
                {
                    var matchingInterface = activeNetworkInterfaces.FirstOrDefault(ni =>
                        ni.GetIPProperties().UnicastAddresses.Any(ua =>
                            ua.Address.ToString() == subnet.ip));

                    if (matchingInterface != null)
                    {
                        // Simple check: WiFi if it's Wireless, Ethernet if it's Ethernet
                        if (matchingInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        {
                            WiFiIP = subnet.ip;
                            WiFiSubnet = subnet.subnet;
                        }
                        else if (matchingInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                        {
                            EthernetIP = subnet.ip;
                            EthernetSubnet = subnet.subnet;
                        }
                    }
                }
            }

            // Update status messages based on interface availability and connection status
            if (!hasEthernetInterface)
            {
                EthernetIP = "No Ethernet Adapter";
                EthernetSubnet = "No Ethernet Adapter";
            }
            else if (EthernetIP == "No Connection")
            {
                // Check if Ethernet adapter exists but is disconnected
                var ethernetInterface = allNetworkInterfaces.FirstOrDefault(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet);
                if (ethernetInterface != null && ethernetInterface.OperationalStatus != OperationalStatus.Up)
                {
                    EthernetIP = "Cable Disconnected";
                    EthernetSubnet = "Cable Disconnected";
                }
                else
                {
                    EthernetIP = "Not Connected";
                    EthernetSubnet = "Not Connected";
                }
            }

            if (!hasWiFiInterface)
            {
                WiFiIP = "No WiFi Adapter";
                WiFiSubnet = "No WiFi Adapter";
            }
            else if (WiFiIP == "No Connection")
            {
                // Check if WiFi adapter exists but is disconnected
                var wifiInterface = allNetworkInterfaces.FirstOrDefault(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
                if (wifiInterface != null && wifiInterface.OperationalStatus != OperationalStatus.Up)
                {
                    WiFiIP = "WiFi Disconnected";
                    WiFiSubnet = "WiFi Disconnected";
                }
                else
                {
                    WiFiIP = "Not Connected";
                    WiFiSubnet = "Not Connected";
                }
            }

            InfoText = string.Join("\n", subnets.Select(s => $"IP: {s.ip}, Subnet: {s.subnet}"));
        }

        private async Task ScanSubnetAsync()
        {
            try
            {
                // Set scanning state
                IsScanning = true;
                ScanProgressText = "Initializing scan...";
                AvailableDevices.Clear();

                var subnets = NetworkHelper.GetAllLocalSubnets();
                var ports = new List<int> { 1502 };
                var tasks = new List<Task>();

                // Calculate total IPs to scan for progress tracking
                int totalIPs = 0;
                foreach (var (localIp, subnet) in subnets)
                {
                    var ips = NetworkHelper.GetAllIPsInSubnet(localIp, subnet)
                        .Where(ip => ip.ToString() != localIp && !ip.ToString().EndsWith(".1"));
                    totalIPs += ips.Count();
                }

                ScanProgressText = $"Scanning {totalIPs} IP addresses...";

                int scannedCount = 0;
                int foundDevicesCount = 0;

                foreach (var (localIp, subnet) in subnets)
                {
                    var ips = NetworkHelper.GetAllIPsInSubnet(localIp, subnet)
                        .Where(ip => ip.ToString() != localIp && !ip.ToString().EndsWith(".1"));

                    foreach (var ip in ips)
                    {
                        string ipStr = ip.ToString();
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                var openPorts = await PortScannerHelper.ScanOpenPortsAsync(ipStr, ports);

                                // Update progress on UI thread
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    scannedCount++;
                                    ScanProgressText = $"Scanned {scannedCount}/{totalIPs} addresses - Found {foundDevicesCount} devices";
                                });

                                // ONLY add devices that have open ports 502 or 1502
                                if (openPorts.Any())
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        var device = new DeviceModel
                                        {
                                            IPAddress = ipStr,
                                            IsSelected = false,
                                            StatusColor = Brushes.Green
                                        };

                                        // Set port information
                                        device.Port = string.Join(", ", openPorts);

                                        // Get MAC address for devices with open ports 502 or 1502
                                        try
                                        {
                                            device.MacAddress = NetworkHelper.GetMacAddressFromIp(ipStr);
                                        }
                                        catch
                                        {
                                            device.MacAddress = "Unknown";
                                        }

                                        AvailableDevices.Add(device);
                                        foundDevicesCount++;
                                        ScanProgressText = $"Scanned {scannedCount}/{totalIPs} addresses - Found {foundDevicesCount} devices";
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                // Handle individual IP scan errors
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    scannedCount++;
                                    ScanProgressText = $"Scanned {scannedCount}/{totalIPs} addresses - Found {foundDevicesCount} devices";
                                });
                                System.Diagnostics.Debug.WriteLine($"Error scanning {ipStr}: {ex.Message}");
                            }
                        }));
                    }
                }

                await Task.WhenAll(tasks);

                // Final progress update
                ScanProgressText = $"Scan completed! Found {foundDevicesCount} devices with open ports 1502";

                // Show completion message briefly
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                // Handle errors without affecting the static IP/Subnet display
                ScanProgressText = $"Scan failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error during subnet scan: {ex.Message}");

                // Show error message briefly
                await Task.Delay(3000);
            }
            finally
            {
                // Reset scanning state
                IsScanning = false;
                ScanProgressText = "";
            }
        }

        private void ToggleAllDevices(bool selectAll)
        {
            foreach (var device in AvailableDevices)
            {
                device.IsSelected = selectAll;
            }
        }

        private void AddAllDevices()
        {
            var allDevices = AvailableDevices.Select(ConvertToDeviceInfo).ToList();

            if (allDevices.Count == 0)
            {
                System.Windows.MessageBox.Show("No devices available to save.", "Information",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            // Check for duplicates
            var existingConfigs = _configManager.LoadAllDeviceConfigs();
            var alreadyAdded = allDevices.Where(d => existingConfigs.Any(cfg => cfg.DeviceInfo?.MAC == d.MAC)).ToList();
            if (alreadyAdded.Any())
            {
                string macs = string.Join(", ", alreadyAdded.Select(d => d.MAC));
                System.Windows.MessageBox.Show($"The following devices are already added and will not be added again: {macs}", "Duplicate Devices", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                allDevices = allDevices.Where(d => !alreadyAdded.Contains(d)).ToList();
            }
            if (allDevices.Count == 0) return;

            bool success = _configManager.SaveMultipleDeviceConfigs(allDevices);

            System.Diagnostics.Debug.WriteLine($"Adding {allDevices.Count} devices... Success: {success}");
        }

        private void AddSelectedDevices()
        {
            var selectedDevices = AvailableDevices.Where(d => d.IsSelected)
                                                 .Select(ConvertToDeviceInfo)
                                                 .ToList();

            if (selectedDevices.Count == 0)
            {
                System.Windows.MessageBox.Show("No devices selected. Please select devices to save.", "Information",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            // Check for duplicates
            var existingConfigs = _configManager.LoadAllDeviceConfigs();
            var alreadyAdded = selectedDevices.Where(d => existingConfigs.Any(cfg => cfg.DeviceInfo?.MAC == d.MAC)).ToList();
            if (alreadyAdded.Any())
            {
                string macs = string.Join(", ", alreadyAdded.Select(d => d.MAC));
                System.Windows.MessageBox.Show($"The following devices are already added and will not be added again: {macs}", "Duplicate Devices", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                selectedDevices = selectedDevices.Where(d => !alreadyAdded.Contains(d)).ToList();
            }
            if (selectedDevices.Count == 0) return;

            bool success = _configManager.SaveMultipleDeviceConfigs(selectedDevices);

            System.Diagnostics.Debug.WriteLine($"Adding {selectedDevices.Count} selected devices... Success: {success}");
        }

        private DeviceInfo ConvertToDeviceInfo(DeviceModel deviceModel)
        {
            return new DeviceInfo
            {
                IP = deviceModel.IPAddress,
                MAC = deviceModel.MacAddress ?? "Unknown",
                DeviceName = $"Device_{deviceModel.IPAddress.Replace(".", "_")}",
                Status = DeviceStatus.Discovered
            };
        }

        // Clean up resources when ViewModel is disposed
        public void Dispose()
        {
            // Unsubscribe from network events
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;

            // Stop and dispose timer
            _networkMonitorTimer?.Stop();
            _networkMonitorTimer = null;
        }

        // Properly implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal string GetLocalIPAddress()
        {
            throw new NotImplementedException();
        }
    }

}