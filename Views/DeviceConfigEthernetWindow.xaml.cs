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

            // Add this line to bind the close action
            //vm.CloseAction = () => this.Close();

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Call read commands when window loads with error handling
            try
            {
                if (vm?.ReadGeneralCommand?.CanExecute(null) == true)
                    vm.ReadGeneralCommand.Execute(null);

                if (vm?.ReadRS485Command?.CanExecute(null) == true)
                    vm.ReadRS485Command.Execute(null);

                if (vm?.ReadEthernetCommand?.CanExecute(null) == true)
                    vm.ReadEthernetCommand.Execute(null);

                if (vm?.ReadWifiCommand?.CanExecute(null) == true)
                    vm.ReadWifiCommand.Execute(null);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading initial configuration: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}