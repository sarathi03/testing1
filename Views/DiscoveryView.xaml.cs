using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Threading.Tasks;
using testing1.ViewModels;
using testing1.Models;
using System.Linq;

namespace testing1.Views
{
    public partial class DiscoveryView : UserControl
    {
        public event System.EventHandler NavigateToDeviceManagement;

        public DiscoveryView()
        {
            InitializeComponent();
            this.DataContext = new DiscoveryViewModel();
        }

        // Optional: Handle window events if needed
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                // Add any window-level event handlers here if needed
                window.LocationChanged += (s, e) => { /* Handle window move if needed */ };
                window.SizeChanged += (s, e) => { /* Handle window resize if needed */ };
            }
        }

        // Method to trigger navigation to DeviceManagementView
        public void NavigateToDeviceManagementView()
        {
            NavigateToDeviceManagement?.Invoke(this, System.EventArgs.Empty);
        }

        // Handle Add Selected Devices button click
        private void AddSelectedDevices_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DiscoveryViewModel vm)
            {
                var selectedDevices = vm.AvailableDevices.Where(d => d.IsSelected)
                                                       .Select(d => new DeviceInfo
                                                       {
                                                           IP = d.IPAddress,
                                                           MAC = d.MacAddress ?? "Unknown",
                                                           DeviceName = $"Device_{d.IPAddress.Replace(".", "_")}",
                                                           Status = DeviceStatus.Discovered
                                                       })
                                                       .ToList();

                if (selectedDevices.Count == 0)
                {
                    MessageBox.Show("No devices selected. Please select devices to save.", "Information",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Save to YAML
                var configManager = new DeviceConfigManager();
                bool success = configManager.SaveMultipleDeviceConfigs(selectedDevices);

                if (success)
                {
                    MessageBox.Show($"Successfully saved {selectedDevices.Count} devices to YAML config files.\nConfig saved to: {configManager.GetConfigDirectory()}",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Navigate to DeviceManagementView
                    NavigateToDeviceManagementView();
                }
                else
                {
                    MessageBox.Show("Failed to save devices to YAML config files.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Handle Manage Device button click
        private void ManageDevice_Click(object sender, RoutedEventArgs e)
        {
            NavigateToDeviceManagementView();
        }
    }
}