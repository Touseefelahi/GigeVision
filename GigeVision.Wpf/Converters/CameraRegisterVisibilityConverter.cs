using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using GigeVision.Wpf.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace GigeVision.Wpf.Converters
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