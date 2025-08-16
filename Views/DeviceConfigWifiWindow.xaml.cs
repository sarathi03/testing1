using System.Windows;
using testing1.ViewModels;

namespace testing1.Views
{
    public partial class DeviceConfigWifiWindow : Window
    {
        public DeviceConfigWifiWindow(string deviceIp = null, string macAddress = null)
        {
            InitializeComponent();
            var vm = new DeviceConfigViewModel();
            if (!string.IsNullOrEmpty(deviceIp)) vm.DeviceIp = deviceIp;
            if (!string.IsNullOrEmpty(macAddress)) vm.MACAddress = macAddress;
            DataContext = vm;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}