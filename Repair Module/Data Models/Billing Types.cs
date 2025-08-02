using System;

namespace Segway.Modules.SART.Repair
{
    /// <summary>Public Class - Billing_Types</summary>
    public class Billing_Types
    {
        /// <summary>Public Property - Code</summary>
        public String Code { get; set; }
        /// <summary>Public Property - Description</summary>
        public String Description { get; set; }

        /// <summary>Public Contructor - Billing_Types</summary>
        public Billing_Types() { }


        /// <summary>Contructor</summary>
        /// <param name="code">String</param>
        /// <param name="desc">String</param>
        public Billing_Types(String code, String desc)
        {
            Code = code;
            Description = desc;
        }
    }
}
