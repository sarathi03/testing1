using System.Windows;
using testing1.Models;
using testing1.ViewModels;

namespace testing1
{
    public partial class MainWindow : Window
    {
        private DeviceConfigManager configManager;

        public MainWindow()
        {
            InitializeComponent();
            configManager = new DeviceConfigManager();
            // Start with DiscoveryView
            var discoveryView = new Views.DiscoveryView();
            discoveryView.NavigateToDeviceManagement += OnNavigateToDeviceManagement;
            MainContent.Content = discoveryView;
        }

        // Handle navigation from DiscoveryView to DeviceManagementView
        private void OnNavigateToDeviceManagement(object sender, System.EventArgs e)
        {
            var deviceManagementView = new Views.DeviceManagementView();
            deviceManagementView.NavigateToDiscovery += OnNavigateToDiscovery;
            MainContent.Content = deviceManagementView;
            
            // Bring window to front
            this.Activate();
            this.Focus();
        }

        // Handle navigation from DeviceManagementView to DiscoveryView
        private void OnNavigateToDiscovery(object sender, System.EventArgs e)
        {
            var discoveryView = new Views.DiscoveryView();
            discoveryView.NavigateToDeviceManagement += OnNavigateToDeviceManagement;
            MainContent.Content = discoveryView;
            
            // Bring window to front
            this.Activate();
            this.Focus();
        }

        private void AddSelectedDevices_Click(object sender, RoutedEventArgs e)
        {
            // Your device selection logic here
            var device = new DeviceInfo
            {
                IP = "192.168.1.100",
                MAC = "aa:bb:cc:dd:ee:ff",
                DeviceName = "Test Device",
                Status = DeviceStatus.Online
            };

            bool success = configManager.SaveDeviceConfig(device);
            if (success)
            {
                MessageBox.Show($"Device saved successfully!\nConfig saved to: {configManager.GetConfigDirectory()}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            configManager.OpenConfigDirectory();
        }
    }
}
