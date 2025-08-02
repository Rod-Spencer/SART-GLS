using Microsoft.Practices.Unity;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.WorkOrder;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.CAN;
using Segway.Service.CAN.Objects;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using System;
using System.Reflection;

namespace Segway.Modules.SART.CodeLoad
{
    public partial class CU_Code_ViewModel
    {
        /// <summary>Public Method - Load_CU_Code</summary>
        /// <param name="side">CAN_CU_Sides</param>
        /// <param name="jtags">JTag_Data - </param>
        /// <returns>Boolean</returns>
        public Boolean Load_CU_Code(CAN_CU_Sides side, JTag jtags)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                logger.Info("Creating instance of SART_Event_Object");
                Boolean status = false;

                SART_Event_Object obj = new SART_Event_Object(InfrastructureModule.Current_Work_Order.Work_Order_ID,
                                                              Event_Types.Load_CUA_Code + (side - CAN_CU_Sides.A),
                                                              Event_Statuses.In_Progress,
                                                              InfrastructureModule.Token.LoginContext.UserName);

                logger.Info("Inserting instance of SART_Event_Object to DB");
                obj = SART_EVOBJ_Web_Service_Client_REST.Insert_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                if (obj == null)
                {
                    String msg = "Unable to insert Event Object";
                    logger.Error(msg);
                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
                    return false;
                }
                logger.Info("SART Event Object ID: {0}", obj.ID);
                container.RegisterInstance<int>("ObjectID", obj.ID, new ContainerControlledLifetimeManager());
                aggregator.GetEvent<CU_Load_Code_EventID_Event>().Publish(obj.ID);

                logger.Info("Establishing CAN connection");
                CAN2 can = SART_Common.EstablishConnection(obj.ID);
                if (can != null)
                {
                    try
                    {
                        if (CAN2_Commands.Continue_Processing == true)
                        {
                            logger.Info("Loading CU Code");
                            /////////////////////////////////////////////////////////////////////
                            // Load CU Code
                            status = CAN2_Commands.CU_Load_Code(can, side, jtags, SART_Common.Start_Process, SART_Common.End_Process);
                            // Load CU Code
                            /////////////////////////////////////////////////////////////////////

                            if (status == true)
                            {
                                /////////////////////////////////////////////////////////////////////
                                // Extract CU Code Information
                                logger.Info("Extracting Code Information");
                                status = CAN2_Commands.CU_Extract_Build_Information(can, side, true, SART_Common.Start_Process, SART_Common.End_Process);
                                // Extract CU Code Information
                                /////////////////////////////////////////////////////////////////////
                            }

                        }
                    }
                    catch (Authentication_Exception ae)
                    {
                        logger.Warn(Exception_Helper.FormatExceptionString(ae));
                        throw;
                    }
                    finally
                    {
                        logger.Info("Closing CAN connection");
                        SART_Common.ClosingConnection(obj.ID, can);
                    }

                }

                obj.Timestamp_End = DateTime.Now;
                obj.EventStatus = Event_Statuses.Finished;
                SART_EVOBJ_Web_Service_Client_REST.Update_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                SART_Common.Create_Event(WorkOrder_Events.Loaded_CUA_Code + (side - CAN_CU_Sides.A), status ? Event_Statuses.Passed : Event_Statuses.Failed, obj.ID);
                //SART_Events logevent = new SART_Events();
                //logevent.EventType = WorkOrder_Events.Loaded_CUA_Code + (side - CAN_CU_Sides.A);
                //logevent.Object_ID = obj.ID;
                //logevent.Work_Order_ID = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                //logevent.User_Name = User_Info.UserName;
                //logevent.Timestamp = DateTime.Now;
                //logevent.StatusType = status ? Event_Statuses.Passed : Event_Statuses.Failed;
                //logevent = SART_2012_Web_Service_Client.Insert_SART_Events_Key(InfrastructureModule.Token, logevent);
                aggregator.GetEvent<CU_Load_Code_EventID_Event>().Publish(0);
                aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
                return status;
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
