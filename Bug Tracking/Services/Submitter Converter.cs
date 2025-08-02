using System;
using System.Globalization;
using System.Windows.Data;

namespace Segway.Service.Bugs
{
    /// <summary>Public Class - Submitter_Converter</summary>
    public class Submitter_Converter : IValueConverter
    {
        /// <summary>Public Method - Convert</summary>
        /// <param name="value">object</param>
        /// <param name="targetType">Type</param>
        /// <param name="parameter">object</param>
        /// <param name="culture">CultureInfo</param>
        /// <returns>object</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value is Int32) == false) return null;
            int submitterID = (Int32)value;
            return Bug_Detail_ViewModel.Get_User_Profile_Name(submitterID);
        }

        /// <summary>Public Method - ConvertBack</summary>
        /// <param name="value">object</param>
        /// <param name="targetType">Type</param>
        /// <param name="parameter">object</param>
        /// <param name="culture">CultureInfo</param>
        /// <returns>object</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
