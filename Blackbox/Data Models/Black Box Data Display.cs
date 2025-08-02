using System;
using System.Collections.Generic;
using System.Text;

namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Data_Display</summary>
    public class Black_Box_Data_Display
    {
        /// <summary>Public Property - PropertyName</summary>
        public String PropertyName { get; set; }
        /// <summary>Public Property - Header</summary>
        public String Header { get; set; }
        /// <summary>Public Property - Width</summary>
        public Double Width { get; set; }
        /// <summary>Public Property - DataType</summary>
        public Type DataType { get; set; }
        /// <summary>Public Property - DisplayHex</summary>
        public Boolean DisplayHex { get; set; }

        /// <summary>Public Contructor - Black_Box_Data_Display</summary>
        public Black_Box_Data_Display() { }
        /// <summary>Contructor</summary>
        /// <param name="pName">String</param>
        /// <param name="head">String</param>
        /// <param name="width">Double</param>
        /// <param name="hex">Boolean</param>
        /// <param name="t">Type</param>
        public Black_Box_Data_Display(String pName, String head, Double width, Boolean hex = true, Type t = null)
        {
            PropertyName = pName;
            Header = head;
            Width = width;
            DataType = t == null ? typeof(short?) : t;
            DisplayHex = hex;
        }


        public override String ToString()
        {
            return $"{PropertyName} W:{Width}, T:{DataType}";
        }
    }
}
