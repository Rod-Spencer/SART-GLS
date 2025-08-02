using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Segway.Modules.SART.Repair
{
    /// <summary>Public Class - Location_Types</summary>
    public class Location_Types
    {
        /// <summary>Public Property - Code</summary>
        public String Code { get; set; }


        /// <summary>Public Property - Description</summary>
        public String Description { get; set; }


        /// <summary>Constructor - Location_Types</summary>
        public Location_Types() { }


        /// <summary>Contructor</summary>
        /// <param name="code">String</param>
        /// <param name="desc">String</param>
        public Location_Types(String code, String desc)
        {
            Code = code;
            Description = desc;
        }

    }
}
