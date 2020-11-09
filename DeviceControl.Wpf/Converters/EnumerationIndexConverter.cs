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
            if (value is CameraRegisterContainer cameraRegisterContainer)
            {
                if (cameraRegisterContainer.TypeValue is Enumeration enumeration)
                {
                    int index = 0;
                    foreach (var enumValue in enumeration.Entry)
                    {
                        if (cameraRegisterContainer.Value is IntSwissKnife intSwiss)
                        {
                            if (enumValue.Value == (uint)intSwiss.Value)
                                return index;
                        }
                        else if (cameraRegisterContainer.Value is uint uintValue)
                        {
                            if (enumValue.Value == uintValue)
                                return index;
                        }
                        else if (cameraRegisterContainer.Value is double doubleValue)
                        {
                            if (enumValue.Value == doubleValue)
                                return index;
                        }
                        else if (cameraRegisterContainer.Value is int intValue)
                        {
                            if (enumValue.Value == intValue)
                                return index;
                        }

                        index++;
                    }
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