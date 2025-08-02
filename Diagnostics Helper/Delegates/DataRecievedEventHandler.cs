using System;
using System.Collections.Generic;

using Segway.Service.CAN;


namespace Segway.Modules.Diagnostics_Helper
{
    public delegate void DataRecievedEventHandler(CAN_CU_Sides side, Dictionary<String, Int16> varsB);
}
