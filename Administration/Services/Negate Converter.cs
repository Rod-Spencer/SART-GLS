using System;
using System.Globalization;
using System.Windows.Data;

namespace Segway.Modules.Administration
{
    /// <summary>Returns the negation of a Boolean</summary>
    public class Negate_Converter : IValueConverter
    {

        /// <summary>Public Method - Convert</summary>
        /// <param name="value">object</param>
        /// <param name="targetType">Type</param>
        /// <param name="parameter">object</param>
        /// <param name="culture">CultureInfo</param>
        /// <returns>object</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Boolean)
            {
                return !((Boolean)value);
            }
            return value;
        }

        /// <summary>Public Method - ConvertBack</summary>
        /// <param name="value">object</param>
        /// <param name="targetType">Type</param>
        /// <param name="parameter">object</param>
        /// <param name="culture">CultureInfo</param>
        /// <returns>object</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Boolean)
            {
                return !((Boolean)value);
            }
            return value;
        }
    }
}
