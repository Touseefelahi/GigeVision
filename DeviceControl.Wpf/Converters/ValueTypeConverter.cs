using System;
using System.Globalization;
using System.Windows.Data;

namespace DeviceControl.Wpf.Converters
{
    public class ValueTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (value is CameraRegisterType cameraRegisterType)
            //{
            //    switch (cameraRegisterType)
            //    {
            //        case CameraRegisterType.Integer:
            //            return "TextBoxInteger";

            //        case CameraRegisterType.Float:
            //            return "TextBoxFloat";

            //        case CameraRegisterType.StringReg:
            //            return "TextBoxString";

            //        case CameraRegisterType.Enumeration:
            //            return "ComboBox";

            //        case CameraRegisterType.Command:
            //            return "Button";

            //        case CameraRegisterType.Boolean:
            //            return "Checkbox";

            //        default:
            //            break;
            //    }
            //}
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}