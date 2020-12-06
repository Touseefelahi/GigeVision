using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace GigeVision.Wpf.Converters
{
    internal class CheckBoxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is uint doubleValue)
            {
                if (doubleValue == 0)
                    return false;
                if (doubleValue == 1)
                    return true;
            }

            if (value is bool booleanValue)
            {
                if (booleanValue)
                    return (uint)1;
                if (!booleanValue)
                    return (uint)0;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}