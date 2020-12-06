using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using GigeVision.Wpf.DTO;
using GigeVision.Wpf.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace GigeVision.Wpf.Converters
{
    public class ValueTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CameraRegisterType cameraRegisterType)
            {
                switch (cameraRegisterType)
                {
                    case CameraRegisterType.Integer:
                        return "TextBoxInteger";

                    case CameraRegisterType.Float:
                        return "TextBox";

                    case CameraRegisterType.StringReg:
                        return "TextBoxString";

                    case CameraRegisterType.Enumeration:
                        return "ComboBox";

                    case CameraRegisterType.Command:
                        return "Button";

                    case CameraRegisterType.Boolean:
                        return "Checkbox";

                    default:
                        break;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}