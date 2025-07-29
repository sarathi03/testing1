using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace testing1.Models
{
    public class DeviceManagementModel : INotifyPropertyChanged
    {
        private int _deviceNumber;
        private DeviceInfo _deviceInfo;
        private DateTime _lastModified = DateTime.Now;
        private DateTime _createdDate = DateTime.Now;
        private DateTime _lastUpdated = DateTime.Now;

        public DeviceManagementModel()
        {
            DeviceInfo = new DeviceInfo();
            CreatedDate = DateTime.Now;
            LastUpdated = DateTime.Now;
        }

        public DeviceManagementModel(DeviceInfo deviceInfo)
        {
            DeviceInfo = deviceInfo ?? new DeviceInfo();
            CreatedDate = DateTime.Now;
            LastUpdated = DateTime.Now;
        }

        public int DeviceNumber
        {
            get => _deviceNumber;
            set
            {
                if (_deviceNumber != value)
                {
                    _deviceNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public DeviceInfo DeviceInfo
        {
            get => _deviceInfo;
            set
            {
                if (_deviceInfo != value)
                {
                    _deviceInfo = value;
                    OnPropertyChanged();
                    UpdateLastModified();
                }
            }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            private set
            {
                if (_lastModified != value)
                {
                    _lastModified = value;
                    OnPropertyChanged();
                }
            }
        }

        // FIXED: Added missing CreatedDate property
        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                if (_createdDate != value)
                {
                    _createdDate = value;
                    OnPropertyChanged();
                }
            }
        }

        // FIXED: Added missing LastUpdated property
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                if (_lastUpdated != value)
                {
                    _lastUpdated = value;
                    OnPropertyChanged();
                }
            }
        }

        public void UpdateLastModified()
        {
            LastModified = DateTime.Now;
            LastUpdated = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}