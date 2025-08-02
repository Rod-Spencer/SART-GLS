using NLog;
using Segway.COFF.Objects;
using Segway.Database.Objects;
using Segway.Modules.SART_Infrastructure;
using Segway.SART.Objects;
using Segway.Service.CAN;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using Segway.Service.Tools.CAN2.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;



namespace Segway.Modules.Diagnostics_Helper
{
    public class Diagnostics
    {
        //private KEY64 _key;

        private Object _EnterDiagnosticInProcess = new object();
        private Object _DataReceivedInProcess = new object();

        private bool IsInDiagnosticMode;

        private CAN2 _can;

        //private WatchManager _WatchManager;

        private DiagDataManager _dataManager;

        private PowerUpModes _mode;

        private static Logger logger = Logger_Helper.GetCurrentLogger();

        private readonly static string NULL_STATE_IS_ACTIVE = "null_state_is_active";

        public string ExtendedErrorsA { get; set; }

        public string ExtendedErrorsB { get; set; }

        public bool IsAInDiagnosticMode { get; set; }

        public bool IsBInDiagnosticMode { get; set; }


        #region Settings

        private SART_Settings _Settings;

        /// <summary>Property Settings of type SART_Settings</summary>
        public SART_Settings Settings
        {
            get
            {
                if (_Settings == null) _Settings = new SART_Settings();
                return _Settings;
            }
            set
            {
                _Settings = value;
            }
        }

        #endregion




        public Diagnostics(CAN2 can)
        {
            _can = can;
            //_key.key = new ushort[] { 12815, 53852, 43352, 63816 };
        }

        private void DataRecievedEventHandler(CAN_CU_Sides side, Dictionary<String, Int16> vars)
        {
            if (vars == null) return;

            lock (_DataReceivedInProcess)
            {
                try
                {
                    if (side == CAN_CU_Sides.A)
                    {
                        if (!IsAInDiagnosticMode)
                        {
                            if (logger.IsTraceEnabled)
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Append("Received A: ");
                                bool first = true;
                                foreach (KeyValuePair<String, Int16> item in vars)
                                {
                                    if (!first)
                                    {
                                        sb.Append(", ");
                                    }
                                    sb.Append(string.Format("{0}-{1:X4}", item.Key, item.Value));
                                    first = false;
                                }
                                logger.Trace(sb.ToString());
                            }
                            if (!vars.ContainsKey(NULL_STATE_IS_ACTIVE))
                            {
                                logger.Warn("varsA does not contain: {0}", NULL_STATE_IS_ACTIVE);
                            }
                            else
                            {
                                IsAInDiagnosticMode = vars[NULL_STATE_IS_ACTIVE] == 1;
                                if (IsAInDiagnosticMode)
                                {
                                    logger.Info("A-Side is in Diagnostic Mode");
                                }
                            }
                        }
                    }
                    else if (side == CAN_CU_Sides.B && !IsBInDiagnosticMode)
                    {
                        if (logger.IsTraceEnabled)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("Received B: ");
                            bool first = true;
                            foreach (KeyValuePair<string, Int16> item in vars)
                            {
                                if (!first)
                                {
                                    sb.Append(", ");
                                }
                                sb.Append(string.Format("{0}-{1:X4}", item.Key, item.Value));
                                first = false;
                            }
                            logger.Trace(sb.ToString());
                        }
                        if (!vars.ContainsKey(NULL_STATE_IS_ACTIVE))
                        {
                            logger.Warn("varsB does not contain: {0}", NULL_STATE_IS_ACTIVE);
                        }
                        else
                        {
                            IsBInDiagnosticMode = vars[NULL_STATE_IS_ACTIVE] == 1;
                            if (IsBInDiagnosticMode)
                            {
                                logger.Info("B-Side is in Diagnostic Mode");
                            }
                        }
                    }
                    IsInDiagnosticMode = (IsAInDiagnosticMode && IsBInDiagnosticMode);
                    if (IsInDiagnosticMode)
                    {
                        _dataManager.Unload();
                        return;
                    }


                    if (side == CAN_CU_Sides.A)
                    {
                        EmbeddedFaults AFaults = _dataManager.GetEmbeddedFaults(CAN_CU_Sides.A);
                        if (FaultsDecoder.DecodeFaultsIfAny(CAN_CU_Sides.A, AFaults, _mode != PowerUpModes.POWER_UP_WIRELESS_WID))
                        {
                            ExtendedErrorsA = AFaults.ToString();
                            StringBuilder message = new StringBuilder();
                            message.AppendLine("###################################### A-Side Real embedded faults occurred #####################################");
                            message.AppendLine(ExtendedErrorsA);
                            message.Append("#################################################################################################################");
                            logger.Warn(message.ToString());
                            _dataManager.Unload();
                            return;
                        }
                    }
                    else if (side == CAN_CU_Sides.B)
                    {
                        EmbeddedFaults BFaults = _dataManager.GetEmbeddedFaults(CAN_CU_Sides.B);
                        if (FaultsDecoder.DecodeFaultsIfAny(CAN_CU_Sides.B, BFaults, _mode != PowerUpModes.POWER_UP_WIRELESS_WID))
                        {
                            ExtendedErrorsB = BFaults.ToString();
                            StringBuilder message = new StringBuilder();
                            message.AppendLine("###################################### B-Side Real embedded faults occurred #####################################");
                            message.AppendLine(ExtendedErrorsB);
                            message.Append("#################################################################################################################");
                            logger.Warn(message.ToString());
                            _dataManager.Unload();
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Error(Exception_Helper.FormatExceptionString(e));
                }
            }
        }

