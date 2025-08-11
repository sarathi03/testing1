using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace testing1.Models
{
    public class DeviceConfig : INotifyPropertyChanged
    {
        private DeviceInfo _deviceInfo;
        private DateTime _createdDate;
        private DateTime _lastModified;
        private int _deviceNumber;
        private string _configVersion;

        public DeviceInfo DeviceInfo
        {
            get => _deviceInfo;
            set
            {
                _deviceInfo = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set
            {
                _lastModified = value;
                OnPropertyChanged();
            }
        }

        public int DeviceNumber
        {
            get => _deviceNumber;
            set
            {
                _deviceNumber = value;
                OnPropertyChanged();
            }
        }

        public string ConfigVersion
        {
            get => _configVersion;
            set
            {
                _configVersion = value;
                OnPropertyChanged();
            }
        }

        public DeviceConfig()
        {
            CreatedDate = DateTime.Now;
            LastModified = DateTime.Now;
            ConfigVersion = "1.0";
        }

        public DeviceConfig(DeviceInfo deviceInfo) : this()
        {
            DeviceInfo = deviceInfo;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class EnhancedDeviceConfig : DeviceConfig
    {
        // Additional properties specific to enhanced configuration
        private Dictionary<string, object> _advancedSettings;
        private bool _isAdvancedMode;

        public Dictionary<string, object> AdvancedSettings
        {
            get => _advancedSettings;
            set
            {
                _advancedSettings = value;
                OnPropertyChanged();
            }
        }

        public bool IsAdvancedMode
        {
            get => _isAdvancedMode;
            set
            {
                _isAdvancedMode = value;
                OnPropertyChanged();
            }
        }
        
        // Default constructor
        public EnhancedDeviceConfig() : base()
        {
            AdvancedSettings = new Dictionary<string, object>();
            IsAdvancedMode = false;
        }

        // Constructor with DeviceInfo
        public EnhancedDeviceConfig(DeviceInfo deviceInfo) : base(deviceInfo)
        {
            AdvancedSettings = new Dictionary<string, object>();
            IsAdvancedMode = false;
        }
    }
}