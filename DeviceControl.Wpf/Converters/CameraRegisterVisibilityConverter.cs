using GigeVision.Core.Enums;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeviceControl.Wpf.Converters
{
    public class CameraRegisterVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is CameraRegisterVisibility cameraRegisterVisibility)
                if (values[1] is Enums.CameraRegisterVisibility cameraRegisterVisibility1)
                    if ((uint)cameraRegisterVisibility > (uint)cameraRegisterVisibility1)
                        return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}