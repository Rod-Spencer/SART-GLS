using Microsoft.Practices.Unity;
using NLog;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.WorkOrder;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.CAN;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Segway.Modules.CU_Log_Module
{
    public partial class Log_Extract
    {
        private static Logger logger = Logger_Helper.GetCurrentLogger();
        // static Dictionary<String, SART_Event_Log_Entry> EntryLog = new Dictionary<string, SART_Event_Log_Entry>();
        private static CU_Log_Extraction_View_Model _CULogViewModel;
        private static String _ExtractionErrMessage;


        #region Token

        /// <summary>Property Token of type AuthenticationToken</summary>
        public static AuthenticationToken Token
        {
            get
            {
                if (InfrastructureModule.Container.IsRegistered<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName) == true)
                {
                    return InfrastructureModule.Container.Resolve<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName);
                }
                return null;
            }
        }

        #endregion

        #region LoginContext

        /// <summary>Property LoginContext of type Login_Context</summary>
        public static Login_Context LoginContext
        {
            get
            {
                if (Token != null) return Token.LoginContext;
                return null;
            }
        }
        #endregion

        public static SART_CU_Logs Extract_CU_Log(CU_Log_Extraction_View_Model vm, CAN_CU_Sides side = CAN_CU_Sides.A)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SART_CU_Logs cuLog = null;
                _CULogViewModel = vm;
                _ExtractionErrMessage = "";

                logger.Debug("Creating instance of SART_Event_Object");
                SART_Event_Object obj = new SART_Event_Object(InfrastructureModule.Current_Work_Order.Work_Order_ID,
                                                              Event_Types.Extract_CUA_Log + (side - CAN_CU_Sides.A),
                                                              Event_Statuses.In_Progress,
                                                              LoginContext.UserName);

                //if (side == CAN_CU_Sides.B) obj.Type_Event = Event_Types.Extract_CUB_Log;
                //else obj.Type_Event = ;
                //obj.EventStatus = ;

                logger.Debug("Inserting instance of SART_Event_Object to DB");
                obj = SART_EVOBJ_Web_Service_Client_REST.Insert_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                if (obj == null)
                {
                    String msg = "Unable to insert Event Object";
                    logger.Error(msg);
                    InfrastructureModule.Aggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
                    return null;
                }
                logger.Debug("SART Event Object ID: {0}", obj.ID);
                InfrastructureModule.Aggregator.GetEvent<CU_Log_EventID_Event>().Publish(obj.ID);
                CU_Log_Module.CU_Log_Module_Container.RegisterInstance<int>("ObjectID", obj.ID, new ContainerControlledLifetimeManager());

                logger.Debug("Establishing CAN connection for side: {0}", side);
                CAN2 can = SART_Common.EstablishConnection(obj.ID);

                if (can != null)
                {
                    if (CAN2_Commands.Continue_Processing == false) return null;
                    logger.Debug("Extracting CU Log");

                    ////////////////////////////////////
                    cuLog = CAN2_Commands.Extract_CU_Log(can, InfrastructureModule.Current_Work_Order.PT_Serial, side, SART_Common.Start_Process, SART_Common.End_Process, Validate_Process);
                    ////////////////////////////////////
                    if ((cuLog != null) && (String.IsNullOrEmpty(cuLog.CU_Serial) == false))
                    {
                        cuLog.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                        cuLog.PT_Serial = InfrastructureModule.Current_Work_Order.PT_Serial;
                        cuLog.User_Name = LoginContext.UserName;
                        cuLog.Account_ID = LoginContext.Group_ID;
                        cuLog.Customer_ID = LoginContext.Customer_ID;
                        cuLog.Date_Time_Extracted = DateTime.Now;

                        logger.Debug("Uploading to Database");
                        SART_Event_Log_Entry elog = SART_Common.Create_Entry_Log(CAN_Processes.Upload_To_Database, obj.ID);
                        cuLog = SART_Log_Web_Service_Client_REST.Insert_SART_CU_Logs_Object(InfrastructureModule.Token, cuLog);
                        SART_Common.Update_Entry_Log(cuLog != null, elog);
                        if (cuLog != null)
                        {
                            if (side == CAN_CU_Sides.A) InfrastructureModule.Aggregator.GetEvent<WorkOrder_Read_CU_Log_Event>().Publish(WorkOrder_Events.Extracted_CUA_Log);
                            else if (side == CAN_CU_Sides.B) InfrastructureModule.Aggregator.GetEvent<WorkOrder_Read_CU_Log_Event>().Publish(WorkOrder_Events.Extracted_CUB_Log);
                        }
                    }
                }

                logger.Debug("Closing CAN connection");
                SART_Common.ClosingConnection(obj.ID, can);

                obj.Timestamp_End = DateTime.Now;
                obj.EventStatus = Event_Statuses.Finished;
                SART_EVOBJ_Web_Service_Client_REST.Update_SART_Event_Object_Key(InfrastructureModule.Token, obj);

                var sts = SART_Common.Create_Event(WorkOrder_Events.Extracted_CUA_Log + (side - CAN_CU_Sides.A), cuLog != null ? Event_Statuses.Passed : Event_Statuses.Failed, obj.ID);
                if (sts == true)
                {
                    InfrastructureModule.Aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
                }
                InfrastructureModule.Aggregator.GetEvent<CU_Log_EventID_Event>().Publish(0);
                return cuLog;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private static Boolean Validate_Process(CAN_CU_Sides side, String cuSerial)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Enum.IsDefined(typeof(CAN_CU_Sides), side) == false) throw new ArgumentException("Parameter side (CAN_CU_Sides) is not defined.");
                if (String.IsNullOrEmpty(cuSerial)) throw new ArgumentNullException("Parameter cuSerial (String) can not be null or empty.");

                if (String.IsNullOrEmpty(cuSerial) == false)
                {
                    logger.Debug("Validating CU-{0} ({1})", side, cuSerial);
                    SART_Common.Start_Process(CAN_Processes.CUA_Validate_Serial_Number + (side - CAN_CU_Sides.A));
                    List<SART_PT_Configuration> cfgList = CU_Log_Module.CU_Log_Module_Container.Resolve<List<SART_PT_Configuration>>("Configurations");
                    if ((cfgList == null) || (cfgList.Count <= 0))
                    {
                        logger.Warn("No configuration information");
                        SART_Common.End_Process(CAN_Processes.CUA_Validate_Serial_Number + (side - CAN_CU_Sides.A), false, String.Format("CU Serial: {0} is invalid", cuSerial));
                        return false;
                    }

                    List<SART_PT_Configuration> configurations = new List<SART_PT_Configuration>(cfgList);
                    if (configurations.Count > 1) configurations.Sort(new SART_PT_Configuration_Created_Reverse_Comparer());
                    foreach (SART_PT_Configuration Cfg in configurations)
                    //for (int x = cfgList.Count - 1; x >= 0; x--)
                    {
                        if (Cfg == null) continue;
                        //SART_PT_Configuration Cfg = cfgList[x];

                        if (side == CAN_CU_Sides.A)
                        {
                            if (String.IsNullOrEmpty(Cfg.CUA_Serial) == false)
                            {
                                if (cuSerial == Cfg.CUA_Serial)
                                {
                                    logger.Debug("Found Match to CU-A");
                                    SART_Common.End_Process(CAN_Processes.CUA_Validate_Serial_Number, true);
                                    return true;
                                }
                                else
                                {
                                    logger.Debug("Did not Match! ({0} != {1})", cuSerial, Cfg.CUA_Serial);
                                }
                            }
                        }
                        else if (side == CAN_CU_Sides.B)
                        {
                            if (String.IsNullOrEmpty(Cfg.CUB_Serial) == false)
                            {
                                if (cuSerial == Cfg.CUB_Serial)
                                {
                                    logger.Debug("Found Match to CU-B");
                                    SART_Common.End_Process(CAN_Processes.CUB_Validate_Serial_Number, true);
                                    return true;
                                }
                                else
                                {
                                    logger.Debug("Did not Match! ({0} != {1})", cuSerial, Cfg.CUB_Serial);
                                    //SART_Common.End_Process(CAN_Processes.CUB_Validate_Serial_Number, false, String.Format("CU Serial: {0} is invalid", cuSerial));
                                    //return false;
                                }
                            }
                        }
                    }


                    logger.Debug("Checking in replaced components from Repair tab");
                    List<SART_WO_Components> compList = SART_WOComp_Web_Service_Client_REST.Select_SART_WO_Components_WORK_ORDER_ID(InfrastructureModule.Token, InfrastructureModule.Current_Work_Order.Work_Order_ID);
                    if ((compList != null) && (compList.Count > 0))
                    {
                        foreach (SART_WO_Components comp in compList)
                        {
                            if (comp.Serial_Number_New == cuSerial)
                            {
                                logger.Debug("Found Match to repair component");
                                return true;
                            }
                        }
                    }

                    String msg = String.Format("CU-{0} serial number '{1}' does not match machine history.", side, cuSerial);
                    logger.Debug(msg);
                    if (_CULogViewModel != null)
                    {
                        _CULogViewModel.CUSerialNumberValidationErrorMessage = msg;
                        _CULogViewModel.CUSerialNumberValidationErrorPopupOpen = true;

                        logger.Debug("Waiting for user response...");
                        while (_CULogViewModel.ContinueOnError.HasValue == false)
                        {
                            //Application_Helper.DoEvents();
                            Thread.Sleep(200);
                        }

                        if (_CULogViewModel.ContinueOnError.HasValue == true && _CULogViewModel.ContinueOnError.Value == true)
                        {
                            logger.Debug(String.Format("CU-{0} serial number ({1}) not found in database, but user chose to continue.", side, cuSerial));
                            SART_Common.End_Process(CAN_Processes.CUA_Validate_Serial_Number + (side - CAN_CU_Sides.A), false, "User chose to continue, overriding validation error.");
                            _ExtractionErrMessage = "User chose to continue, overriding validation error.";
                            return true;
                        }
                    }

                    String ErrMsg = String.Format("CU-{0} serial number ({1}) does not match.", side, cuSerial);
                    SART_Common.End_Process(CAN_Processes.CUA_Validate_Serial_Number + (side - CAN_CU_Sides.A), false, ErrMsg);
                    _ExtractionErrMessage = ErrMsg;
                    return false;
                }
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                return false;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

    }
}
