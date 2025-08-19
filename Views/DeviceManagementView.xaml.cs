using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using testing1.Models;
using testing1.ViewModels;

namespace testing1.Views
{
    public partial class DeviceManagementView : UserControl, IDisposable
    {
        public DeviceManagementViewModel ViewModel { get; set; }
        public event EventHandler NavigateToDiscovery;

        private DispatcherTimer _popupTimer;

        public DeviceManagementView()
        {
            InitializeComponent();
            ViewModel = new DeviceManagementViewModel();
            DataContext = ViewModel;

            // Initialize popup timer for hover functionality
            _popupTimer = new DispatcherTimer();
            _popupTimer.Interval = TimeSpan.FromMilliseconds(200);
            _popupTimer.Tick += PopupTimer_Tick;
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
            _popupTimer?.Stop();
            ViewModel?.Dispose();
        }

        private void NavigateToDiscovery_Click(object sender, RoutedEventArgs e)
        {
            NavigateToDiscoveryView();
        }

        // Method to get current selected device
        public DeviceManagementModel GetSelectedDevice()
        {
            return ViewModel?.SelectedDevice;
        }

        #region Configuration Hover Popup Event Handlers

        private void ConfigureButton_MouseEnter(object sender, MouseEventArgs e)
        {
            _popupTimer.Stop();
            _popupTimer.Tag = sender; // Store the button reference
            _popupTimer.Start();
        }

        private void ConfigureButton_MouseLeave(object sender, MouseEventArgs e)
        {
            _popupTimer.Stop();

            // Small delay before hiding to allow mouse to move to popup
            var hideTimer = new DispatcherTimer();
            hideTimer.Interval = TimeSpan.FromMilliseconds(150);
            hideTimer.Tick += (s, args) =>
            {
                hideTimer.Stop();
                if (sender is Button button)
                {
                    var popup = FindConfigurationPopup(button);
                    if (popup != null && !popup.IsMouseOver)
                    {
                        popup.IsOpen = false;
                    }
                }
            };
            hideTimer.Start();
        }

        private void ConfigurationPopup_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.Popup popup)
            {
                popup.IsOpen = false;
            }
        }

        private void PopupTimer_Tick(object sender, EventArgs e)
        {
            _popupTimer.Stop();

            if (_popupTimer.Tag is Button configureButton)
            {
                var popup = FindConfigurationPopup(configureButton);
                if (popup != null)
                {
                    popup.IsOpen = true;
                }
            }
        }

        #endregion

        #region WiFi and Ethernet Button Handlers

        private void WifiButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(sender is Button wifiButton))
                {
                    MessageBox.Show("WiFi button not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get the device model from the button's tag or from the selected device
                var deviceModel = GetDeviceModelFromButton(wifiButton) ?? ViewModel?.SelectedDevice;

                if (deviceModel?.DeviceInfo == null)
                {
                    MessageBox.Show("Please select a device first.", "No Device Selected",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Close any open popup
                ClosePopupFromButton(wifiButton);

                // Open WiFi configuration window
                OpenWifiConfiguration(deviceModel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening WiFi configuration: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EthernetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(sender is Button ethernetButton))
                {
                    MessageBox.Show("Ethernet button not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get the device model from the button's tag or from the selected device
                var deviceModel = GetDeviceModelFromButton(ethernetButton) ?? ViewModel?.SelectedDevice;

                if (deviceModel?.DeviceInfo == null)
                {
                    MessageBox.Show("Please select a device first.", "No Device Selected",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Close any open popup
                ClosePopupFromButton(ethernetButton);

                // Open Ethernet configuration window
                OpenEthernetConfiguration(deviceModel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Ethernet configuration: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Configuration Window Navigation

        private void OpenWifiConfiguration(DeviceManagementModel deviceModel)
        {
            try
            {
                var wifiWindow = new DeviceConfigWifiWindow(deviceModel.DeviceInfo.IP, deviceModel.DeviceInfo.MAC);
                wifiWindow.Owner = Window.GetWindow(this);
                wifiWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                wifiWindow.Title = $"Configure WiFi - {deviceModel.DeviceInfo.DeviceName} ({deviceModel.DeviceInfo.IP})";
                wifiWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening WiFi configuration: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEthernetConfiguration(DeviceManagementModel deviceModel)
        {
            try
            {
                var ethernetWindow = new DeviceConfigEthernetWindow(deviceModel.DeviceInfo.IP, deviceModel.DeviceInfo.MAC);
                ethernetWindow.Owner = Window.GetWindow(this);
                ethernetWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ethernetWindow.Title = $"Configure Ethernet - {deviceModel.DeviceInfo.DeviceName} ({deviceModel.DeviceInfo.IP})";
                ethernetWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Ethernet configuration: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private DeviceManagementModel GetDeviceModelFromButton(Button button)
        {
            // Try to get device model from button's DataContext or Tag
            if (button.DataContext is DeviceManagementModel deviceModel)
                return deviceModel;

            if (button.Tag is DeviceManagementModel taggedModel)
                return taggedModel;

            return null;
        }

        private void ClosePopupFromButton(Button button)
        {
            var popup = FindParentPopup(button);
            if (popup != null)
            {
                popup.IsOpen = false;
            }
        }

        private System.Windows.Controls.Primitives.Popup FindConfigurationPopup(Button configureButton)
        {
            // The popup is in the same Grid as the configure button
            var parent = configureButton.Parent;
            while (parent != null)
            {
                if (parent is Grid grid)
                {
                    foreach (UIElement child in grid.Children)
                    {
                        if (child is System.Windows.Controls.Primitives.Popup popup && popup.Name == "ConfigurationPopup")
                        {
                            return popup;
                        }
                    }
                }
                parent = System.Windows.LogicalTreeHelper.GetParent(parent);
            }
            return null;
        }

        private System.Windows.Controls.Primitives.Popup FindParentPopup(DependencyObject element)
        {
            // Traverse up the visual tree to find the popup
            DependencyObject parent = element;
            while (parent != null)
            {
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                if (parent is System.Windows.Controls.Primitives.Popup popup && popup.Name == "ConfigurationPopup")
                {
                    return popup;
                }
            }
            return null;
        }

        #endregion

        #region Temporary Method (Remove after XAML is updated)

        // TODO: Remove this method after updating XAML
        // This is a temporary placeholder to prevent compile errors
        private void PopupConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            // This method should be removed once XAML is updated
            // The Configure button should be removed from XAML entirely
        }

        #endregion

        #region Event Handlers for XAML Elements

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // DataGrid selection is handled automatically through data binding
            // The ViewModel's SelectedDevice property is updated automatically
        }

        #endregion

    }
}