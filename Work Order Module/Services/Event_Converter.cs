using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Segway.Modules.WorkOrder
{
    public class Event_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Int32)
            {
                Int32 id = (Int32)value;
                if (id > 0) return Brushes.DarkMagenta;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
