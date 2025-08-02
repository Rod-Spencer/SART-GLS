using Segway.Service.CAN;
using System;



namespace Segway.Modules.Diagnostics_Helper
{
    public class FrameCompletedEventArgs : EventArgs
    {
        public CAN_CU_Sides Side { get; private set; }
        public CUFrameData Data { get; private set; }

        public FrameCompletedEventArgs(CAN_CU_Sides id, CUFrameData data)
        {
            Data = data;
            Side = id;
        }

        private FrameCompletedEventArgs(CAN_CU_Sides side)
        {
            Side = side;
        }

        public override String ToString()
        {
            return String.Format("{0} - {1}", Side, Data);
        }

        public FrameCompletedEventArgs Copy()
        {
            FrameCompletedEventArgs c = new FrameCompletedEventArgs(Side);
            c.Data = new CUFrameData(this.Data.Data);
            return c;
        }
    }


}
