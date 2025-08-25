using System;
using System.Collections.Generic;
using System.Linq;
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

        #region WiFi and Ethernet Button Handlers - UPDATED FOR IMMEDIATE RESPONSE

        private void WifiButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // EMERGENCY: Force close all popups immediately
                foreach (System.Windows.Controls.Primitives.Popup popup in FindVisualChildren<System.Windows.Controls.Primitives.Popup>(this))
                {
                    if (popup.IsOpen) popup.IsOpen = false;
                }

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

                // Open WiFi configuration immediately (non-blocking)
                OpenWifiConfigurationImmediate(deviceModel);
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
                // EMERGENCY: Force close all popups immediately
                foreach (System.Windows.Controls.Primitives.Popup popup in FindVisualChildren<System.Windows.Controls.Primitives.Popup>(this))
                {
                    if (popup.IsOpen) popup.IsOpen = false;
                }

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

                // Open Ethernet configuration immediately (non-blocking)
                OpenEthernetConfigurationImmediate(deviceModel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Ethernet configuration: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Immediate Configuration Window Navigation - UPDATED

        private void OpenWifiConfigurationImmediate(DeviceManagementModel deviceModel)
        {
            try
            {
                // Create window immediately
                var wifiWindow = new DeviceConfigWifiWindow(deviceModel.DeviceInfo.IP, deviceModel.DeviceInfo.MAC);
                wifiWindow.Owner = Window.GetWindow(this);
                wifiWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                wifiWindow.Title = $"Configure WiFi - {deviceModel.DeviceInfo.DeviceName} ({deviceModel.DeviceInfo.IP})";

                // Show immediately (non-blocking) - UI responds instantly
                wifiWindow.Show(); // Changed from ShowDialog() to Show()

                // Bring to front
                wifiWindow.Activate();

                // Start loading configurations in background (fire and forget)
                // The window will handle its own loading states
                _ = LoadWifiConfigurationsInBackground(wifiWindow);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening WiFi configuration: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEthernetConfigurationImmediate(DeviceManagementModel deviceModel)
        {
            try
            {
                // Create window immediately
                var ethernetWindow = new DeviceConfigEthernetWindow(deviceModel.DeviceInfo.IP, deviceModel.DeviceInfo.MAC);
                ethernetWindow.Owner = Window.GetWindow(this);
                ethernetWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ethernetWindow.Title = $"Configure Ethernet - {deviceModel.DeviceInfo.DeviceName} ({deviceModel.DeviceInfo.IP})";

                // Show immediately (non-blocking) - UI responds instantly
                ethernetWindow.Show(); // Changed from ShowDialog() to Show()

                // Bring to front
                ethernetWindow.Activate();

                // Start loading configurations in background (fire and forget)
                // The window will handle its own loading states
                _ = LoadEthernetConfigurationsInBackground(ethernetWindow);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Ethernet configuration: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Background Configuration Loading - NEW

        private async System.Threading.Tasks.Task LoadWifiConfigurationsInBackground(DeviceConfigWifiWindow wifiWindow)
        {
            try
            {
                // Let the window handle its own loading
                // If the window has a method like LoadConfigurationsAsync(), call it here
                // await wifiWindow.LoadConfigurationsAsync();

                // Or if you need to do loading here and pass data to window:
                // var configurations = await LoadWifiConfigurationsAsync();
                // wifiWindow.UpdateConfigurations(configurations);
            }
            catch (Exception ex)
            {
                // Handle background loading errors
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Error loading WiFi configurations: {ex.Message}",
                                  "Configuration Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }));
            }
        }

        private async System.Threading.Tasks.Task LoadEthernetConfigurationsInBackground(DeviceConfigEthernetWindow ethernetWindow)
        {
            try
            {
                // Let the window handle its own loading
                // If the window has a method like LoadConfigurationsAsync(), call it here
                // await ethernetWindow.LoadConfigurationsAsync();

                // Or if you need to do loading here and pass data to window:
                // var configurations = await LoadEthernetConfigurationsAsync();
                // ethernetWindow.UpdateConfigurations(configurations);
            }
            catch (Exception ex)
            {
                // Handle background loading errors
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Error loading Ethernet configurations: {ex.Message}",
                                  "Configuration Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }));
            }
        }

        #endregion

        #region Helper Methods - UPDATED

        private DeviceManagementModel GetDeviceModelFromButton(Button button)
        {
            // Try to get device model from button's DataContext or Tag
            if (button.DataContext is DeviceManagementModel deviceModel)
                return deviceModel;

            if (button.Tag is DeviceManagementModel taggedModel)
                return taggedModel;

            return null;
        }

        // UPDATED: Immediate popup closure - no delays, multiple methods to ensure closure
        private void ClosePopupImmediately(Button button)
        {
            // Method 1: Find popup from button hierarchy
            var popup = FindParentPopup(button);
            if (popup != null)
            {
                popup.IsOpen = false;
            }

            // Method 2: Find popup using the Configure button approach
            // Get the configure button that opened this popup
            var configureButton = FindConfigureButtonFromWifiEthernetButton(button);
            if (configureButton != null)
            {
                var configPopup = FindConfigurationPopup(configureButton);
                if (configPopup != null)
                {
                    configPopup.IsOpen = false;
                }
            }

            // Method 3: Force close all popups with "ConfigurationPopup" name
            CloseAllConfigurationPopups();
        }

        // Keep original method for backward compatibility
        private void ClosePopupFromButton(Button button)
        {
            ClosePopupImmediately(button);
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

        // NEW: Find configure button from WiFi/Ethernet button
        private Button FindConfigureButtonFromWifiEthernetButton(Button wifiEthernetButton)
        {
            // The WiFi/Ethernet button is inside the popup, 
            // we need to find the Configure button that opened this popup
            var parent = wifiEthernetButton.Parent;
            while (parent != null)
            {
                if (parent is Grid grid)
                {
                    // Look for Configure button in the same container
                    foreach (UIElement child in grid.Children)
                    {
                        if (child is Button btn && (btn.Name == "ConfigureButton" || btn.Content?.ToString() == "Configure"))
                        {
                            return btn;
                        }
                    }
                }
                parent = System.Windows.LogicalTreeHelper.GetParent(parent);
            }
            return null;
        }

        // NEW: Helper method to find all visual children of a specific type
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);

                if (child != null && child is T)
                    yield return (T)child;

                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        // NEW: Force close all configuration popups
        private void CloseAllConfigurationPopups()
        {
            CloseAllPopupsInVisualTree(this);
        }

        // NEW: Recursively find and close all popups
        private void CloseAllPopupsInVisualTree(DependencyObject parent)
        {
            if (parent == null) return;

            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is System.Windows.Controls.Primitives.Popup popup &&
                    (popup.Name == "ConfigurationPopup" || popup.IsOpen))
                {
                    popup.IsOpen = false;
                }

                CloseAllPopupsInVisualTree(child);
            }
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