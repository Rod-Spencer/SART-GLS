using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;

namespace Segway.Modules.WorkOrder
{
    public class Serial_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is String)) return value;
            if (value == null) return value;
            if ((String)value == String.Empty) return "<Blank>";
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
