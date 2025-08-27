using System.Windows;
using System.Windows.Controls;
using testing1.ViewModels;

namespace testing1.Views
{
    public partial class DeviceConfigEthernetWindow : Window
    {
        private DeviceConfigViewModel vm;

        public DeviceConfigEthernetWindow(string deviceIp = null, string macAddress = null)
        {
            InitializeComponent();
            // Create the view model only once
            vm = new DeviceConfigViewModel();
            // Set the close action to allow the ViewModel to close the window
            vm.CloseAction = () => this.Close();
            // Set the properties if provided
            if (!string.IsNullOrEmpty(deviceIp))
                vm.DeviceIp = deviceIp;
            if (!string.IsNullOrEmpty(macAddress))
                vm.MACAddress = macAddress;
            // Set the DataContext
            DataContext = vm;
        }

        private void EthernetTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Tag = 0; // force bound value
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Use the new async loading method with loading state
            await vm.LoadAllConfigurationsAsync();
        }
    }
}