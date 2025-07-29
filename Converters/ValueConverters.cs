using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using testing1.Models;

namespace testing1.Converters
{
    /// <summary>
    /// Converts DeviceStatus enum to Color for status indicator
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceStatus status)
            {
                return status switch
                {
                    DeviceStatus.Connected => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                    DeviceStatus.NotConnected => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
                    DeviceStatus.Offline => new SolidColorBrush(Color.FromRgb(66, 66, 66)), // Dark Grey
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Light Grey
                };
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }





    /// <summary>
    /// Inverts boolean values
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts DateTime to formatted string
    /// </summary>
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                string format = parameter?.ToString() ?? "yyyy-MM-dd HH:mm:ss";
                return dateTime.ToString(format);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && DateTime.TryParse(stringValue, out DateTime result))
            {
                return result;
            }
            return DateTime.MinValue;
        }
    }
}