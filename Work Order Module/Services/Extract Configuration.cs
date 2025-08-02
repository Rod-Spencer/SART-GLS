using Microsoft.Practices.Unity;
using Segway.Database.Objects;
using Segway.Manufacturing.Objects;
using Segway.Modules.SART_Infrastructure;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.CAN;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Segway.Modules.WorkOrder
{
    public partial class Work_Order_Configuration_ViewModel
    {
        public Boolean Extract_Configuration(Security secRec, SART_PT_Configuration config)
        {
            try
            {
                logger.Debug("Entered");
                _ProgressLog = null;
                Boolean status = false;

                logger.Debug("Registering object instances: Work_Order, aggregator");

                logger.Debug("Creating instance of SART_Event_Object");
                SART_Event_Object obj = new SART_Event_Object(InfrastructureModule.Current_Work_Order.Work_Order_ID,
                                                              Event_Types.Extract_Start_Configuration,
                                                              Event_Statuses.In_Progress,
                                                              InfrastructureModule.Token.LoginContext.UserName);

                logger.Debug("Inserting instance of SART_Event_Object to DB");
                obj = SART_EVOBJ_Web_Service_Client_REST.Insert_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                if (obj == null)
                {
                    String msg = "Unable to insert Event Object";
                    logger.Error(msg);
                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
                    return false;
                }
                logger.Debug("SART Event Object ID: {0}", obj.ID);
                aggregator.GetEvent<WO_Config_EventID_Event>().Publish(obj.ID);
                container.RegisterInstance<int>("ObjectID", obj.ID, new ContainerControlledLifetimeManager());

                logger.Debug("Establishing CAN connection");
                CAN2 can = SART_Common.EstablishConnection(obj.ID);
                if (can != null)
                {
                    try
                    {
                        if (PT_Config.ConfigType == ConfigurationTypes.Service_Start)
                        {
                            //////////////////////////////////////////////////////////////////////////////
                            // Extract the CU Logs
                            //   - After speaking with Steven, they only want the logs pulled on the
                            //     start configuration.  Do not pull final configuration logs.
                            logger.Debug("Extracting the A-Side CU Log");

                            List<SART_CU_Logs> loglist = null;
                            SqlBooleanCriteria criteria = new SqlBooleanCriteria();

                            criteria.Add(new FieldData("PT_Serial", InfrastructureModule.Current_Work_Order.PT_Serial));
                            criteria.Add(new FieldData("Work_Order", InfrastructureModule.Current_Work_Order.Work_Order_ID));
                            criteria.Add(new FieldData("Side", CAN_CU_Sides.A.ToString()[0]));
                            criteria.Add(new FieldData("SART_Extraction", Get_Extraction_Type(PT_Config.ConfigType)));
                            logger.Debug("criteria: {0}", criteria);
                            loglist = SART_Log_Web_Service_Client_REST.Select_SART_CU_Logs_Criteria(InfrastructureModule.Token, criteria);
                            if ((loglist == null) || (loglist.Count == 0))
                            {
                                //////////////////////////////////////////////////////////////////////////////
                                // Extract the A-Side CU Log
                                logger.Info("Extracting the A-Side CU Log");
                                SART_CU_Logs CUA_Log = CAN2_Commands.Extract_CU_Log(can, InfrastructureModule.Current_Work_Order.PT_Serial, CAN_CU_Sides.A, SART_Common.Start_Process, SART_Common.End_Process);
                                if ((CUA_Log != null) && (String.IsNullOrEmpty(CUA_Log.CU_Serial) == false))
                                {
                                    CUA_Log.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                                    CUA_Log.PT_Serial = InfrastructureModule.Current_Work_Order.PT_Serial;
                                    CUA_Log.User_Name = LoginContext.UserName;
                                    CUA_Log.Account_ID = LoginContext.Group_ID;
                                    CUA_Log.Customer_ID = LoginContext.Customer_ID;
                                    CUA_Log.Date_Time_Extracted = DateTime.Now;
                                    CUA_Log.SART_Extraction = Get_Extraction_Type(PT_Config.ConfigType);
                                    CUA_Log.CU_Side = CAN_CU_Sides.A;
                                    logger.Debug("Uploading A-Side CU Log to Database");
                                    SART_Event_Log_Entry elog = SART_Common.Create_Entry_Log(CAN_Processes.Upload_To_Database, obj.ID);
                                    CUA_Log = SART_Log_Web_Service_Client_REST.Insert_SART_CU_Logs_Object(InfrastructureModule.Token, CUA_Log);
                                    SART_Common.Update_Entry_Log(CUA_Log != null, elog);
                                    InfrastructureModule.Aggregator.GetEvent<WorkOrder_Read_CU_Log_Event>().Publish(WorkOrder_Events.Extracted_CUA_Log);
                                }
                                else
                                {
                                    logger.Warn("Unable to extract CU Log for side: A");
                                }
                                // Extract the A-Side CU Log
                                //////////////////////////////////////////////////////////////////////////////
                            }
                            if (CAN2_Commands.Continue_Processing == false) throw new OperationCanceledException();


                            criteria = new SqlBooleanCriteria();
                            criteria.Add(new FieldData("PT_Serial", InfrastructureModule.Current_Work_Order.PT_Serial));
                            criteria.Add(new FieldData("Work_Order", InfrastructureModule.Current_Work_Order.Work_Order_ID));
                            criteria.Add(new FieldData("Side", CAN_CU_Sides.B.ToString()[0]));
                            criteria.Add(new FieldData("SART_Extraction", Get_Extraction_Type(PT_Config.ConfigType)));
                            logger.Debug("criteria: {0}", criteria);
                            loglist = SART_Log_Web_Service_Client_REST.Select_SART_CU_Logs_Criteria(InfrastructureModule.Token, criteria);
                            if ((loglist == null) || (loglist.Count == 0))
                            {
                                //////////////////////////////////////////////////////////////////////////////
                                // Extract the B-Side CU Log
                                logger.Info("Extracting the B-Side CU Log");
                                SART_CU_Logs CUB_Log = CAN2_Commands.Extract_CU_Log(can, InfrastructureModule.Current_Work_Order.PT_Serial, CAN_CU_Sides.B, SART_Common.Start_Process, SART_Common.End_Process);
                                if ((CUB_Log != null) && (String.IsNullOrEmpty(CUB_Log.CU_Serial) == false))
                                {
                                    CUB_Log.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                                    CUB_Log.PT_Serial = InfrastructureModule.Current_Work_Order.PT_Serial;
                                    CUB_Log.User_Name = LoginContext.UserName;
                                    CUB_Log.Account_ID = LoginContext.Group_ID;
                                    CUB_Log.Customer_ID = LoginContext.Customer_ID;
                                    CUB_Log.Date_Time_Extracted = DateTime.Now;
                                    CUB_Log.SART_Extraction = Get_Extraction_Type(PT_Config.ConfigType);
                                    CUB_Log.CU_Side = CAN_CU_Sides.B;
                                    logger.Debug("Uploading B-Side CU Log to Database");
                                    SART_Event_Log_Entry elog = SART_Common.Create_Entry_Log(CAN_Processes.Upload_To_Database, obj.ID);
                                    CUB_Log = SART_Log_Web_Service_Client_REST.Insert_SART_CU_Logs_Object(InfrastructureModule.Token, CUB_Log);
                                    SART_Common.Update_Entry_Log(CUB_Log != null, elog);
                                    InfrastructureModule.Aggregator.GetEvent<WorkOrder_Read_CU_Log_Event>().Publish(WorkOrder_Events.Extracted_CUB_Log);
                                }
                                else
                                {
                                    logger.Warn("Unable to extract CU Log for side: B");
                                }
                                // Extract the B-Side CU Log
                                //////////////////////////////////////////////////////////////////////////////
                                if (CAN2_Commands.Continue_Processing == false) throw new OperationCanceledException();
                            }
                        }
                        // Extract the CU Logs
                        //////////////////////////////////////////////////////////////////////////////






                        //////////////////////////////////////////////////////////////////////////////
                        // Extract the current configuration
                        if (CAN2_Commands.Continue_Processing == true)
                        {
                            logger.Debug("Extracting CU Configuration");
                            CAN2_Commands.JTags.Set_For_Load(secRec);
                            /////////////////////////////////////////////////////////////////////
                            status = CAN2_Commands.Extract_Configuration(can, secRec, config, SART_Common.Start_Process, SART_Common.End_Process, SART_Common.Display_Process);
                            /////////////////////////////////////////////////////////////////////
                        }
                        // Extract the current configuration
                        //////////////////////////////////////////////////////////////////////////////
                        if (CAN2_Commands.Continue_Processing == false) return false;







                        if (PT_Config.ConfigType == ConfigurationTypes.Service_Final)
                        {
                            if (InfrastructureModule.Current_Work_Order.Priority != "Incident")
                            {
                                SART_Common.ClosingConnection(obj.ID, can);
                                SART_Common.Start_Process(CAN_Processes.CUA_Full_Stop);
                                Thread.Sleep(InfrastructureModule.Settings.Delay_Full_Stop);
                                SART_Common.End_Process(CAN_Processes.CUA_Full_Stop, true);
                                can = SART_Common.EstablishConnection(obj.ID);

                                for (CAN_CU_Sides side = CAN_CU_Sides.A; side <= CAN_CU_Sides.B; side++)
                                {
                                    String Serial = "";
                                    //////////////////////////////////////////////////////////////////////////////
                                    // Clear the A-Side CU Log
                                    logger.Debug("Clearing the {0}-Side CU Log", side);
                                    if (CAN2_Commands.Clear_CU_Log(can, InfrastructureModule.Current_Work_Order.PT_Serial, out Serial, side, SART_Common.Start_Process, SART_Common.End_Process) == false)
                                    {
                                        logger.Warn("Clearing the {0}-Side CU Log - Failed", side);
                                    }
                                    // Clear the A-Side CU Log
                                    //////////////////////////////////////////////////////////////////////////////
                                    if (CAN2_Commands.Continue_Processing == false) throw new OperationCanceledException();
                                }
                            }
                        }
                    }
                    catch (Authentication_Exception ae)
                    {
                        logger.Warn(Exception_Helper.FormatExceptionString(ae));
                        throw;
                    }
                    catch (OperationCanceledException)
                    {
                        logger.Warn("User has aborted operation");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(Exception_Helper.FormatExceptionString(ex));
                    }
                    finally
                    {
                        logger.Debug("Closing CAN connection");
                        try
                        {
                            SART_Common.ClosingConnection(obj.ID, can);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(Exception_Helper.FormatExceptionString(ex));
                        }
                    }
                }

                obj.Timestamp_End = DateTime.Now;
                obj.EventStatus = Event_Statuses.Finished;
                SART_EVOBJ_Web_Service_Client_REST.Update_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                SART_Common.Create_Event(Get_Configuration_Type(PT_Config.ConfigType), status ? Event_Statuses.Passed : Event_Statuses.Failed, obj.ID);
                //SART_Events logevent = new SART_Events();
                //logevent.EventType = Get_Configuration_Type(PT_Config.ConfigType);
                //logevent.Object_ID = obj.ID;
                //logevent.Work_Order_ID = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                //logevent.User_Name = User_Info.UserName;
                //logevent.Timestamp = DateTime.Now;
                //logevent.StatusType = status ? Event_Statuses.Passed : Event_Statuses.Failed;
                ////if (logevent.StatusType == Event_Statuses.Failed) logevent.Message = _ExtractionErrMessage;
                //logevent = SART_2012_Web_Service_Client.Insert_SART_Events_Key(InfrastructureModule.Token, logevent);
                aggregator.GetEvent<WO_Config_EventID_Event>().Publish(0);
                aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
                return status;
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
                logger.Debug("Leaving");
            }
        }

        private String Get_Extraction_Type(ConfigurationTypes ctype)
        {
            switch (ctype)
            {
                case ConfigurationTypes.Service_Start: return "SC";
                case ConfigurationTypes.Service_Final: return "FC";
                default: return null;
            }
        }

        private WorkOrder_Events Get_Configuration_Type(ConfigurationTypes ctype)
        {
            switch (ctype)
            {
                case ConfigurationTypes.Service_Start: return WorkOrder_Events.Extracted_Start_Configuration;
                case ConfigurationTypes.Service_Final: return WorkOrder_Events.Extracted_Final_Configuration;
                default:
                    throw new ArgumentException(String.Format("Parameter ctype (ConfigurationTypes) of value: {0} is not valid for this operation.", ctype));
            }
        }
    }
}

