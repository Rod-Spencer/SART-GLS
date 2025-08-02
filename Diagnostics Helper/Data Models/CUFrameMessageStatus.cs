using Segway.Service.CAN;
using System;


namespace Segway.Modules.Diagnostics_Helper
{
    public class CUFrameMessageStatus
    {
        //public Int16 MessageID { get; set; }
        public Message_IDs MessageID { get; set; }
        public Boolean IsReceived { get; set; }


        public CUFrameMessageStatus()
        {
            MessageID = 0;
            IsReceived = false;
        }
    }
}
