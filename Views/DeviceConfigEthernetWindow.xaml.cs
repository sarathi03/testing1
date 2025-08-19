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
            vm = new DeviceConfigViewModel();   // 👈 fixed (no "var")

            if (!string.IsNullOrEmpty(deviceIp))
                vm.DeviceIp = deviceIp;

            if (!string.IsNullOrEmpty(macAddress))
                vm.MACAddress = macAddress;

            DataContext = vm;
        }

        private void EthernetTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Tag = 0; // force binded value
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Call RS485, Ethernet, and Wifi read commands once
            if (vm.ReadRS485Command.CanExecute(null))
                vm.ReadRS485Command.Execute(null);

            if (vm.ReadEthernetCommand.CanExecute(null))
                vm.ReadEthernetCommand.Execute(null);

            if (vm.ReadWifiCommand.CanExecute(null))
                vm.ReadWifiCommand.Execute(null);
        }
    }
}
