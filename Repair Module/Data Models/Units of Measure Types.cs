using System;
using System.Collections.Generic;
using System.Text;

namespace Segway.Modules.SART.Repair
{
    /// <summary>Public Class - Units_of_Measure_Types</summary>
    public class Units_of_Measure_Types
    {
        /// <summary>Public Property - Code</summary>
        public String Code { get; set; }
        /// <summary>Public Property - Description</summary>
        public String Description { get; set; }

        /// <summary>Public Contructor - Units_of_Measure_Types</summary>
        public Units_of_Measure_Types() { }
        /// <summary>Contructor</summary>
        /// <param name="code">String</param>
        /// <param name="desc">String</param>
        public Units_of_Measure_Types(String code, String desc)
        {
            Code = code;
            Description = desc;
        }
    }
}