        public Enter_Diagnostic_Mode_Status EnterDiagnosticMode(PowerUpModes mode)
        {
            lock (_EnterDiagnosticInProcess)
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                COFF_Descriptor cd = new COFF_Descriptor(PT_Generations.Gen2, PT_Component.CU, PT_Models.I2, PT_Code_Type.Application);
                if (CAN2_Commands.Loaded_COFF_Files.ContainsKey(cd.Description) == false)
                {
                    SqlBooleanCriteria criteria = SqlBooleanCriteria.Create_Criteria(cd);

                    List<SART_COFF_Files> coffList = SART_COFF_Web_Service_Client_REST.Select_SART_COFF_Files_Criteria(InfrastructureModule.Token, criteria);
                    if ((coffList == null) || (coffList.Count == 0))
                    {
                        throw new Exception("Unable to retrieve COFF file");
                    }
                    CAN2_Commands.CU_COFF = coffList[coffList.Count - 1];
                    CAN2_Commands.Loaded_COFF_Files[cd.Description] = CAN2_Commands.CU_COFF.Data;
                }

                try
                {
                    IsInDiagnosticMode = false;
                    ExtendedErrorsB = String.Empty;
                    ExtendedErrorsA = String.Empty;
                    _mode = mode;

                    bool aStatus = false;
                    bool bStatus = false;
                    Enter_Diagnostic_Mode_Status diagStatus = Enter_Diagnostic_Mode_Status.Success;
                    logger.Debug("Must see Heartbeat for Side-A/B within {0}ms", Settings.Timeout_Heartbeat);
                    DateTime now = DateTime.Now;
                    DateTime timeout = now.AddMilliseconds((double)Settings.Timeout_Heartbeat);
                    while (DateTime.Now < timeout)
                    {
                        if ((aStatus == true) && (bStatus == true)) break;

                        if ((aStatus == false) && (_can.WaitFor(Message_IDs.CID_CUA_HEARTBEAT, CAN_CU_Sides.A, 50, false) != null))
                        {
                            logger.Debug("Communication established with CU-A");
                            IsAInDiagnosticMode = true;
                            aStatus = true;
                        }
                        if ((bStatus == false) && (_can.WaitFor(Message_IDs.CID_CUB_HEARTBEAT, CAN_CU_Sides.B, 50, false) != null))
                        {
                            logger.Debug("Communication established with CU-B");
                            IsBInDiagnosticMode = true;
                            bStatus = true;
                        }
                    }
                    if ((aStatus == false) || (bStatus == false))
                    {
                        if (!aStatus)
                        {
                            logger.Error("Unable to establish communication with CU-A");
                            IsAInDiagnosticMode = false;
                            diagStatus = diagStatus | Enter_Diagnostic_Mode_Status.No_Heartbeat_A;
                        }
                        if (!bStatus)
                        {
                            logger.Error("Unable to establish communication with CU-B");
                            IsBInDiagnosticMode = false;
                            diagStatus = diagStatus | Enter_Diagnostic_Mode_Status.No_Heartbeat_B;
                        }
                        return diagStatus;
                    }


                    diagStatus = Enter_Diagnostic_Mode_Status.Success;
                    IsBInDiagnosticMode = false;
                    IsAInDiagnosticMode = false;
                    logger.Info("Unlocking diagnostic mode");
                    if (CU_Unlock.Unlock_Diagnostic(_can, CAN_CU_Sides.A, CAN2_Commands.Diagnostic_Unlock_Code) == false)
                    {
                        logger.Error("Failed to unlock diagnostic mode: Side-A");
                        diagStatus |= Enter_Diagnostic_Mode_Status.Failed_Unlock_Diagnostic_A;
                    }
                    if (CU_Unlock.Unlock_Diagnostic(_can, CAN_CU_Sides.B, CAN2_Commands.Diagnostic_Unlock_Code) == false)
                    {
                        logger.Error("Failed to unlock diagnostic mode: Side-B");
                        diagStatus |= Enter_Diagnostic_Mode_Status.Failed_Unlock_Diagnostic_B;
                    }
                    if (diagStatus != Enter_Diagnostic_Mode_Status.Success)
                    {
                        return diagStatus;
                    }




                    _dataManager = new DiagDataManager();
                    logger.Info("Initializing Data Manager");
                    if (_dataManager.Initialize(_can, new List<string>(new String[] { NULL_STATE_IS_ACTIVE })) == false)
                    {
                        logger.Error("Failed to set watch variable: null_state_is_active");
                        return Enter_Diagnostic_Mode_Status.Failed_Watch_Setup;
                    }


                    logger.Debug("Assigning Data Receiver Event Handler");
                    _dataManager.DataRecieved += new DataRecievedEventHandler(DataRecievedEventHandler);
                    Thread.Sleep(500);


                    logger.Debug("Trying to enter diagnostic mode");
                    if (mode != PowerUpModes.POWER_UP_WIRELESS_WID)
                    {
                        CU_Enter_Motor_Cmd_Mode cUEnterMotorCmdModeA = new CU_Enter_Motor_Cmd_Mode(CAN_CU_Sides.A);
                        CU_Enter_Motor_Cmd_Mode cUEnterMotorCmdModeB = new CU_Enter_Motor_Cmd_Mode(CAN_CU_Sides.B);
                        cUEnterMotorCmdModeA.Send(_can, true);
                        cUEnterMotorCmdModeB.Send(_can, true);
                        Thread.Sleep(50);
                        cUEnterMotorCmdModeA.Send(_can, true);
                        cUEnterMotorCmdModeB.Send(_can, true);
                        Thread.Sleep(50);
                    }
                    else
                    {
                        CU_Xfer_Null_Mode cUXferNullModeA = new CU_Xfer_Null_Mode(CAN_CU_Sides.A);
                        CU_Xfer_Null_Mode cUXferNullModeB = new CU_Xfer_Null_Mode(CAN_CU_Sides.B);
                        cUXferNullModeA.Send(_can, true);
                        cUXferNullModeB.Send(_can, true);
                        Thread.Sleep(50);
                        cUXferNullModeA.Send(_can, true);
                        cUXferNullModeB.Send(_can, true);
                        Thread.Sleep(50);
                    }

                    logger.Info("Waiting to enter diagnostic mode...{0}ms", CAN2_Commands.Delay_Diagnostic_Wakeup);
                    Thread.Sleep(CAN2_Commands.Delay_Diagnostic_Wakeup);

                    if (!string.IsNullOrEmpty(ExtendedErrorsA) || !string.IsNullOrEmpty(ExtendedErrorsB))
                    {
                        if (!string.IsNullOrEmpty(ExtendedErrorsA))
                        {
                            logger.Warn("A-Side Error(s): {0}", ExtendedErrorsA);
                        }
                        if (!string.IsNullOrEmpty(ExtendedErrorsB))
                        {
                            logger.Warn("B-Side Error(s): {0}", ExtendedErrorsB);
                        }
                    }

                    WatchManager.Flush_CAN();
                    logger.Debug("Setting timeout: {0} ms", CAN2_Commands.Timeout_Enter_Diagnostic_Mode);
                    timeout = DateTime.Now.AddMilliseconds(CAN2_Commands.Timeout_Enter_Diagnostic_Mode);
                    while (DateTime.Now < timeout)
                    {
                        if (IsInDiagnosticMode == true)
                        {
                            logger.Debug("Diagnostics mode entered successfully...");
                            return Enter_Diagnostic_Mode_Status.Success;
                        }
                        Thread.Sleep(250);
                    }

                    logger.Error("Failed to enter diagnostics mode.");
                    return Enter_Diagnostic_Mode_Status.Failed_Enter_Diagnostic_Timeout;
                }
                catch (Exception exception)
                {
                    Exception exp = exception;
                    logger.Error(Exception_Helper.FormatExceptionString(exp));
                    throw exp;
                }
                finally
                {
                    if (_dataManager != null)
                    {
                        _dataManager.DataRecieved -= new DataRecievedEventHandler(DataRecievedEventHandler);
                        _dataManager.Unload();
                    }
                    logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
                }
            }
        }
    }
}












