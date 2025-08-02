using Segway.Service.Common;
using Segway.Service.Helper;
using System;
using System.IO;
using System.Windows.Data;

namespace Segway.Modules.WorkOrder
{
    /// <summary>Public Class - Image_Converter</summary>
    public class Image_Converter : IValueConverter
    {
        /// <summary>Public Method - Convert</summary>
        /// <param name="value">object</param>
        /// <param name="targetType">Type</param>
        /// <param name="parameter">object</param>
        /// <param name="culture">System.Globalization.CultureInfo</param>
        /// <returns>object</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is String)) return null;
            String filename = value.ToString();
            if (String.IsNullOrEmpty(filename) == true) return null;
            FileInfo fi = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "Cache", filename));
            if (fi.Directory.Exists == false)
            {
                fi.Directory.Create();
                return null;
            }
            if (fi.Exists == false) return null;
            return Image_Helper.ImageFromFile(fi.FullName);
        }

        /// <summary>Public Method - ConvertBack</summary>
        /// <param name="value">object</param>
        /// <param name="targetType">Type</param>
        /// <param name="parameter">object</param>
        /// <param name="culture">System.Globalization.CultureInfo</param>
        /// <returns>object</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
