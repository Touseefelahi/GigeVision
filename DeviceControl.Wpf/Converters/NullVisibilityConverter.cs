using DeviceControl.Wpf.DTO;
using GenICam;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeviceControl.Wpf.Converters
{
    public class NullVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}