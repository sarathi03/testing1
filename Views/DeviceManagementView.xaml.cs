using System;
using System.Windows;
using System.Windows.Controls;
using testing1.Models;
using testing1.ViewModels;
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
            DataContext = ViewModel; // Set DataContext in code-behind instead of XAML
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
            if (deviceModel?.DeviceInfo == null) return;
            var configWindow = new DeviceConfigWindow(deviceModel.DeviceInfo.IP, deviceModel.DeviceInfo.MAC);
            configWindow.Owner = Window.GetWindow(this);
            configWindow.ShowDialog();
        }

        // Add handler for Configure button
        public void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DeviceManagementModel deviceModel)
            {
                ConfigureDevice(deviceModel);
            }
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