using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
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

        private DispatcherTimer _popupTimer;
        private string _selectedConfigType = "";
        private bool _isConfigurationTypeSelected = false;

        public DeviceManagementView()
        {
            InitializeComponent();
            ViewModel = new DeviceManagementViewModel();
            DataContext = ViewModel;

            // Initialize popup timer
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

        #region Configuration Popup Event Handlers

        private void ConfigureButton_MouseEnter(object sender, MouseEventArgs e)
        {
            _popupTimer.Stop();
            _popupTimer.Start();
        }

        private void ConfigureButton_MouseLeave(object sender, MouseEventArgs e)
        {
            _popupTimer.Stop();

            // Small delay before hiding to allow mouse to move to popup
            var hideTimer = new DispatcherTimer();
            hideTimer.Interval = TimeSpan.FromMilliseconds(100);
            hideTimer.Tick += (s, args) =>
            {
                hideTimer.Stop();
                if (sender is Button button)
                {
                    var popup = FindConfigurationPopup(button);
                    if (popup != null && !popup.IsMouseOver)
                    {
                        popup.IsOpen = false;
                        ResetPopupState(popup);
                    }
                }
            };
            hideTimer.Start();
        }

        private void ConfigurationPopup_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Popup popup)
            {
                popup.IsOpen = false;
                ResetPopupState(popup);
            }
        }

        private void PopupTimer_Tick(object sender, EventArgs e)
        {
            _popupTimer.Stop();

            // Find the configure button that was hovered
            var button = Mouse.DirectlyOver as FrameworkElement;
            while (button != null && !(button is Button && button.Name == "ConfigureButton"))
            {
                button = button.Parent as FrameworkElement;
            }

            if (button is Button configureButton)
            {
                var popup = FindConfigurationPopup(configureButton);
                if (popup != null)
                {
                    ResetPopupState(popup);
                    popup.IsOpen = true;
                }
            }
        }

        private void WifiButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button wifiButton)
            {
                var popup = FindParentPopup(wifiButton);
                if (popup != null)
                {
                    SetSelectedConfigType(popup, "WiFi");
                    UpdateConfigureButtonState(popup);
                }
            }
        }

        private void EthernetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button ethernetButton)
            {
                var popup = FindParentPopup(ethernetButton);
                if (popup != null)
                {
                    SetSelectedConfigType(popup, "Ethernet");
                    UpdateConfigureButtonState(popup);
                }
            }
        }

        private void PopupConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button configButton && configButton.Tag is DeviceManagementModel deviceModel)
            {
                var popup = FindParentPopup(configButton);
                if (popup != null)
                {
                    var portTextBox = FindChild<TextBox>(popup, "PortTextBox");
                    var port = portTextBox?.Text ?? "502";

                    popup.IsOpen = false;

                    ConfigureDevice(deviceModel, _selectedConfigType, port);
                }
            }
        }

        #endregion

        #region Helper Methods

        private Popup FindConfigurationPopup(Button configureButton)
        {
            var grid = configureButton.Parent as Grid;
            return grid?.FindName("ConfigurationPopup") as Popup;
        }

        private Popup FindParentPopup(FrameworkElement element)
        {
            var parent = element.Parent;
            while (parent != null)
            {
                if (parent is Popup popup)
                    return popup;
                parent = (parent as FrameworkElement)?.Parent;
            }
            return null;
        }

        private void SetSelectedConfigType(Popup popup, string configType)
        {
            _selectedConfigType = configType;
            _isConfigurationTypeSelected = true;

            // Update button visual states
            var wifiButton = FindChild<Button>(popup, "WifiButton");
            var ethernetButton = FindChild<Button>(popup, "EthernetButton");

            if (wifiButton != null)
            {
                wifiButton.SetValue(Button.TagProperty, configType == "WiFi" ? "Selected" : "");
            }

            if (ethernetButton != null)
            {
                ethernetButton.SetValue(Button.TagProperty, configType == "Ethernet" ? "Selected" : "");
            }
        }

        private void UpdateConfigureButtonState(Popup popup)
        {
            var configureButton = FindChild<Button>(popup, "PopupConfigureButton");
            if (configureButton != null)
            {
                configureButton.IsEnabled = _isConfigurationTypeSelected;

                // Update visual state
                if (_isConfigurationTypeSelected)
                {
                    configureButton.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#255DD4"));
                    configureButton.Foreground = System.Windows.Media.Brushes.White;
                }
                else
                {
                    configureButton.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC"));
                    configureButton.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666"));
                }
            }
        }

        private void ResetPopupState(Popup popup)
        {
            _selectedConfigType = "";
            _isConfigurationTypeSelected = false;

            var wifiButton = FindChild<Button>(popup, "WifiButton");
            var ethernetButton = FindChild<Button>(popup, "EthernetButton");
            var configureButton = FindChild<Button>(popup, "PopupConfigureButton");

            if (wifiButton != null)
            {
                wifiButton.SetValue(Button.TagProperty, "");
                wifiButton.Background = System.Windows.Media.Brushes.White;
                wifiButton.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333"));
            }

            if (ethernetButton != null)
            {
                ethernetButton.SetValue(Button.TagProperty, "");
                ethernetButton.Background = System.Windows.Media.Brushes.White;
                ethernetButton.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333"));
            }

            if (configureButton != null)
            {
                configureButton.IsEnabled = false;
                configureButton.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC"));
                configureButton.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666"));
            }

            var portTextBox = FindChild<TextBox>(popup, "PortTextBox");
            if (portTextBox != null)
            {
                portTextBox.Text = "502";
            }
        }

        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;
            int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T && (child as FrameworkElement)?.Name == childName)
                {
                    foundChild = (T)child;
                    break;
                }

                foundChild = FindChild<T>(child, childName);
                if (foundChild != null) break;
            }

            return foundChild;
        }

        #endregion

        private void ConfigureDevice(DeviceManagementModel deviceModel, string configType, string port)
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
                Window configWindow = null;
                string windowTitle = "";

                // Create the appropriate configuration window based on selected type
                if (configType == "WiFi")
                {
                    configWindow = new DeviceConfigWifiWindow(deviceModel.DeviceInfo.IP, deviceModel.DeviceInfo.MAC);
                    windowTitle = $"Configure WiFi Device - {deviceModel.DeviceInfo.DeviceName} ({deviceModel.DeviceInfo.IP})";
                }
                else if (configType == "Ethernet")
                {
                    configWindow = new DeviceConfigEthernetWindow(deviceModel.DeviceInfo.IP, deviceModel.DeviceInfo.MAC);
                    windowTitle = $"Configure Ethernet Device - {deviceModel.DeviceInfo.DeviceName} ({deviceModel.DeviceInfo.IP})";
                }
                else
                {
                    MessageBox.Show("Please select WiFi or Ethernet configuration type.", "Configuration Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Set window properties
                if (configWindow != null)
                {
                    configWindow.Owner = Window.GetWindow(this);
                    configWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    configWindow.Title = windowTitle;

                    // Set the port if the view model supports it
                    if (configWindow.DataContext is DeviceConfigViewModel vm && !string.IsNullOrEmpty(port))
                    {
                        if (int.TryParse(port, out int portValue))
                        {
                            vm.General.Port = portValue;
                        }
                    }

                    // Show the configuration dialog
                    configWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening device configuration: {ex.Message}", "Configuration Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Legacy method for backward compatibility (if still needed)
        public void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DeviceManagementModel deviceModel)
            {
                // For backward compatibility, default to the original configuration window
                ConfigureDevice(deviceModel, "Legacy", "502");
            }
            else
            {
                MessageBox.Show("Unable to identify the selected device.", "Configuration Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Optional: Handler for double-click on device row to configure
        private void DeviceListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedDevice != null)
            {
                ConfigureDevice(ViewModel.SelectedDevice, "Legacy", "502");
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle selection changed if needed
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