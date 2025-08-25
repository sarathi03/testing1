using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace testing1.Models
{
    public class SelectedDeviceModel : INotifyPropertyChanged
    {
        private DeviceInfo _deviceInfo;
        private bool _isSelected;
        private DateTime _selectedTime = DateTime.Now;

        public SelectedDeviceModel()
        {
            DeviceInfo = new DeviceInfo();
        }

        public SelectedDeviceModel(DeviceInfo deviceInfo)
        {
            DeviceInfo = deviceInfo ?? new DeviceInfo();
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
                }
            }
        }

        //public bool IsSelected
        //{
        //    get => _isSelected;
        //    set
        //    {
        //        if (_isSelected != value)
        //        {
        //            _isSelected = value;
        //            OnPropertyChanged();
        //            if (value)
        //            {
        //                SelectedTime = DateTime.Now;
        //            }
        //        }
        //    }
        //}

        //public DateTime SelectedTime
        //{
        //    get => _selectedTime;
        //    private set
        //    {
        //        if (_selectedTime != value)
        //        {
        //            _selectedTime = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}