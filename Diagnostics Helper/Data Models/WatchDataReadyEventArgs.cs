using Segway.Service.CAN;
using System;
using System.Collections.Generic;


namespace Segway.Modules.Diagnostics_Helper
{
    public class WatchDataReadyEventArgs : EventArgs
    {
        public CAN_CU_Sides Side
        {
            get;
            private set;
        }

        public Dictionary<String, Int16> WatchVariables
        {
            get;
            private set;
        }

        public WatchDataReadyEventArgs(CAN_CU_Sides side, Dictionary<String, Int16> vars)
        {
            Side = side;
            WatchVariables = vars;
        }
    }
}
