using GenICam;
using System;
using System.Globalization;
using System.Linq;
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
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
                return BitConverter.ToInt32(IPAddress.Parse(stringValue).GetAddressBytes().ToArray());

            return null;
        }
    }
}