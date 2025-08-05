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

namespace testing1.ViewModels
{
    public class DeviceConfigViewModel : INotifyPropertyChanged
    {
        private TcpClientHelper _tcpHelper;

        public RS485Settings RS485 { get; set; } = new();
        public EthernetSettings Ethernet { get; set; } = new();

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
        public ICommand ResetCommand { get; }

        public DeviceConfigViewModel()
        {
            SendRS485Command = new RelayCommand(SendRS485);
            ReadRS485Command = new RelayCommand(ReadRS485);
            SendEthernetCommand = new RelayCommand(SendEthernet);
            ReadEthernetCommand = new RelayCommand(ReadEthernet);
            ResetCommand = new RelayCommand(ResetDevice);
        }

        // RS485 Configuration Methods
        private void SendRS485()
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

                byte[] config = new byte[7];
                Buffer.BlockCopy(BitConverter.GetBytes(RS485.BaudRate), 0, config, 0, 4);
                config[4] = RS485.Parity;
                config[5] = RS485.DataBit;
                config[6] = RS485.StopBit;

                _tcpHelper.SendCommand("SETRS485", config);
                _tcpHelper.Disconnect();
                
                MessageBox.Show("RS485 configuration sent successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending RS485 configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReadRS485()
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

                _tcpHelper.SendCommand("GETRS485");
                var data = _tcpHelper.ReadResponse(7);
                
                if (data.Length >= 7)
                {
                    RS485.BaudRate = BitConverter.ToUInt32(data, 0);
                    RS485.Parity = data[4];
                    RS485.DataBit = data[5];
                    RS485.StopBit = data[6];
                    
                    OnPropertyChanged(nameof(RS485));
                    MessageBox.Show("RS485 configuration read successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Invalid response from device.", "Read Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                _tcpHelper.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading RS485 configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            try
            {
                _tcpHelper = new TcpClientHelper(DeviceIp);
                if (!_tcpHelper.Connect(DeviceIp))
                {
                    MessageBox.Show("Failed to connect to device.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                
                // Port
                //BitConverter.GetBytes(Ethernet.Port).CopyTo(config, offset);
                
                _tcpHelper.SendCommand("SETNW", config);
                _tcpHelper.Disconnect();
                
                MessageBox.Show("Ethernet configuration sent successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending Ethernet configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReadEthernet()
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

                _tcpHelper.SendCommand("GETNW");
                var buffer = _tcpHelper.ReadResponse(84);

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
                //Ethernet.Port = (ushort)BitConverter.ToInt32(buffer, offset);

                OnPropertyChanged(nameof(Ethernet));
                _tcpHelper.Disconnect();

                MessageBox.Show("Ethernet configuration read successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading Ethernet configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            MessageBox.Show("Device reset success", "Reset Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
} 