using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using testing1.Commands;
using testing1.Models;
using testing1.Helpers;
using testingwifi.Models;

namespace testing1.ViewModels
{
    public class DeviceConfigViewModel : INotifyPropertyChanged
    {
        private TcpClientHelper _tcpHelper;

        // Loading state properties
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowContent));
                OnPropertyChanged(nameof(ShowLoadingSpinner));
            }
        }

        private bool _hasData;
        public bool HasData
        {
            get => _hasData;
            set
            {
                _hasData = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowContent));
            }
        }

        // Computed properties for UI visibility
        public bool ShowLoadingSpinner => IsLoading;
        public bool ShowContent => !IsLoading && HasData;

        // Initialize objects with property change notifications
        private RS485Settings _rs485 = new();
        public RS485Settings RS485
        {
            get => _rs485;
            set
            {
                _rs485 = value;
                OnPropertyChanged();
            }
        }

        private EthernetSettings _ethernet = new();
        public EthernetSettings Ethernet
        {
            get => _ethernet;
            set
            {
                _ethernet = value;
                OnPropertyChanged();
            }
        }

        private WifiSettings _wifi = new();
        public WifiSettings Wifi
        {
            get => _wifi;
            set
            {
                _wifi = value;
                OnPropertyChanged();
            }
        }

        private GeneralSettings _general = new();
        public GeneralSettings General
        {
            get => _general;
            set
            {
                _general = value;
                OnPropertyChanged();
            }
        }

        private string _deviceIp;
        public string DeviceIp
        {
            get => _deviceIp;
            set { _deviceIp = value; OnPropertyChanged(); }
        }

        private string _macAddress;
        public string MACAddress
        {
            get => _macAddress;
            set { _macAddress = value; OnPropertyChanged(); }
        }

        // Configuration Commands
        public ICommand SendRS485Command { get; }
        public ICommand ReadRS485Command { get; }
        public ICommand SendEthernetCommand { get; }
        public ICommand ReadEthernetCommand { get; }
        public ICommand SendGeneralCommand { get; }
        public ICommand ReadGeneralCommand { get; }
        public ICommand SendWifiCommand { get; }
        public ICommand ReadWifiCommand { get; }
        public ICommand ApplyChangesCommand { get; }
        public ICommand ResetCommand { get; }
        public Action CloseAction { get; set; }

        public DeviceConfigViewModel()
        {
            SendRS485Command = new RelayCommand(SendRS485);
            ReadRS485Command = new RelayCommand(ReadRS485);
            SendEthernetCommand = new RelayCommand(SendEthernet);
            ReadEthernetCommand = new RelayCommand(ReadEthernet);
            SendWifiCommand = new RelayCommand(SendWifi);
            ReadWifiCommand = new RelayCommand(ReadWifi);
            SendGeneralCommand = new RelayCommand(SendGeneral);
            ReadGeneralCommand = new RelayCommand(ReadGeneral);
            ResetCommand = new RelayCommand(ResetDevice);
            ApplyChangesCommand = new RelayCommand(OnApplyChanges);
        }

        // New method to load all configurations with loading state
        public async Task LoadAllConfigurationsAsync()
        {
            IsLoading = true;
            HasData = false;

            try
            {
                await Task.Run(() =>
                {
                    // Execute all read commands
                    if (ReadGeneralCommand.CanExecute(null))
                        ReadGeneralCommand.Execute(null);
                    if (ReadRS485Command.CanExecute(null))
                        ReadRS485Command.Execute(null);
                    if (ReadEthernetCommand.CanExecute(null))
                        ReadEthernetCommand.Execute(null);
                    if (ReadWifiCommand.CanExecute(null))
                        ReadWifiCommand.Execute(null);
                });

                HasData = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configurations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                HasData = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnApplyChanges()
        {
            try
            {
                // Check if General config uses port 1502 before proceeding
                if (General.Port == 1502)
                {
                    MessageBox.Show("You cannot apply 1502 port.\nTry to use different Port", "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // Stop execution completely
                }

                // Execute configurations in the correct order (without reset)
                // 1. Send RS485 configuration
                SendRS485Command?.Execute(null);

                // 2. Send General configuration (modified to not reset immediately)
                SendGeneralCommand?.Execute(null); // New method without reset

                // 3. Send Wi-Fi configuration  
                SendWifiCommand?.Execute(null);

                // 4. Send Ethernet configuration
                SendEthernetCommand?.Execute(null);

                // 5. Finally send the Reset command after all configurations
                MessageBoxResult result = MessageBox.Show(
                    "All configurations sent successfully.\nReset Device to Apply Configuration\nClick OK to Reset",
                    "Applied Changes",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.OK)
                {
                    ResetDevice(); // Reset only after user confirms
                    MessageBox.Show("Close the configuration tab if changes in Network Mode\nIf IP changed Again add the device", " ", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseAction?.Invoke();
                }
                else
                {
                    MessageBox.Show("Configurations sent but device not reset. Changes will take effect after manual reset.",
                        "Configurations Applied", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // RS485 Configuration Methods
        private void SendRS485()
        {
            if (string.IsNullOrWhiteSpace(DeviceIp))
            {
                MessageBox.Show("Please enter a valid device IP address.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TcpClientHelper tcpHelper = null;
            try
            {
                tcpHelper = new TcpClientHelper(DeviceIp);
                if (!tcpHelper.Connect(DeviceIp))
                {
                    MessageBox.Show("Failed to connect to device for RS485 config.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] config = new byte[7];
                Buffer.BlockCopy(BitConverter.GetBytes(RS485.BaudRate), 0, config, 0, 4);
                config[4] = RS485.Parity;
                config[5] = RS485.DataBit;
                config[6] = RS485.StopBit;

                tcpHelper.SendCommand("SETRS485", config);
                //MessageBox.Show("RS485 configuration sent successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending RS485 configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                tcpHelper?.Disconnect();
            }
        }

        private void ReadRS485()
        {
            if (string.IsNullOrWhiteSpace(DeviceIp))
            {
                MessageBox.Show("Please enter a valid device IP address.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TcpClientHelper tcpHelper = null;
            try
            {
                tcpHelper = new TcpClientHelper(DeviceIp);
                if (!tcpHelper.Connect(DeviceIp))
                {
                    MessageBox.Show("Failed to connect to device for reading RS485 config.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                tcpHelper.SendCommand("GETRS485");
                var data = tcpHelper.ReadResponse(7);

                if (data.Length >= 7)
                {
                    RS485.BaudRate = BitConverter.ToUInt32(data, 0);
                    RS485.Parity = data[4];
                    RS485.DataBit = data[5];
                    RS485.StopBit = data[6];

                    OnPropertyChanged(nameof(RS485));
                    //MessageBox.Show("RS485 configuration read successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Invalid response from device.", "Read Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading RS485 configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                tcpHelper?.Disconnect();
            }
        }

        // Ethernet Configuration Methods
        private void SendEthernet()
        {
            if (string.IsNullOrWhiteSpace(DeviceIp))
            {
                MessageBox.Show("Please enter a valid device IP address.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TcpClientHelper tcpHelper = null;
            try
            {
                tcpHelper = new TcpClientHelper(DeviceIp);
                if (!tcpHelper.Connect(DeviceIp))
                {
                    MessageBox.Show("Failed to connect to device for Ethernet config.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] config = new byte[84];
                int offset = 0;

                // Static IP flag
                BitConverter.GetBytes(Ethernet.IsStatic ? 1 : 0).CopyTo(config, offset);
                offset += 4;

                // Helper function to copy strings
                void CopyString(string value, int length)
                {
                    var bytes = Encoding.ASCII.GetBytes((value ?? "").PadRight(length, '\0'));
                    Array.Copy(bytes, 0, config, offset, Math.Min(bytes.Length, length));
                    offset += length;
                }

                CopyString(Ethernet.IP, 16);
                CopyString(Ethernet.Gateway, 16);
                CopyString(Ethernet.Netmask, 16);
                CopyString(Ethernet.DnsMain, 16);
                CopyString(Ethernet.DnsBackup, 16);

                tcpHelper.SendCommand("SETNW", config);
                //MessageBox.Show("Ethernet configuration sent successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending Ethernet configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                tcpHelper?.Disconnect();
            }
        }

        private void ReadEthernet()
        {
            if (string.IsNullOrWhiteSpace(DeviceIp))
            {
                MessageBox.Show("Please enter a valid device IP address.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TcpClientHelper tcpHelper = null;
            try
            {
                tcpHelper = new TcpClientHelper(DeviceIp);
                if (!tcpHelper.Connect(DeviceIp))
                {
                    MessageBox.Show("Failed to connect to device for reading Ethernet config.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                tcpHelper.SendCommand("GETNW");
                var buffer = tcpHelper.ReadResponse(84);

                if (buffer.Length < 84)
                {
                    MessageBox.Show("Invalid response from device.", "Read Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int offset = 0;

                // Static IP flag
                Ethernet.IsStatic = BitConverter.ToInt32(buffer, offset) == 1;
                offset += 4;

                // Helper function to read strings
                string ReadString(int length)
                {
                    string result = Encoding.ASCII.GetString(buffer, offset, length).Trim('\0');
                    offset += length;
                    return result;
                }

                Ethernet.IP = ReadString(16);
                Ethernet.Gateway = ReadString(16);
                Ethernet.Netmask = ReadString(16);
                Ethernet.DnsMain = ReadString(16);
                Ethernet.DnsBackup = ReadString(16);

                OnPropertyChanged(nameof(Ethernet));
                //MessageBox.Show("Ethernet configuration read successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading Ethernet configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                tcpHelper?.Disconnect();
            }
        }

        private void SendGeneral()
        {
            if (string.IsNullOrWhiteSpace(DeviceIp))
            {
                MessageBox.Show("Please enter a valid device IP address.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 🚫 Restrict port 1502
            //if (General.Port == 1502)
            //{
            //    MessageBox.Show("You cannot apply 1502 port.", "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            TcpClientHelper tcpHelper = null;
            try
            {
                tcpHelper = new TcpClientHelper(DeviceIp);
                if (!tcpHelper.Connect(DeviceIp))
                {
                    MessageBox.Show("Failed to connect to device for General config.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] config = new byte[8];
                Buffer.BlockCopy(BitConverter.GetBytes(General.NetMode), 0, config, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(General.Port), 0, config, 4, 4);

                tcpHelper.SendCommand("SETGEN", config);

                // Add success message back but without the reset dialog
                //MessageBox.Show("General configuration sent successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending General configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                tcpHelper?.Disconnect();
            }
        }

        //Read General
        private void ReadGeneral()
        {
            if (string.IsNullOrWhiteSpace(DeviceIp))
            {
                MessageBox.Show("Please enter a valid device IP address.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TcpClientHelper tcpHelper = null;
            try
            {
                tcpHelper = new TcpClientHelper(DeviceIp);
                if (!tcpHelper.Connect(DeviceIp))
                {
                    MessageBox.Show("Failed to connect to device for reading General config.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                tcpHelper.SendCommand("GETGEN");
                var data = tcpHelper.ReadResponse(8);

                if (data.Length >= 8)
                {
                    General.NetMode = BitConverter.ToInt32(data, 0);
                    General.Port = BitConverter.ToInt32(data, 4);
                    OnPropertyChanged(nameof(General));
                    //MessageBox.Show("General configuration read successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Invalid response from device.", "Read Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading General configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                tcpHelper?.Disconnect();
            }
        }

        // WiFi Configuration Methods
        private void SendWifi()
        {
            if (string.IsNullOrWhiteSpace(DeviceIp))
            {
                MessageBox.Show("Please enter a valid device IP address.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _tcpHelper = new TcpClientHelper(DeviceIp);
                if (!_tcpHelper.Connect(DeviceIp))
                {
                    MessageBox.Show("Failed to connect to device.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] config = new byte[180];
                int offset = 0;

                // Helper function to copy strings with specific length
                void CopyString(string value, int length)
                {
                    var bytes = Encoding.ASCII.GetBytes((value ?? "").PadRight(length, '\0'));
                    Array.Copy(bytes, 0, config, offset, Math.Min(bytes.Length, length));
                    offset += length;
                }

                // SSID - 16 bytes
                CopyString(Wifi.SSID, 32);

                // Password - 64 byte
                CopyString(Wifi.Password, 64);

                // Is_static flag - 4 bytes
                BitConverter.GetBytes(Wifi.Is_static ? 1 : 0).CopyTo(config, offset);
                offset += 4;

                //IP - 16
                CopyString(Wifi.IP, 16);

                //Gateway - 16
                CopyString(Wifi.Gateway, 16);

                // Subnetmask - 16 bytes
                CopyString(Wifi.netmask, 16);

                // DNS_main - 16 bytes
                CopyString(Wifi.DNS_main, 16);

                // DNS_backup - 16 bytes
                CopyString(Wifi.DNS_backup, 16);

                _tcpHelper.SendCommand("SETWIFI", config);

                //MessageBox.Show("WiFi configuration sent successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _tcpHelper.Disconnect(); // Disconnect after user clicks OK on success message
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending WiFi configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _tcpHelper?.Disconnect(); // Disconnect on exception (with null check)
            }
        }

        private void CopyString(bool gateway, int v)
        {
            throw new NotImplementedException();
        }

        private void ReadWifi()
        {
            if (string.IsNullOrWhiteSpace(DeviceIp))
            {
                MessageBox.Show("Please enter a valid device IP address.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _tcpHelper = new TcpClientHelper(DeviceIp);
                if (!_tcpHelper.Connect(DeviceIp))
                {
                    MessageBox.Show("Failed to connect to device.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _tcpHelper.SendCommand("GETWIFI");
                var buffer = _tcpHelper.ReadResponse(180); // 132 bytes for WiFi config

                if (buffer.Length < 180)
                {
                    MessageBox.Show("Invalid response from device.", "Read Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _tcpHelper.Disconnect(); // Disconnect on error
                    return;
                }

                int offset = 0;

                // Helper function to read strings
                string ReadString(int length)
                {
                    string result = Encoding.ASCII.GetString(buffer, offset, length).Trim('\0');
                    offset += length;
                    return result;
                }

                // SSID - 16 bytes
                Wifi.SSID = ReadString(32);

                // Password - 64 bytes
                Wifi.Password = ReadString(64);

                // Is_static flag - 4 bytes
                Wifi.Is_static = BitConverter.ToInt32(buffer, offset) == 1;
                offset += 4;

                //IP - 16
                Wifi.IP = ReadString(16);

                //Gateway
                Wifi.Gateway = ReadString(16);

                // Subnetmask - 16 bytes
                Wifi.netmask = ReadString(16);

                // DNS_main - 16 bytes
                Wifi.DNS_main = ReadString(16);

                // DNS_backup - 16 bytes
                Wifi.DNS_backup = ReadString(16);

                OnPropertyChanged(nameof(Wifi));

                //MessageBox.Show("WiFi configuration read successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _tcpHelper.Disconnect(); // Disconnect after user clicks OK on success message
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading WiFi configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _tcpHelper?.Disconnect(); // Disconnect on exception (with null check)
            }
        }

        private void ResetDevice()
        {
            _tcpHelper = new TcpClientHelper(DeviceIp);
            if (!_tcpHelper.Connect(DeviceIp))
            {
                MessageBox.Show("Could not connect to device.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _tcpHelper.SendCommand("RST");

            _tcpHelper.Disconnect();

            //MessageBox.Show("Device reset success", "Reset Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}