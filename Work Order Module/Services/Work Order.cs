using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Segway.SART.Objects;

namespace Segway.Modules.WorkOrder
{
    public class Work_Order : Work_Order_Interface
    {
        public String Work_Order_Number { get; set; }
        public String PT_Serial_Number { get; set; }
        public WorkOrderStatuses Work_Order_Status { get; set; }
        public Boolean Start_Config_Exists { get; set; }
        public Boolean Start_Config_Override { get; set; }

        public Work_Order()
        {
            this.PT_Serial_Number = String.Empty;
            this.Work_Order_Number = String.Empty;
            this.Work_Order_Status = WorkOrderStatuses.Not_Defined;
        }
    }

}

