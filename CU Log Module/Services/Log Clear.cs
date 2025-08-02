using Microsoft.Practices.Unity;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.WorkOrder;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.CAN;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using System;
using System.Reflection;

namespace Segway.Modules.CU_Log_Module
{
    public partial class Log_Extract
    {
        public static Boolean Clear_CU_Log(CU_Log_Extraction_View_Model vm, out String cuSerial, CAN_CU_Sides side = CAN_CU_Sides.A)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                _CULogViewModel = vm;
                cuSerial = null;
                Boolean pass = true;
                logger.Debug("Registering object instances: Work_Order, aggregator");

                logger.Debug("Creating instance of SART_Event_Object");
                SART_Event_Object obj = new SART_Event_Object(InfrastructureModule.Current_Work_Order.Work_Order_ID,
                                                              Event_Types.Extract_CUA_Log,
                                                              Event_Statuses.In_Progress,
                                                              InfrastructureModule.Token.LoginContext.UserName);
                logger.Debug("Inserting instance of SART_Event_Object to DB");
                obj = SART_EVOBJ_Web_Service_Client_REST.Insert_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                if (obj == null)
                {
                    pass = false;
                    String msg = "Unable to insert Event Object";
                    logger.Error(msg);
                    InfrastructureModule.Aggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
                    return false;
                }
                logger.Debug("SART Event Ojbect ID: {0}", obj.ID);
                CU_Log_Module.CU_Log_Module_Container.RegisterInstance<int>("ObjectID", obj.ID, new ContainerControlledLifetimeManager());
                InfrastructureModule.Aggregator.GetEvent<CU_Log_EventID_Event>().Publish(obj.ID);


                logger.Debug("Establishing CAN connection for side: {0}", side);
                CAN2 can = SART_Common.EstablishConnection(obj.ID);

                if (can != null)
                {
                    ////////////////////////////////////
                    logger.Debug("Clearing CU Log");
                    pass = CAN2_Commands.Clear_CU_Log(can, InfrastructureModule.Current_Work_Order.PT_Serial, out cuSerial, side, SART_Common.Start_Process, SART_Common.End_Process, Validate_Process);
                    while (pass)
                    {
                        break;
                    }
                    ////////////////////////////////////
                }
                else
                {
                    pass = false;
                }

                logger.Debug("Closing CAN connection");
                SART_Common.ClosingConnection(obj.ID, can);


                obj.Timestamp_End = DateTime.Now;
                obj.EventStatus = Event_Statuses.Finished;
                SART_EVOBJ_Web_Service_Client_REST.Update_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                //SART_Events logevent = new SART_Events();
                //logevent.EventType = (WorkOrder_Events)((int)WorkOrder_Events.Cleared_CUA_Log + (int)side - (int)CAN_CU_Sides.A);
                //logevent.Object_ID = obj.ID;
                //logevent.Work_Order_ID = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                //logevent.User_Name = user.UserName;
                //logevent.Timestamp = DateTime.Now;
                //logevent.StatusType = pass ? Event_Statuses.Passed : Event_Statuses.Failed;
                //logevent = SART_2012_Web_Service_Client.Insert_SART_Events_Key(InfrastructureModule.Token, logevent);
                var sts = SART_Common.Create_Event(WorkOrder_Events.Cleared_CUA_Log + (side - CAN_CU_Sides.A), pass ? Event_Statuses.Passed : Event_Statuses.Failed, obj.ID);
                if (sts == true)
                {
                    InfrastructureModule.Aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
                }

                InfrastructureModule.Aggregator.GetEvent<CU_Log_EventID_Event>().Publish(0);
                return true;
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
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
    }
}
