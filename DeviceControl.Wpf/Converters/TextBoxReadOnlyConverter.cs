using GenICam;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DeviceControl.Wpf.Converters
{
    public class TextBoxReadOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IRegister register)
                if (register.AccessMode != GenAccessMode.RO)
                    return true;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}