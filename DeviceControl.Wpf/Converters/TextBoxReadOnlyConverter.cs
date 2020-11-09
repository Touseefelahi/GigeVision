using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DeviceControl.Wpf.Converters
{
    public class TextBoxReadOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CameraRegister cameraRegister)
            {
                if (cameraRegister.AccessMode == CameraRegisterAccessMode.RO)
                    return false;
            }
            else if (value is null)
                return false;

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}