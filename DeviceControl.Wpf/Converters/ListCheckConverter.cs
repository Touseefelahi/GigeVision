using DeviceControl.Wpf.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace DeviceControl.Wpf.Converters
{
    public class ListCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Dictionary<string, uint> dictionaryValue)
            {
                if (dictionaryValue.Count > 0)
                    return true;
            }
            else if (value is List<CameraRegisterDTO> dtoValue)
            {
                if (dtoValue.Count > 0)
                    return true;
            }
            else if (value is ObservableCollection<CameraRegisterGroupDTO> groupDtoValue)
            {
                if (groupDtoValue.Count > 0)
                    return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}