#if Removed_By_Rod_Spencer_2016_04_26

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Segway.Service.Tools.CAN2;
using Segway.Service.Tools.CAN2.Messages;
using Segway.Service.CAN;
using Segway.Service.Common;
using Segway.Service.LoggerHelper;
using NLog;
using Segway.Modules.SART_Infrastructure;

namespace Segway.Modules.Diagnostics_Helper
{
    public class Diagnostics
    {
        internal KEY64 DiagnosticsKey
        {
            get
            {
                return _key;
            }
        }

        private KEY64 _key = new KEY64();
        private Boolean Evaluation_Inprocess = false;
        private Boolean IsInDiagnosticMode = false;
        private CAN2 _can = null;
        private WatchManager _WatchManager = null;
        private DiagDataManager _dataManager = null;
        private PowerUpModes _mode;
        private static Logger logger = Logger_Helper.GetCurrentLogger();
        private readonly static string NULL_STATE_IS_ACTIVE = "null_state_is_active";



        public Diagnostics(CAN2 can, WatchManager wm)
        {
            _can = can;
            _WatchManager = wm;

            _key.key = new ushort[4];
            _key.key[0] = 0x320f;
            _key.key[1] = 0xd25c;
            _key.key[2] = 0xa958;
            _key.key[3] = 0xf948;
        }

