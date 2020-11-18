using GenICam;
using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Windows.Data;

namespace DeviceControl.Wpf.Converters
{
    public class RepresentaionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var ip = new IPAddress((Int64)value);
                return ip.ToString();
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is null)
                    return 0;

                return value;
            }
            catch
            {
                return null;
            }
        }
    }
}