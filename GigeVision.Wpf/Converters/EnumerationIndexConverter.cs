using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace GigeVision.Wpf.Converters
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
                    if (enumeration.Register.Value != null)
                        if (enumValue.Value == (uint)enumeration.Register.Value)
                            return index;

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