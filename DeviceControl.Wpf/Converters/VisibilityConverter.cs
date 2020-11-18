using DeviceControl.Wpf.Enums;
using GenICam;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeviceControl.Wpf.Converters
{
    public class VisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is GenVisibility visibility)
                if (values[1] is DeviceControlVisibility visibility1)
                    if ((uint)visibility > (uint)visibility1)
                        return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}