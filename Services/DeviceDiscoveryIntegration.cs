using System;
using System.Windows;
using testing1.Models;
using testing1.ViewModels;
using testing1.Views;

namespace testing1.Services
{
    /// <summary>
    /// Helper class to integrate device discovery with device management
    /// </summary>
    public static class DeviceDiscoveryIntegration
    {
        private static DeviceManagementView _deviceManagementView;
        private static Window _deviceManagementWindow;

        /// <summary>
        /// Initialize or show the device management window
        /// </summary>
        public static void ShowDeviceManagementWindow()
        {
            try
            {
                if (_deviceManagementWindow == null || !_deviceManagementWindow.IsLoaded)
                {
                    _deviceManagementView = new DeviceManagementView();
                    _deviceManagementWindow = new Window
                    {
                        Title = "Device Management Panel",
                        Content = _deviceManagementView,
                        Width = 1200,
                        Height = 700,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.CanResize
                    };

                    _deviceManagementWindow.Closed += (s, e) =>
                    {
                        _deviceManagementWindow = null;
                        _deviceManagementView = null;
                    };
                }

                _deviceManagementWindow.Show();
                _deviceManagementWindow.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing Device Management window: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Add a selected device from discovery window to management panel
        /// </summary>
        /// <param name="deviceInfo">Device to add</param>
        public static async void AddSelectedDeviceToManagement(DeviceInfo deviceInfo)
        {
            try
            {
                if (deviceInfo == null)
                {
                    MessageBox.Show("Device information is null.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Ensure management window is open
                ShowDeviceManagementWindow();

                // Add device to management panel
                if (_deviceManagementView?.ViewModel != null)
                {
                    await _deviceManagementView.ViewModel.AddSelectedDeviceAsync(deviceInfo);
                }
                else
                {
                    MessageBox.Show("Device Management window is not available.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding device to management panel: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Get reference to the current device management view model
        /// </summary>
        /// <returns>DeviceManagementViewModel or null if not available</returns>
        public static DeviceManagementViewModel GetDeviceManagementViewModel()
        {
            return _deviceManagementView?.ViewModel;
        }

        /// <summary>
        /// Check if device management window is currently open
        /// </summary>
        /// <returns>True if window is open and loaded</returns>
        public static bool IsDeviceManagementWindowOpen()
        {
            return _deviceManagementWindow != null && _deviceManagementWindow.IsLoaded;
        }

        /// <summary>
        /// Close the device management window
        /// </summary>
        public static void CloseDeviceManagementWindow()
        {
            _deviceManagementWindow?.Close();
        }
    }

    /// <summary>
    /// Extension methods for easier integration
    /// </summary>
    public static class DeviceInfoExtensions
    {
        /// <summary>
        /// Add this device to the management panel
        /// </summary>
        /// <param name="deviceInfo">Device to add</param>
        public static void AddToManagementPanel(this DeviceInfo deviceInfo)
        {
            DeviceDiscoveryIntegration.AddSelectedDeviceToManagement(deviceInfo);
        }

        /// <summary>
        /// Create a DeviceManagementModel from DeviceInfo
        /// </summary>
        /// <param name="deviceInfo">Source device info</param>
        /// <returns>New DeviceManagementModel</returns>
        public static DeviceManagementModel ToManagementModel(this DeviceInfo deviceInfo)
        {
            return new DeviceManagementModel(deviceInfo);
        }

        /// <summary>
        /// Validate if device info has required fields for management
        /// </summary>
        /// <param name="deviceInfo">Device to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidForManagement(this DeviceInfo deviceInfo)
        {
            return !string.IsNullOrWhiteSpace(deviceInfo?.IP) &&
                   deviceInfo.Port > 0 &&
                   !string.IsNullOrWhiteSpace(deviceInfo.MAC);
        }
    }
}