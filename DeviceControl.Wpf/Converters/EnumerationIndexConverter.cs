using GigeVision.Core.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DeviceControl.Wpf.Converters
{
    public class EnumerationIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enumeration enumeration)
            {
                int index = 0;
                foreach (var enumValue in enumeration.Entry)
                {
                    if (enumeration.Register != null)
                    {
                        if (enumeration.Register.Value is IntSwissKnife intSwiss)
                            if (enumValue.Value == (uint)intSwiss.Value)
                                return index;

                        if (enumeration.Register.Value is uint uintValue)
                            if (enumValue.Value == uintValue)
                                return index;
                    }

                    index++;
                };
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}