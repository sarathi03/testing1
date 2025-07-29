using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using testing1.Commands;
using testing1.Models;
using testing1.Services;

namespace testing1.ViewModels
{
    public class DeviceManagementViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<DeviceManagementModel> _devices;
        private DeviceManagementModel _selectedDevice;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private DeviceStatusMonitor _statusMonitor;

        public ObservableCollection<DeviceManagementModel> Devices
        {
            get => _devices;
            set
            {
                if (_devices != value)
                {
                    _devices = value;
                    OnPropertyChanged();
                }
            }
        }

        public DeviceManagementModel SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    OnPropertyChanged();
                    ((RelayCommand)RemoveDeviceCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)EditDeviceCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FilterDevices();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        // Commands
        public ICommand AddDeviceCommand { get; }
        public ICommand RemoveDeviceCommand { get; }
        public ICommand EditDeviceCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand FilterCommand { get; }

        public DeviceManagementViewModel()
        {
            Devices = new ObservableCollection<DeviceManagementModel>();

            // Initialize commands
            AddDeviceCommand = new RelayCommand(AddNewDevice);
            RemoveDeviceCommand = new RelayCommand(RemoveSelectedDevice, () => SelectedDevice != null);
            EditDeviceCommand = new RelayCommand(EditSelectedDevice, () => SelectedDevice != null);
            RefreshCommand = new RelayCommand(RefreshDevices);
            ClearSearchCommand = new RelayCommand(ClearSearch, () => !string.IsNullOrWhiteSpace(SearchText));
            FilterCommand = new RelayCommand(ShowFilterOptions);

            // Initialize status monitor
            _statusMonitor = new DeviceStatusMonitor();
            _statusMonitor.DeviceStatusChanged += OnDeviceStatusChanged;
            _statusMonitor.StartMonitoring();

            // Load YAML configs from config folder
            LoadYamlConfigs();
        }

        private void ShowFilterOptions()
        {
            // Create filter options menu
            var filterMenu = new System.Windows.Controls.ContextMenu();
            
            // A-Z Name Order
            var aToZItem = new System.Windows.Controls.MenuItem { Header = "A-Z Name Order" };
            aToZItem.Click += (s, e) => FilterByNameAZ();
            filterMenu.Items.Add(aToZItem);
            
            // Z-A Name Order
            var zToAItem = new System.Windows.Controls.MenuItem { Header = "Z-A Name Order" };
            zToAItem.Click += (s, e) => FilterByNameZA();
            filterMenu.Items.Add(zToAItem);
            
            // Group by Location
            var groupByLocationItem = new System.Windows.Controls.MenuItem { Header = "Group by Location" };
            groupByLocationItem.Click += (s, e) => GroupByLocation();
            filterMenu.Items.Add(groupByLocationItem);
            
            // Show online devices only
            var onlineOnlyItem = new System.Windows.Controls.MenuItem { Header = "Online Devices Only" };
            onlineOnlyItem.Click += (s, e) => FilterByStatus(DeviceStatus.Online);
            filterMenu.Items.Add(onlineOnlyItem);
            
            // Show offline devices only
            var offlineOnlyItem = new System.Windows.Controls.MenuItem { Header = "Offline Devices Only" };
            offlineOnlyItem.Click += (s, e) => FilterByStatus(DeviceStatus.Offline);
            filterMenu.Items.Add(offlineOnlyItem);

            // Show the filter menu
            filterMenu.IsOpen = true;
        }

        private void FilterByNameAZ()
        {
            var sortedDevices = Devices.OrderBy(d => d.DeviceInfo.DeviceName).ToList();
            Devices.Clear();
            foreach (var device in sortedDevices)
            {
                Devices.Add(device);
            }
        }

        private void FilterByNameZA()
        {
            var sortedDevices = Devices.OrderByDescending(d => d.DeviceInfo.DeviceName).ToList();
            Devices.Clear();
            foreach (var device in sortedDevices)
            {
                Devices.Add(device);
            }
        }

        private void GroupByLocation()
        {
            var groupedDevices = Devices.OrderBy(d => d.DeviceInfo.Location).ThenBy(d => d.DeviceInfo.DeviceName).ToList();
            Devices.Clear();
            foreach (var device in groupedDevices)
            {
                Devices.Add(device);
            }
        }

        private void FilterByStatus(DeviceStatus status)
        {
            var filteredDevices = Devices.Where(d => d.DeviceInfo.Status == status).ToList();
            Devices.Clear();
            foreach (var device in filteredDevices)
            {
                Devices.Add(device);
            }
        }

