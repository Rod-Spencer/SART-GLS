using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Segway.Modules.SART.Repair
{
    /// <summary>Public Class - Change_Types</summary>
    public class Change_Types
    {
        /// <summary>Public Property - Code</summary>
        public String Code { get; set; }

        /// <summary>Public Property - Change</summary>
        public String Change { get; set; }


        /// <summary>Public Contructor - Change_Types</summary>
        public Change_Types() { }

        /// <summary>Contructor</summary>
        /// <param name="code">String</param>
        /// <param name="change">String</param>
        public Change_Types(String code, String change)
        {
            Code = code;
            Change = change;
        }
    }
}
