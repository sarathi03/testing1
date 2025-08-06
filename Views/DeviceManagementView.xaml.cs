using System;
using System.Windows;
using System.Windows.Controls;
using testing1.Models;
using testing1.ViewModels;
using testing1.Views;
using System.ComponentModel;

namespace testing1.Views
{
    public partial class DeviceManagementView : UserControl, IDisposable
    {
        public DeviceManagementViewModel ViewModel { get; private set; }
        public event EventHandler NavigateToDiscovery;

        public DeviceManagementView()
        {
            InitializeComponent();
            ViewModel = new DeviceManagementViewModel();
            DataContext = ViewModel;
        }

        // Constructor that accepts a device to add
        public DeviceManagementView(DeviceInfo deviceToAdd) : this()
        {
            if (deviceToAdd != null)
            {
                ViewModel.AddDevice(deviceToAdd);
            }
        }

        // Method to add a device from external call
        public void AddDevice(DeviceInfo device)
        {
            ViewModel?.AddDevice(device);
        }

        // Method to show the parent window and bring it to front
        public void ShowAndActivate()
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Show();
                parentWindow.Activate();
                parentWindow.Focus();
            }
        }

        // Method to trigger navigation to DiscoveryView
        public void NavigateToDiscoveryView()
        {
            NavigateToDiscovery?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            ViewModel?.Dispose();
        }

        private void NavigateToDiscovery_Click(object sender, RoutedEventArgs e)
        {
            NavigateToDiscoveryView();
        }

        private void ConfigureDevice(DeviceManagementModel deviceModel)
        {
            if (deviceModel?.DeviceInfo == null)
            {
                MessageBox.Show("No device selected or device information is missing.", "Configuration Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate that the device IP is available
            if (string.IsNullOrEmpty(deviceModel.DeviceInfo.IP))
            {
                MessageBox.Show("Device IP address is not available.", "Configuration Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if device is online before configuring
            if (deviceModel.DeviceInfo.Status == DeviceStatus.Offline)
            {
                var result = MessageBox.Show(
                    $"Device {deviceModel.DeviceInfo.DeviceName} appears to be offline. Do you want to attempt configuration anyway?",
                    "Device Offline",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            try
            {
                // Create and show the configuration window
                var configWindow = new DeviceConfigWindow(deviceModel.DeviceInfo.IP, deviceModel.DeviceInfo.MAC)
                {
                    Owner = Window.GetWindow(this),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Title = $"Configure Device - {deviceModel.DeviceInfo.DeviceName} ({deviceModel.DeviceInfo.IP})"
                };

                // Show the configuration dialog
                configWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening device configuration: {ex.Message}", "Configuration Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Handler for Configure button click
        public void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DeviceManagementModel deviceModel)
            {
                ConfigureDevice(deviceModel);
            }
            else
            {
                MessageBox.Show("Unable to identify the selected device.", "Configuration Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Optional: Handler for double-click on device row to configure
        private void DeviceListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedDevice != null)
            {
                ConfigureDevice(ViewModel.SelectedDevice);
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}

// Extension method for GetViewModel (add this if not already present elsewhere)
namespace testing1.Extensions
{
    public static class ViewExtensions
    {
        public static T GetViewModel<T>(this UserControl view) where T : class
        {
            return view.DataContext as T;
        }

        public static T GetViewModel<T>(this Window view) where T : class
        {
            return view.DataContext as T;
        }
    }
}
