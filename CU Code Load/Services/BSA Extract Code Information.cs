using NLog;
using Segway.Modules.SART_Infrastructure;
using Segway.SART.Objects;
using Segway.Service.CAN;
using Segway.Service.LoggerHelper;
using Segway.Service.Tools.CAN2;
using Segway.Service.Tools.CAN2.Messages;
using Segway.Service.Tools.COFF;
using System;
using System.Threading;

namespace Segway.Modules.SART.CodeLoad
{
    /// <summary>Public Class - BSA_Extract</summary>
    public class BSA_Extract
    {
        private static Logger logger = Logger_Helper.GetCurrentLogger();


        /// <summary>Public Static Method - Code_Information</summary>
        /// <param name="obj">SART_Event_Object</param>
        /// <returns>Boolean</returns>
        public static Boolean Code_Information(SART_Event_Object obj)
        {
            Wake wake = null;
            CAN2 can = null;
            try
            {
                ////////////////////////////////////////////////////////////////////////////
                // Establishing CAN connection
                logger.Debug("Establishing CAN connection");
                can = SART_Common.EstablishConnection(obj.ID);
                // Establishing CAN connection
                ////////////////////////////////////////////////////////////////////////////



                ////////////////////////////////////////////////////////////////////////////
                // Send a WAKE - Start
                SART_Common.Start_Process(CAN_Processes.Wake_Start_A_B);
                wake = new Wake(CAN_CU_Sides.Both);
                // Sending WAKE
                if (wake.Send_Start(can) == false)
                {
                    SART_Common.End_Process(CAN_Processes.Wake_Start_A_B, false);
                    return false;
                }
                SART_Common.End_Process(CAN_Processes.Wake_Start_A_B, true);
                // Send a WAKE - Start
                ////////////////////////////////////////////////////////////////////////////
                if (CAN2_Commands.Continue_Processing == false) return false;

                Thread.Sleep(CAN2_Commands.Delay_Wake_Start);


                ////////////////////////////////////////////////////////////////////////////
                // Wait for Heartbeat
                DateTime timeout = DateTime.Now.AddMilliseconds(CAN2_Commands.Timeout_Heartbeat);
                logger.Debug("Must see Heartbeat for Side-A/B within {0}ms", CAN2_Commands.Timeout_Heartbeat);
                Boolean HeartBeatA = false;
                Boolean HeartBeatB = false;
                while ((DateTime.Now < timeout) && (((HeartBeatA == true) && (HeartBeatB == true)) == false))
                {
                    if (HeartBeatA == false)
                    {
                        if (can.WaitFor(Message_IDs.CID_CUA_HEARTBEAT, CAN_CU_Sides.A, 50) != null)
                        {
                            logger.Debug("Communication established with CU-A");
                            HeartBeatA = true;
                        }
                    }
                    if (HeartBeatB == false)
                    {
                        if (can.WaitFor(Message_IDs.CID_CUB_HEARTBEAT, CAN_CU_Sides.B, 50) != null)
                        {
                            logger.Debug("Communication established with CU-B");
                            HeartBeatB = true;
                        }
                    }
                }

                if (HeartBeatA == false) logger.Error("Unable to establish communication with CU-A");
                if (HeartBeatB == false) logger.Error("Unable to establish communication with CU-B");
                if ((HeartBeatA == false) || (HeartBeatB == false)) return false;
                // Wait for Heartbeat
                ////////////////////////////////////////////////////////////////////////////


                ////////////////////////////////////////////////////////////////////////////
                // Enter Diagnostic Mode
                logger.Debug("Unlocking diagnostic mode");
                SART_Common.Start_Process(CAN_Processes.Entering_Diagnostic_Mode);
                if (CU_Unlock.Unlock_Diagnostic(can, CAN_CU_Sides.A, CAN2_Commands.Diagnostic_Unlock_Code) == false)
                {
                    logger.Error("Failed to unlock diagnostic mode: Side-A");
                    SART_Common.End_Process(CAN_Processes.Entering_Diagnostic_Mode, false);
                }
                if (CU_Unlock.Unlock_Diagnostic(can, CAN_CU_Sides.B, CAN2_Commands.Diagnostic_Unlock_Code) == false)
                {
                    logger.Error("Failed to unlock diagnostic mode: Side-B");
                    SART_Common.End_Process(CAN_Processes.Entering_Diagnostic_Mode, false);
                }
                SART_Common.End_Process(CAN_Processes.Entering_Diagnostic_Mode, true);
                Thread.Sleep(10);
                // Enter Diagnostic Mode
                ////////////////////////////////////////////////////////////////////////////

                //for (int x = 0; x < 2; x++)
                //{
                //    CU_Enter_Motor_Cmd_Mode.Send(can, side);
                //    Thread.Sleep(50);
                //}

                //if (COFF_File.Load(CAN2_Commands.CU_COFF.Data) == false)
                //{
                //    //aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                //    return false;
                //}


                SART_Common.Start_Process(CAN_Processes.Reading_BSA_SW_Version);
                ////////////////////////////////////////////////////////////////////////////
                // Setup Watch Variables
                String variable = "bsa_software_version";
                int address = (int)COFF_File.ResolveValue(variable);
                if (address <= 0)
                {
                    String ErrorMessage = String.Format("Failed to resolve symbol '{0}'", variable);
                    logger.Error(ErrorMessage);
                    SART_Common.End_Process(CAN_Processes.Reading_BSA_SW_Version, false, ErrorMessage);
                    return false;
                }
                logger.Debug("Symbol '{0}' resolved. Address: {1} ({1:X4})", variable, address);
                if (CU_Set_Watch.Send_CU_Set_Watch(can, 0, address) == false)
                {
                    String ErrorMessage = String.Format("Failed to set watch '{0}'", variable);
                    logger.Error(ErrorMessage);
                    SART_Common.End_Process(CAN_Processes.Reading_BSA_SW_Version, false, ErrorMessage);
                    return false;
                }
                if (CU_Set_Watch.Send_CU_Set_Watch(can, 1, address + 1) == false)
                {
                    String ErrorMessage = String.Format("Failed to set watch 'build_count'");
                    logger.Error(ErrorMessage);
                    SART_Common.End_Process(CAN_Processes.Reading_BSA_SW_Version, false, ErrorMessage);
                    return false;
                }
                // Setup Watch Variables
                ////////////////////////////////////////////////////////////////////////////

                ////////////////////////////////////////////////////////////////////////////
                // Reading Watch Variables
                UInt16 versionA = 0;
                UInt16 buildA = 0;
                UInt16 versionB = 0;
                UInt16 buildB = 0;
                timeout = DateTime.Now.AddMilliseconds(CAN2_Commands.Timeout_Watch_Variables);
                while (DateTime.Now < timeout)
                {
                    if ((versionA != 0) && (buildA != 0) && (versionB != 0) && (buildB != 0)) break;

                    if ((versionA == 0) || (buildA == 0))
                    {
                        var msg = can.WaitFor(Message_IDs.CID_CUA_DATA1, CAN_CU_Sides.A, 50);
                        if (msg != null)
                        {
                            versionA = msg.Data[0];
                            buildA = msg.Data[1];
                        }
                    }

                    if ((versionB == 0) || (buildB == 0))
                    {
                        var msg = can.WaitFor(Message_IDs.CID_CUB_DATA1, CAN_CU_Sides.B, 50);
                        if (msg != null)
                        {
                            versionB = msg.Data[0];
                            buildB = msg.Data[1];
                        }
                    }
                }

                String msgStr = String.Format("Side-A - BSA SW Ver:{0} (x{0:X4}), Build: {1} (x{1:X4}),  Side-B - BSA SW Ver:{2} (x{2:X4}), Build: {3} (x{3:X4})", versionA, buildA, versionB, buildB);
                if ((versionA != versionB) || (buildA != buildB))
                {
                    logger.Warn(msgStr);
                    SART_Common.End_Process(CAN_Processes.Reading_BSA_SW_Version, false, msgStr);
                    return false;
                }

                logger.Debug(msgStr);
                SART_Common.End_Process(CAN_Processes.Reading_BSA_SW_Version, true, msgStr);
                return true;


#if DoNotDo

                //Application_Helper.DoEvents();
                /////////////////////////////////////////////////////////////////////////////////////////////////
                ///// Send a WAKE - Start

                SART_Common.Start_Process(CAN_Processes.Wake_Start_A_B);
                wake = new Wake(CAN_CU_Sides.Both);
                // Sending WAKE
                if (wake.Send_Start(can) == false)
                {
                    EndIndicator(CAN_Processes.Wake_Start_A_B, false);
                    return false;
                }
                SART_Common.End_Process(CAN_Processes.Wake_Start_A_B, true);
                //Application_Helper.DoEvents();
                Thread.Sleep(Delay_Wake_Start * 2);

                ///// Send a WAKE - Start
                /////////////////////////////////////////////////////////////////////////////////////////////////
                if (Continue_Processing == false) return false;

                SART_Common.Start_Process(CAN_Processes.Entering_Diagnostic_Mode);
                //Application_Helper.DoEvents();

                if (SetupDelegate != null)
                {
                    Continue_Processing = SetupDelegate(can, StartIndicator, EndIndicator, OnDataRecievedEventHandlerMethod);
                }
                if (Continue_Processing == false) return false;


                SART_Common.Start_Process(CAN_Processes.Reading_BSA_SW_Version);
                _Side = side;
                _SWVersionA = _BuildCountA = _SWVersionB = _BuildCountB = 0;
                _bHandlerInProcess = false;
                DateTime dt = DateTime.Now.AddSeconds(10);
                while (dt > DateTime.Now)
                {
                    if (_SWVersionA != 0 && _BuildCountA != 0 && _SWVersionB != 0 && _BuildCountB != 0)
                    {
                        _bHandlerInProcess = true;
                        break;
                    }
                    //Application_Helper.DoEvents();
                    Thread.Sleep(100);
                }

                if ((_SWVersionA != 0) && (_BuildCountA != 0) && (_SWVersionB != 0) && (_BuildCountB != 0))
                {
                    if (EndIndicator != null)
                        EndIndicator(CAN_Processes.Reading_BSA_SW_Version, true,
  String.Format("BSA-A S/W Version: {0:X4} {1}, BSA-B S/W Version: {2:X4} {3}", _SWVersionA, _BuildCountA, _SWVersionB, _BuildCountB));
                }
                else
                {
                    SART_Common.End_Process(CAN_Processes.Reading_BSA_SW_Version, false);
                }
                //Application_Helper.DoEvents();


                if (CleanupDelegate != null)
                {
                    Continue_Processing = CleanupDelegate(StartIndicator, EndIndicator, OnDataRecievedEventHandlerMethod);
                }
                if (Continue_Processing == false) return false;

                return true;
#endif
            }

            finally
            {
                ////////////////////////////////////////////////////////////////////////////
                SART_Common.Start_Process(CAN_Processes.Wake_Stop_A_B);
                if (wake != null) wake.Send_Stop(can);
                SART_Common.End_Process(CAN_Processes.Wake_Stop_A_B, true);
                ////////////////////////////////////////////////////////////////////////////


                ////////////////////////////////////////////////////////////////////////////
                logger.Debug("Closing CAN connection");
                SART_Common.ClosingConnection(obj.ID, can);
                ////////////////////////////////////////////////////////////////////////////
            }
        }


#if DoNotDo


        static Boolean _bHandlerInProcess = false;
        static Int32 _SWVersionA = 0, _SWVersionB = 0;
        static Int32 _BuildCountA = 0, _BuildCountB = 0;
        static CAN_CU_Sides _Side = CAN_CU_Sides.A;

        static void OnDataRecievedEventHandlerMethod(Dictionary<String, UInt16> varsA, Dictionary<String, UInt16> varsB)
        {
            if (_bHandlerInProcess == false)
            {
                _bHandlerInProcess = true;
                _SWVersionA = varsA["bsa_software_version"];
                _BuildCountA = varsA["bsa_software_build_count"];
                _SWVersionB = varsB["bsa_software_version"];
                _BuildCountB = varsB["bsa_software_build_count"];
                //logger.Debug("BSA Software Version: {0}, BSA Software Build Count: {1}", _SWVersionA, _BuildCountA);
                _bHandlerInProcess = false;
            }
        }

#endif
    }
}