        public Boolean EnterDiagnosticMode(PowerUpModes mode)
        {
            try
            {
                ExtendedErrorsA = ExtendedErrorsB = String.Empty;
                Evaluation_Inprocess = false;
                _mode = mode;
                Boolean aStatus = true, bStatus = true;
                logger.Debug("Must see Heartbeat for Side-A withing {0}ms", InfrastructureModule.Settings.Timeout_Heartbeat);
                if (_can.WaitFor(Message_IDs.CID_CUA_HEARTBEAT, CAN_CU_Sides.A, InfrastructureModule.Settings.Timeout_Heartbeat) == null)
                {
                    logger.Error("Unable to establish communication with CU-A");
                    IsAInDiagnosticMode = false;
                    aStatus = false;
                }
                else
                {
                    logger.Debug("Communication established with CU-A");
                    IsAInDiagnosticMode = true;
                }

                logger.Debug("Must see Heartbeat for Side-B withing {0}ms", InfrastructureModule.Settings.Timeout_Heartbeat);
                if (_can.WaitFor(Message_IDs.CID_CUB_HEARTBEAT, CAN_CU_Sides.B, InfrastructureModule.Settings.Timeout_Heartbeat) == null)
                {
                    logger.Error("Unable to establish communication with CU-B");
                    IsBInDiagnosticMode = false;
                    bStatus = false;
                }
                else
                {
                    logger.Debug("Communication established with CU-B");
                    IsBInDiagnosticMode = true;
                }

                if (!aStatus || !bStatus) return false;

                IsInDiagnosticMode = IsAInDiagnosticMode = IsBInDiagnosticMode = false;
                logger.Debug("Unlocking diagnostic mode");
                if (CU_Unlock.Unlock_Diagnostic(_can, CAN_CU_Sides.A, _key.key) == false)
                {
                    logger.Error("Failed to unlock diagnostic mode: Side-A");
                }
                Thread.Sleep(10);
                if (CU_Unlock.Unlock_Diagnostic(_can, CAN_CU_Sides.B, _key.key) == false)
                {
                    logger.Error("Failed to unlock diagnostic mode: Side-B");
                }
                Thread.Sleep(10);

                _dataManager = new DiagDataManager(_WatchManager);
                //if (_dataManager.Initialize(new List<String> { "null_state_is_active" }) == false) return false;
                logger.Debug("Initializing Data Manager");
                if (_dataManager.Initialize(new List<String> { "null_state_is_active", "ap_main_been_in_balance" }))
                {
                    _dataManager.DataRecieved += new DataRecievedEventHandler(DataRecievedEventHandler);
                    //_WatchManager.WatchDataReady += WatchDataReadyHandler;
                    //_dataManager.
                    Thread.Sleep(500);
                    //_WatchManager.Start();

                    // Request Diagnostic Mode
                    logger.Debug("Trying to enter diagnostic mode");
                    if (mode == PowerUpModes.POWER_UP_WIRELESS_WID)
                    {
                        CU_Xfer_Null_Mode msg1 = new CU_Xfer_Null_Mode(CAN_CU_Sides.A);
                        msg1.Send(_can);
                        Thread.Sleep(50);
                        msg1.Send(_can);
                        Thread.Sleep(50);
                        CU_Xfer_Null_Mode msg2 = new CU_Xfer_Null_Mode(CAN_CU_Sides.B);
                        msg2.Send(_can);
                        Thread.Sleep(50);
                        msg2.Send(_can);
                        Thread.Sleep(50);
                    }
                    else
                    {
                        CU_Enter_Motor_Cmd_Mode msg1 = new CU_Enter_Motor_Cmd_Mode(CAN_CU_Sides.A);
                        msg1.Send(_can);
                        Thread.Sleep(50);
                        msg1.Send(_can);
                        Thread.Sleep(50);
                        CU_Enter_Motor_Cmd_Mode msg2 = new CU_Enter_Motor_Cmd_Mode(CAN_CU_Sides.B);
                        msg2.Send(_can);
                        Thread.Sleep(50);
                        msg2.Send(_can);
                        Thread.Sleep(50);
                    }
                    //
                    // Give it some time to settle down
                    logger.Debug("Waiting to enter diagnostic mode...");
                    Thread.Sleep(2000);

                    DateTime currenttime = DateTime.Now;
                    DateTime timeout = currenttime.AddSeconds(5);
                    while ((currenttime < timeout) && (String.IsNullOrEmpty(ExtendedErrorsA) == false) && (String.IsNullOrEmpty(ExtendedErrorsB) == false))
                    {
                        if (IsInDiagnosticMode == true)
                        {
                            logger.Debug("Diagnostics mode entered successfully...");
                            break;
                        }
                        Thread.Sleep(200);
                        logger.Debug("Waiting to enter diagnostic mode: timeout in {0:N2} seconds", (timeout - currenttime).TotalSeconds);
                        currenttime = DateTime.Now;
                    }
                }
                else
                {
                    logger.Error("Failed to set watch variable: null_state_is_active");
                }

            }
            catch (Exception exp)
            {
                logger.Error(Exception_Helper.FormatExceptionString(exp));
                throw exp;
            }
            finally
            {
                if (IsInDiagnosticMode == false) logger.Error("Failed to enter diagnostics mode.");

                if (_dataManager != null)
                {
                    _dataManager.DataRecieved -= DataRecievedEventHandler;
                    _dataManager.Unload();
                }
            }

            return IsInDiagnosticMode;
        }

