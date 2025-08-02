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
        /// <summary>Public Method - Reset_CU_Code</summary>
        /// <param name="side">CAN_CU_Sides</param>
        /// <param name="jtags">Security</param>
        /// <returns>Boolean</returns>
        public Boolean Reset_CU_Code(CAN_CU_Sides side, JTag jtags)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                logger.Debug("Creating instance of SART_Event_Object");
                Boolean status = false;

                SART_Event_Object obj = new SART_Event_Object(InfrastructureModule.Current_Work_Order.Work_Order_ID,
                                                              Event_Types.Reset_CUA_Code + (side - CAN_CU_Sides.A),
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
                container.RegisterInstance<int>("ObjectID", obj.ID, new ContainerControlledLifetimeManager());
                aggregator.GetEvent<CU_Reset_Code_EventID_Event>().Publish(obj.ID);

                logger.Debug("Establishing CAN connection");
                CAN2 can = SART_Common.EstablishConnection(obj.ID);
                if (can != null)
                {
                    try
                    {
                        if (CAN2_Commands.Continue_Processing == true)
                        {
                            logger.Debug("Extracting CU Log");
                            /////////////////////////////////////////////////////////////////////
                            // Load CU Code
                            status = CAN2_Commands.CU_Load_Code(can, side, jtags, SART_Common.Start_Process, SART_Common.End_Process);
                            // Load CU Code
                            /////////////////////////////////////////////////////////////////////
                        }
                    }
                    catch (Authentication_Exception ae)
                    {
                        logger.Warn(Exception_Helper.FormatExceptionString(ae));
                        throw;
                    }
                    finally
                    {
                        logger.Debug("Closing CAN connection");
                        SART_Common.ClosingConnection(obj.ID, can);
                    }
                }

                obj.Timestamp_End = DateTime.Now;
                obj.EventStatus = Event_Statuses.Finished;
                SART_EVOBJ_Web_Service_Client_REST.Update_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                SART_Common.Create_Event(WorkOrder_Events.Reset_CUA + (side - CAN_CU_Sides.A), status ? Event_Statuses.Passed : Event_Statuses.Failed, obj.ID);
                aggregator.GetEvent<CU_Reset_Code_EventID_Event>().Publish(0);
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