        // Load YAML configs from config folder
        private void LoadYamlConfigs()
        {
            try
            {
                var configManager = new DeviceConfigManager();
                var configs = configManager.LoadAllDeviceConfigs();

                foreach (var config in configs)
                {
                    if (config.DeviceInfo != null)
                    {
                        var deviceModel = new DeviceManagementModel(config.DeviceInfo);
                        deviceModel.DeviceNumber = config.DeviceNumber;
                        Devices.Add(deviceModel);

                        // Add to status monitor
                        _statusMonitor.AddDevice(deviceModel.DeviceInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading YAML configs: {ex.Message}");
            }
        }

        // Add the missing AddDevice method that accepts DeviceManagementModel
        public void AddDevice(DeviceManagementModel deviceModel)
        {
            if (deviceModel == null) return;

            // Check if device already exists
            var existingDevice = Devices.FirstOrDefault(d =>
                d.DeviceInfo?.IP == deviceModel.DeviceInfo?.IP &&
                d.DeviceInfo?.Port == deviceModel.DeviceInfo?.Port);

            if (existingDevice != null)
            {
                // Update existing device
                existingDevice.DeviceInfo = deviceModel.DeviceInfo;
                existingDevice.UpdateLastModified();
                return;
            }

            // Set device number
            deviceModel.DeviceNumber = Devices.Count + 1;
            Devices.Add(deviceModel);

            // Add to status monitor
            if (deviceModel.DeviceInfo != null)
            {
                _statusMonitor.AddDevice(deviceModel.DeviceInfo);
            }
        }

        // Add the missing AddDevice method that accepts DeviceInfo
        public void AddDevice(DeviceInfo deviceInfo)
        {
            if (deviceInfo == null) return;

            var managementModel = new DeviceManagementModel(deviceInfo);
            AddDevice(managementModel);
        }

        // Add the missing AddSelectedDeviceAsync method
        public async Task AddSelectedDeviceAsync(DeviceInfo deviceInfo)
        {
            IsLoading = true;
            try
            {
                await Task.Run(() => AddDevice(deviceInfo));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddNewDevice()
        {
            // Don't add dummy devices anymore - just trigger navigation
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var deviceManagementView = System.Windows.Application.Current.MainWindow?.Content as Views.DeviceManagementView;
                deviceManagementView?.NavigateToDiscoveryView();
            });
        }

        private void RemoveSelectedDevice()
        {
            if (SelectedDevice != null)
            {
                // Remove from status monitor
                if (SelectedDevice.DeviceInfo != null)
                {
                    _statusMonitor.RemoveDevice(SelectedDevice.DeviceInfo);
                }

                // Remove config file using EnhancedDeviceConfigManager
                if (SelectedDevice.DeviceInfo != null)
                {
                    var configManager = new testing1.Services.EnhancedDeviceConfigManager();
                    configManager.DeleteDeviceConfig(SelectedDevice.DeviceInfo);
                }

                Devices.Remove(SelectedDevice);
                SelectedDevice = null;
            }
        }

        private void EditSelectedDevice()
        {
            if (SelectedDevice != null)
            {
                // Simple configuration - just update the last modified time
                SelectedDevice.UpdateLastModified();
                System.Windows.MessageBox.Show($"Device '{SelectedDevice.DeviceInfo.DeviceName}' configuration opened!", "Configure Device");
            }
        }

        private void RefreshDevices()
        {
            IsLoading = true;

            // Simulate refresh operation
            Task.Run(async () =>
            {
                await Task.Delay(1000); // Simulate network operation

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var device in Devices)
                    {
                        device.UpdateLastModified();
                        // Randomly update some device statuses
                        var random = new Random();
                        if (random.Next(0, 3) == 0)
                        {
                            device.DeviceInfo.Status = (DeviceStatus)random.Next(0, 4);
                            device.DeviceInfo.LastSeen = DateTime.Now;
                        }
                    }
                    IsLoading = false;
                });
            });
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private void FilterDevices()
        {
            // Implementation for filtering devices based on search text
            // For now, this is just a placeholder
            // In a real implementation, you would filter the collection view
        }

        private void LoadSampleData()
        {
            var sampleDevices = new[]
            {
                new DeviceInfo
                {
                    DeviceName = "Router-Main",
                    IP = "192.168.1.1",
                    Port = 80,
                    MAC = "AA:BB:CC:DD:EE:FF",
                    Status = DeviceStatus.Online,
                    Location = "Server Room",
                    LastSeen = DateTime.Now.AddMinutes(-5)
                },
                new DeviceInfo
                {
                    DeviceName = "Switch-01",
                    IP = "192.168.1.2",
                    Port = 23,
                    MAC = "11:22:33:44:55:66",
                    Status = DeviceStatus.Offline,
                    Location = "Network Closet",
                    LastSeen = DateTime.Now.AddHours(-2)
                },
                new DeviceInfo
                {
                    DeviceName = "Access Point",
                    IP = "192.168.1.10",
                    Port = 443,
                    MAC = "99:88:77:66:55:44",
                    Status = DeviceStatus.Online,
                    Location = "Conference Room",
                    LastSeen = DateTime.Now.AddMinutes(-1)
                },
                new DeviceInfo
                {
                    DeviceName = "Printer-HP",
                    IP = "192.168.1.20",
                    Port = 9100,
                    MAC = "12:34:56:78:90:AB",
                    Status = DeviceStatus.Connecting,
                    Location = "Office Floor 1",
                    LastSeen = DateTime.Now.AddMinutes(-30)
                }
            };

            foreach (var deviceInfo in sampleDevices)
            {
                AddDevice(deviceInfo);
            }
        }

        private string GenerateRandomMAC()
        {
            var random = new Random();
            var mac = new byte[6];
            random.NextBytes(mac);
            return string.Join(":", mac.Select(b => b.ToString("X2")));
        }

        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            // Update the device status in the UI
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                var deviceModel = Devices.FirstOrDefault(d => d.DeviceInfo?.IP == e.Device.IP);
                if (deviceModel != null)
                {
                    deviceModel.DeviceInfo.Status = e.NewStatus;
                    deviceModel.DeviceInfo.LastSeen = e.Device.LastSeen;
                }
            });
        }

        public void Dispose()
        {
            if (_statusMonitor != null)
            {
                _statusMonitor.StopMonitoring();
                _statusMonitor.DeviceStatusChanged -= OnDeviceStatusChanged;
                _statusMonitor = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}