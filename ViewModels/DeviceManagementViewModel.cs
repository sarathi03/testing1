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
    public class DeviceManagementViewModel : INotifyPropertyChanged, IDisposable
    {
        private ObservableCollection<DeviceManagementModel> _devices;
        private ObservableCollection<DeviceManagementModel> _filteredDevices;
        private DeviceManagementModel _selectedDevice;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private DeviceStatusMonitor _statusMonitor;
        private NetModeMonitor _netModeMonitor;
        private YamlFileManager _configManager;

        public ObservableCollection<DeviceManagementModel> Devices
        {
            get => _devices;
            set
            {
                if (_devices != value)
                {
                    // Unsubscribe from old collection
                    if (_devices != null)
                    {
                        foreach (var device in _devices)
                        {
                            UnsubscribeFromDeviceChanges(device);
                        }
                    }

                    _devices = value;

                    // Subscribe to new collection
                    if (_devices != null)
                    {
                        foreach (var device in _devices)
                        {
                            SubscribeToDeviceChanges(device);
                        }
                    }

                    OnPropertyChanged();
                    UpdateDeviceCounts();
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

        // Device count properties for status bar
        public int OnlineDevicesCount => Devices?.Count(d => d.DeviceInfo?.Status == DeviceStatus.Online || d.DeviceInfo?.Status == DeviceStatus.Connected) ?? 0;
        public int OfflineDevicesCount => Devices?.Count(d => d.DeviceInfo?.Status == DeviceStatus.Offline || d.DeviceInfo?.Status == DeviceStatus.NotConnected) ?? 0;

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
            _configManager = new YamlFileManager();

            // Initialize commands
            AddDeviceCommand = new RelayCommand(AddNewDevice);
            RemoveDeviceCommand = new RelayCommand(RemoveSelectedDevice, () => SelectedDevice != null);
            EditDeviceCommand = new RelayCommand(EditSelectedDevice);
            RefreshCommand = new RelayCommand(RefreshDevices);
            ClearSearchCommand = new RelayCommand(ClearSearch, () => !string.IsNullOrWhiteSpace(SearchText));
            FilterCommand = new RelayCommand(ShowFilterOptions);

            // Initialize status monitor
            _statusMonitor = new DeviceStatusMonitor();
            _statusMonitor.DeviceStatusChanged += OnDeviceStatusChanged;
            _statusMonitor.StartMonitoring();

            // Initialize NetMode monitor
            _netModeMonitor = new NetModeMonitor();
            _netModeMonitor.NetModeChanged += OnNetModeChanged;
            _netModeMonitor.StartMonitoring();

            // Load YAML configs from config folder
            LoadYamlConfigs();
        }

        // NetMode changed event handler
        private void OnNetModeChanged(object sender, NetModeChangedEventArgs e)
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                var deviceModel = Devices.FirstOrDefault(d => d.DeviceInfo?.IP == e.Device.IP);
                if (deviceModel != null)
                {
                    deviceModel.DeviceInfo.ConnectionStatus = e.NewConnectionStatus;
                    System.Diagnostics.Debug.WriteLine($"NetMode updated for {e.Device.IP}: {e.NewConnectionStatus}");
                }
            });
        }

        // Subscribe to device property changes for auto-save
        private void SubscribeToDeviceChanges(DeviceManagementModel device)
        {
            if (device?.DeviceInfo != null)
            {
                device.DeviceInfo.PropertyChanged += OnDeviceInfoPropertyChanged;
            }
        }

        // Unsubscribe from device property changes
        private void UnsubscribeFromDeviceChanges(DeviceManagementModel device)
        {
            if (device?.DeviceInfo != null)
            {
                device.DeviceInfo.PropertyChanged -= OnDeviceInfoPropertyChanged;
            }
        }

        // Handle device info property changes and auto-save
        private void OnDeviceInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is DeviceInfo deviceInfo)
            {
                // Only save for DeviceName and Location changes
                if (e.PropertyName == nameof(DeviceInfo.DeviceName) ||
                    e.PropertyName == nameof(DeviceInfo.Location))
                {
                    // Find the corresponding DeviceManagementModel
                    var deviceModel = Devices.FirstOrDefault(d => d.DeviceInfo == deviceInfo);
                    if (deviceModel != null)
                    {
                        // Update the last modified time
                        deviceModel.UpdateLastModified();

                        // Save to YAML file
                        Task.Run(() => SaveDeviceConfigAsync(deviceModel));
                    }
                }

                // Update device counts when status changes
                if (e.PropertyName == nameof(DeviceInfo.Status))
                {
                    UpdateDeviceCounts();
                }
            }
        }

        // Update device count properties
        private void UpdateDeviceCounts()
        {
            OnPropertyChanged(nameof(OnlineDevicesCount));
            OnPropertyChanged(nameof(OfflineDevicesCount));
        }

        // Async method to save device config without blocking UI
        private async Task SaveDeviceConfigAsync(DeviceManagementModel deviceModel)
        {
            try
            {
                await Task.Run(() =>
                {
                    bool success = _configManager.SaveDeviceManagementModel(deviceModel);

                    // Optional: Show notification on UI thread
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        if (success)
                        {
                            System.Diagnostics.Debug.WriteLine($"Auto-saved config for device: {deviceModel.DeviceInfo.DeviceName}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to auto-save config for device: {deviceModel.DeviceInfo.DeviceName}");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error auto-saving device config: {ex.Message}");
            }
        }

        private void ShowFilterOptions()
        {
            // Create filter options menu
            var filterMenu = new System.Windows.Controls.ContextMenu();

            // A-Z Name Order
            var aToZItem = new System.Windows.Controls.MenuItem { Header = "📝 A-Z Name Order" };
            aToZItem.Click += (s, e) => FilterByNameAZ();
            filterMenu.Items.Add(aToZItem);

            // Z-A Name Order
            var zToAItem = new System.Windows.Controls.MenuItem { Header = "📝 Z-A Name Order" };
            zToAItem.Click += (s, e) => FilterByNameZA();
            filterMenu.Items.Add(zToAItem);

            // Separator
            filterMenu.Items.Add(new System.Windows.Controls.Separator());

            // Group by Location
            var groupByLocationItem = new System.Windows.Controls.MenuItem { Header = "📍 Group by Location" };
            groupByLocationItem.Click += (s, e) => GroupByLocation();
            filterMenu.Items.Add(groupByLocationItem);

            // Group by MAC Address
            var groupByMacItem = new System.Windows.Controls.MenuItem { Header = "🔗 Group by MAC Address" };
            groupByMacItem.Click += (s, e) => GroupByMacAddress();
            filterMenu.Items.Add(groupByMacItem);

            // Separator
            filterMenu.Items.Add(new System.Windows.Controls.Separator());

            // Show online devices only
            var onlineOnlyItem = new System.Windows.Controls.MenuItem { Header = "🟢 Online Devices Only" };
            onlineOnlyItem.Click += (s, e) => FilterByStatus(DeviceStatus.Online);
            filterMenu.Items.Add(onlineOnlyItem);

            // Show offline devices only
            var offlineOnlyItem = new System.Windows.Controls.MenuItem { Header = "🔴 Offline Devices Only" };
            offlineOnlyItem.Click += (s, e) => FilterByStatus(DeviceStatus.Offline);
            filterMenu.Items.Add(offlineOnlyItem);

            // Separator
            filterMenu.Items.Add(new System.Windows.Controls.Separator());

            // Show all devices
            var showAllItem = new System.Windows.Controls.MenuItem { Header = "🔄 Show All Devices" };
            showAllItem.Click += (s, e) => ShowAllDevices();
            filterMenu.Items.Add(showAllItem);

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

        private void GroupByMacAddress()
        {
            // Group devices by MAC address, then order by MAC address and device name
            var groupedDevices = Devices
                .OrderBy(d => d.DeviceInfo.MAC ?? "Unknown")
                .ThenBy(d => d.DeviceInfo.DeviceName)
                .ToList();

            Devices.Clear();
            foreach (var device in groupedDevices)
            {
                Devices.Add(device);
            }
        }

        private void FilterByStatus(DeviceStatus status)
        {
            // Store original collection if not already stored
            if (_filteredDevices == null)
            {
                _filteredDevices = new ObservableCollection<DeviceManagementModel>(Devices);
            }

            var filteredDevices = _filteredDevices.Where(d =>
                d.DeviceInfo.Status == status ||
                (status == DeviceStatus.Online && d.DeviceInfo.Status == DeviceStatus.Connected) ||
                (status == DeviceStatus.Offline && d.DeviceInfo.Status == DeviceStatus.NotConnected)).ToList();

            Devices.Clear();
            foreach (var device in filteredDevices)
            {
                Devices.Add(device);
            }
        }

        private void ShowAllDevices()
        {
            if (_filteredDevices != null)
            {
                Devices.Clear();
                foreach (var device in _filteredDevices)
                {
                    Devices.Add(device);
                }
                _filteredDevices = null;
            }
        }

        // Load YAML configs from config folder
        private void LoadYamlConfigs()
        {
            try
            {
                // Use YamlFileManager for both loading and saving consistency
                var deviceModels = _configManager.LoadAllDeviceManagementModels();

                foreach (var deviceModel in deviceModels)
                {
                    if (deviceModel?.DeviceInfo != null)
                    {
                        // Subscribe to property changes before adding to collection
                        SubscribeToDeviceChanges(deviceModel);

                        Devices.Add(deviceModel);

                        // Add to both monitors
                        _statusMonitor.AddDevice(deviceModel.DeviceInfo);
                        _netModeMonitor.AddDevice(deviceModel.DeviceInfo);
                    }
                }

                // If no DeviceManagementModels found, try loading legacy DeviceConfig format
                if (Devices.Count == 0)
                {
                    var legacyConfigManager = new DeviceConfigManager();
                    var legacyConfigs = legacyConfigManager.LoadAllDeviceConfigs();

                    foreach (var config in legacyConfigs)
                    {
                        if (config.DeviceInfo != null)
                        {
                            var deviceModel = new DeviceManagementModel(config.DeviceInfo);
                            deviceModel.DeviceNumber = config.DeviceNumber;
                            deviceModel.CreatedDate = config.CreatedDate;
                            deviceModel.LastUpdated = config.LastModified;

                            // Subscribe to property changes before adding to collection
                            SubscribeToDeviceChanges(deviceModel);

                            Devices.Add(deviceModel);

                            // Add to both monitors
                            _statusMonitor.AddDevice(deviceModel.DeviceInfo);
                            _netModeMonitor.AddDevice(deviceModel.DeviceInfo);

                            // Convert legacy config to new format
                            Task.Run(() => SaveDeviceConfigAsync(deviceModel));
                        }
                    }
                }

                UpdateDeviceCounts();
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
                UpdateDeviceCounts();
                return;
            }

            // Set device number
            deviceModel.DeviceNumber = Devices.Count + 1;

            // Subscribe to property changes before adding
            SubscribeToDeviceChanges(deviceModel);

            Devices.Add(deviceModel);

            // Add to both monitors
            if (deviceModel.DeviceInfo != null)
            {
                _statusMonitor.AddDevice(deviceModel.DeviceInfo);
                _netModeMonitor.AddDevice(deviceModel.DeviceInfo);
            }

            // Save the new device to YAML
            Task.Run(() => SaveDeviceConfigAsync(deviceModel));
            UpdateDeviceCounts();
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
                var deviceName = SelectedDevice.DeviceInfo?.DeviceName ?? "Unknown Device";

                // Show confirmation dialog
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to remove '{deviceName}' from device management?\n\nThis action cannot be undone.",
                    "Confirm Device Removal",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Unsubscribe from property changes
                    UnsubscribeFromDeviceChanges(SelectedDevice);

                    // Remove from both monitors
                    if (SelectedDevice.DeviceInfo != null)
                    {
                        _statusMonitor.RemoveDevice(SelectedDevice.DeviceInfo);
                        _netModeMonitor.RemoveDevice(SelectedDevice.DeviceInfo);
                    }

                    // Remove config file using YamlFileManager
                    if (SelectedDevice.DeviceInfo != null)
                    {
                        _configManager.DeleteDeviceConfig(SelectedDevice.DeviceInfo);
                    }

                    Devices.Remove(SelectedDevice);
                    SelectedDevice = null;
                    UpdateDeviceCounts();

                    // Show success message
                    System.Windows.MessageBox.Show(
                        $"Device '{deviceName}' has been successfully removed.",
                        "Device Removed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
        }

        private void EditSelectedDevice()
        {
            if (SelectedDevice != null)
            {
                // Simple configuration - just update the last modified time
                SelectedDevice.UpdateLastModified();
                System.Windows.MessageBox.Show(
                    $"Device '{SelectedDevice.DeviceInfo.DeviceName}' configuration opened!\n\n" +
                    $"IP Address: {SelectedDevice.DeviceInfo.IP}\n" +
                    $"Location: {SelectedDevice.DeviceInfo.Location}\n" +
                    $"Status: {SelectedDevice.DeviceInfo.Status}\n" +
                    $"Connection: {SelectedDevice.DeviceInfo.ConnectionStatusText}\n",
                    "Configure Device",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }

        private void RefreshDevices()
        {
            IsLoading = true;

            // Simulate refresh operation
            Task.Run(async () =>
            {
                await Task.Delay(2000); // Simulate network operation

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    int updatedCount = 0;
                    var random = new Random();

                    foreach (var device in Devices)
                    {
                        device.UpdateLastModified();

                        // Randomly update some device statuses (30% chance)
                        if (random.Next(0, 10) < 3)
                        {
                            var oldStatus = device.DeviceInfo.Status;

                            // Toggle between online/offline with some logic
                            if (oldStatus == DeviceStatus.Online || oldStatus == DeviceStatus.Connected)
                            {
                                device.DeviceInfo.Status = random.Next(0, 2) == 0 ? DeviceStatus.Offline : DeviceStatus.Online;
                            }
                            else
                            {
                                device.DeviceInfo.Status = random.Next(0, 2) == 0 ? DeviceStatus.Online : DeviceStatus.Offline;
                            }

                            device.DeviceInfo.LastSeen = DateTime.Now;
                            updatedCount++;
                        }
                    }

                    UpdateDeviceCounts();
                    IsLoading = false;

                    // Show refresh result
                    System.Windows.MessageBox.Show(
                        $"Device refresh completed!\n\n" +
                        $"Total devices: {Devices.Count}\n" +
                        $"Status updates: {updatedCount}\n" +
                        $"Online devices: {OnlineDevicesCount}\n" +
                        $"Offline devices: {OfflineDevicesCount}",
                        "Refresh Complete",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                });
            });
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private void FilterDevices()
        {
            // Enhanced search functionality
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Show all devices if search is empty
                ShowAllDevices();
                return;
            }

            // Store original collection if filtering for the first time
            if (_filteredDevices == null)
            {
                _filteredDevices = new ObservableCollection<DeviceManagementModel>(Devices);
            }

            var searchTerm = SearchText.ToLower();
            var filteredDevices = _filteredDevices.Where(d =>
                (d.DeviceInfo?.DeviceName?.ToLower().Contains(searchTerm) ?? false) ||
                (d.DeviceInfo?.IP?.ToLower().Contains(searchTerm) ?? false) ||
                (d.DeviceInfo?.Location?.ToLower().Contains(searchTerm) ?? false) ||
                (d.DeviceInfo?.MAC?.ToLower().Contains(searchTerm) ?? false) ||
                d.DeviceNumber.ToString().Contains(searchTerm)).ToList();

            Devices.Clear();
            foreach (var device in filteredDevices)
            {
                Devices.Add(device);
            }
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
                    UpdateDeviceCounts();

                    // Notify NetModeMonitor of status changes
                    if (e.NewStatus == DeviceStatus.Connected)
                    {
                        _netModeMonitor.OnDeviceConnected(e.Device);
                    }
                    else if (e.NewStatus == DeviceStatus.Offline)
                    {
                        _netModeMonitor.OnDeviceDisconnected(e.Device);
                    }
                }
            });
        }

        public void Dispose()
        {
            // Unsubscribe from all device property changes
            if (Devices != null)
            {
                foreach (var device in Devices)
                {
                    UnsubscribeFromDeviceChanges(device);
                }
            }

            if (_statusMonitor != null)
            {
                _statusMonitor.StopMonitoring();
                _statusMonitor.DeviceStatusChanged -= OnDeviceStatusChanged;
                _statusMonitor = null;
            }

            if (_netModeMonitor != null)
            {
                _netModeMonitor.StopMonitoring();
                _netModeMonitor.NetModeChanged -= OnNetModeChanged;
                _netModeMonitor = null;
            }

            _configManager = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}