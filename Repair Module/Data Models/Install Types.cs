using System;
using System.Collections.Generic;
using System.Text;

namespace Segway.Modules.SART.Repair
{
    /// <summary>Public Class - Install_Types</summary>
    public class Install_Types
    {
        /// <summary>Public Property - Code</summary>
        public String Code { get; set; }
        /// <summary>Public Property - Description</summary>
        public String Description { get; set; }

        /// <summary>Public Contructor - Install_Types</summary>
        public Install_Types() { }
        /// <summary>Contructor</summary>
        /// <param name="code">String</param>
        /// <param name="desc">String</param>
        public Install_Types(String code, String desc)
        {
            Code = code;
            Description = desc;
        }
    }
}
