using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace testing1.Models
{
    public class DeviceInfo : INotifyPropertyChanged
    {
        private string _deviceName;
        private string _ip;
        private int _port;
        private string _mac;
        private DeviceStatus _status = DeviceStatus.Unknown;
        private string _location;
        private DateTime _lastSeen = DateTime.Now;

        public string DeviceName
        {
            get => _deviceName;
            set
            {
                if (_deviceName != value)
                {
                    _deviceName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string IP
        {
            get => _ip;
            set
            {
                if (_ip != value)
                {
                    _ip = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Port
        {
            get => _port;
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MAC
        {
            get => _mac;
            set
            {
                if (_mac != value)
                {
                    _mac = value;
                    OnPropertyChanged();
                }
            }
        }

        public DeviceStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Location
        {
            get => _location;
            set
            {
                if (_location != value)
                {
                    _location = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastSeen
        {
            get => _lastSeen;
            set
            {
                if (_lastSeen != value)
                {
                    _lastSeen = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum DeviceStatus
    {
        Unknown,
        Discovered,
        Online,
        Offline,
        Connected,
        NotConnected,
        Connecting
    }
}