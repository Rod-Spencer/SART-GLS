using NLog;
using Segway.Service.CAN;
using Segway.Service.LoggerHelper;
using Segway.Service.Tools.CAN2;
using System;
using System.Linq;

namespace Segway.Modules.Diagnostics_Helper
{
    public class CUFrame
    {
        public CUFrameData FrameData { get; set; }
        public CAN_CU_Sides Side { get; private set; }
        public CUFrameMessageStatus[] Messages { get; set; }

        public event FrameCompletedEventHandler FrameCompleted;


        private static Logger logger = Logger_Helper.GetCurrentLogger();



        public CUFrame(CAN_CU_Sides id)
        {
            Side = id;
            FrameData = new CUFrameData();
            Messages = new CUFrameMessageStatus[5];

            for (int x = 0; x < Messages.Length; x++)
            {
                Messages[x] = new CUFrameMessageStatus();
            }

            if (id == CAN_CU_Sides.A)
            {
                Messages[0].MessageID = Message_IDs.CID_CUA_HEARTBEAT;
                Messages[1].MessageID = Message_IDs.CID_CUA_DATA1;
                Messages[2].MessageID = Message_IDs.CID_CUA_DATA2;
                Messages[3].MessageID = Message_IDs.CID_CUA_DATA3;
                Messages[4].MessageID = Message_IDs.CID_CUA_DATA4;
            }
            else
            {
                Messages[0].MessageID = Message_IDs.CID_CUB_HEARTBEAT;
                Messages[1].MessageID = Message_IDs.CID_CUB_DATA1;
                Messages[2].MessageID = Message_IDs.CID_CUB_DATA2;
                Messages[3].MessageID = Message_IDs.CID_CUB_DATA3;
                Messages[4].MessageID = Message_IDs.CID_CUB_DATA4;
            }
        }

        public Boolean IsCompleted()
        {
            foreach (CUFrameMessageStatus msg in Messages)
            {
                if (msg.IsReceived == false) return false;
            }

            return true;
        }



        public Boolean IsMember(Message_IDs msgId)
        {
            foreach (CUFrameMessageStatus msg in Messages)
            {
                if (msg.MessageID == msgId) return true;
            }

            return false;
        }

        public void Clear()
        {
            FrameData = new CUFrameData();
            for (Int32 msgNumber = 0; msgNumber < Messages.Count(); msgNumber++)
            {
                Messages[msgNumber].IsReceived = false;
            }
        }

        public void CompleteFrameFromCANMessage(CAN2_Message canmsg)
        {
            if (canmsg == null) return;
            if (IsMember(canmsg.ID) == false) return;

            logger.Trace("Processing: {0}", canmsg);

            if ((canmsg.ID == Message_IDs.CID_CUA_HEARTBEAT) || (canmsg.ID == Message_IDs.CID_CUB_HEARTBEAT))
            {
                //logger.Trace("Received a Heartbeat: {0}", canmsg);
                Clear();
            }

            if (UpdateFrameData(canmsg) == false) return;
            if (IsCompleted())
            {
                if (FrameCompleted != null) FrameCompleted(new FrameCompletedEventArgs(Side, FrameData));
                //Clear();
            }

            return;
        }

        private Boolean UpdateFrameData(CAN2_Message canmsg)
        {
            if (canmsg == null) return false;
            //logger.Trace("Received: {0}", canmsg);


            switch (canmsg.ID)
            {
                case Message_IDs.CID_CUA_HEARTBEAT:
                case Message_IDs.CID_CUB_HEARTBEAT:
                    Messages[0].IsReceived = true;
                    break;

                case Message_IDs.CID_CUA_DATA1:
                case Message_IDs.CID_CUB_DATA1:
                    FrameData[0] = (Int16)canmsg.Data[0];
                    FrameData[1] = (Int16)canmsg.Data[1];
                    FrameData[2] = (Int16)canmsg.Data[2];
                    FrameData[3] = (Int16)canmsg.Data[3];
                    Messages[1].IsReceived = true;
                    break;
                case Message_IDs.CID_CUA_DATA2:
                case Message_IDs.CID_CUB_DATA2:
                    FrameData[4] = (Int16)canmsg.Data[0];
                    FrameData[5] = (Int16)canmsg.Data[1];
                    FrameData[6] = (Int16)canmsg.Data[2];
                    FrameData[7] = (Int16)canmsg.Data[3];
                    Messages[2].IsReceived = true;
                    break;
                case Message_IDs.CID_CUA_DATA3:
                case Message_IDs.CID_CUB_DATA3:
                    FrameData[8] = (Int16)canmsg.Data[0];
                    FrameData[9] = (Int16)canmsg.Data[1];
                    FrameData[10] = (Int16)canmsg.Data[2];
                    FrameData[11] = (Int16)canmsg.Data[3];
                    Messages[3].IsReceived = true;
                    break;
                case Message_IDs.CID_CUA_DATA4:
                case Message_IDs.CID_CUB_DATA4:
                    FrameData[12] = (Int16)canmsg.Data[0];
                    FrameData[13] = (Int16)canmsg.Data[1];
                    FrameData[14] = (Int16)canmsg.Data[2];
                    FrameData[15] = (Int16)canmsg.Data[3];
                    Messages[4].IsReceived = true;
                    break;
            }

            return true;
        }
    }
}