        void DataRecievedEventHandler(CAN_CU_Sides side, Dictionary<String, UInt16> vars)
        {
            if (_bEvaluationInProcess == false)
            {
                String message = String.Empty;
                String faultsA = String.Empty, faultsB = String.Empty;
                Boolean bFaultsA = false, bFaultsB = false;
                //var _embeddFaultsA = new EmbeddedFaultsStrings();
                //var _embeddFaultsB = new EmbeddedFaultsStrings();
                String prevMsg = String.Empty;

                _bEvaluationInProcess = true;
                if (IsAInDiagnosticMode == false) IsAInDiagnosticMode = varsA["null_state_is_active"] == (Int32)ReturnedStatus.Success;
                if (IsBInDiagnosticMode == false) IsBInDiagnosticMode = varsB["null_state_is_active"] == (Int32)ReturnedStatus.Success;
                _bInDiagnostic = (IsAInDiagnosticMode && IsBInDiagnosticMode);

                if (_bInDiagnostic == true)
                {
                    _dataManager.Unload();
                    return;
                }


                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // Removed by Rod Spencer - 7 Apr 2016
                if (false)
                {
                    var AFaults = _dataManager.GetEmbeddedFaults(CAN_CU_Sides.A);
                    if ((bFaultsA = FaultsDecoder.DecodeFaultsIfAny(CAN_CU_Sides.A, AFaults, _mode != PowerUpModes.POWER_UP_WIRELESS_WID)) != false)
                    {
                        message = ExtendedErrorsA = AFaults.ToString();
                    }

                    var BFaults = _dataManager.GetEmbeddedFaults(CAN_CU_Sides.B);
                    if ((bFaultsB = FaultsDecoder.DecodeFaultsIfAny(CAN_CU_Sides.B, BFaults, _mode != PowerUpModes.POWER_UP_WIRELESS_WID)) != false)
                    {
                        ExtendedErrorsB = BFaults.ToString();
                        if (String.IsNullOrEmpty(message) == false) message += "\n";
                        message += ExtendedErrorsB;
                    }

                    if (bFaultsA == true || bFaultsB == true)
                    {
                        logger.Debug("######################################### Real embedded faults occurred #########################################\n\n");
                        logger.Debug(message);
                        logger.Debug("#################################################################################################################\n\n");
                        _dataManager.Unload();
                        return;
                    }
                }
                // Removed by Rod Spencer - 7 Apr 2016
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                _bEvaluationInProcess = false;
            }
        }

        public Boolean IsAInDiagnosticMode { get; set; }

        public Boolean IsBInDiagnosticMode { get; set; }

        public String ExtendedErrorsA { get; set; }

        public String ExtendedErrorsB { get; set; }

        private void WatchDataReadyHandler(Object sender, WatchDataReadyEventArgs args)
        {
            //if (_bEvaluationInProcess == false)
            //{
            //    _bEvaluationInProcess = true;
            //    if (IsAInDiagnosticMode == false) IsAInDiagnosticMode = args.WatchVariablesA["null_state_is_active"] == (Int32)ReturnedStatus.Success;
            //    if (IsBInDiagnosticMode == false) IsBInDiagnosticMode = args.WatchVariablesB["null_state_is_active"] == (Int32)ReturnedStatus.Success;
            //    _bInDiagnostic = (IsAInDiagnosticMode && IsBInDiagnosticMode);

            //    if (_bInDiagnostic == true)
            //    {
            //        _WatchManager.Stop();
            //    }
            //    else
            //    {
            //        _bEvaluationInProcess = false;
            //    }
            //}
        }
    }
}

#endif
