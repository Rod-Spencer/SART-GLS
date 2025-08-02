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
    /// <summary>Public Property - CU_Code_ViewModel</summary>
    public partial class CU_Code_ViewModel
    {
        /// <summary>Public Method - Reset_BSA_Code</summary>
        /// <param name="side">CAN_CU_Sides</param>
        /// <param name="secRec">Security</param>
        /// <returns>Boolean</returns>
        public Boolean Reset_BSA_Code(CAN_CU_Sides side, JTag_Data secRec)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                logger.Debug("Creating instance of SART_Event_Object");
                Boolean status = false;

                SART_Event_Object obj = new SART_Event_Object(InfrastructureModule.Current_Work_Order.Work_Order_ID,
                                                              Event_Types.Reset_BSAA_Code + (side - CAN_CU_Sides.A),
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
                aggregator.GetEvent<CU_Load_Code_EventID_Event>().Publish(obj.ID);

                logger.Debug("Establishing CAN connection");
                CAN2 can = SART_Common.EstablishConnection(obj.ID);
                if (can != null)
                {
                    try
                    {
                        if (CAN2_Commands.Continue_Processing == true)
                        {
                            logger.Debug("Resetting BSA code");
                            // BSA Load Code
                            status = CAN2_Commands.BSA_Reset_Code(can, side, secRec.BSA, true, true, SART_Common.Start_Process, SART_Common.End_Process);
                            //
                            //if (status)
                            //{
                            //    // First Close the Loader_Loader one
                            //    LogFile_Helper.Close();
                            //    LogFile_Helper.Open("BSA Reset Code2.log");

                            //    // Load COFF file
                            //    SqlBooleanList criteria = new SqlBooleanList();
                            //    criteria.Add(new FieldData("Generation", PTGeneration.Gen2.ToString()));
                            //    criteria.Add(new FieldData("Component", "BSA"));
                            //    criteria.Add(new FieldData("Type", PT_Code_Type.Boot_Loader.ToString()));
                            //    criteria.Add(new FieldData("Build_Date", "2012-05-23"));
                            //    List<SART_COFF_Files> coffList = SART_2012_Web_Service_Client.Select_SART_COFF_Files_Criteria(InfrastructureModule.Token, criteria);
                            //    if ((coffList == null) || (coffList.Count == 0))
                            //    {
                            //        aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to retrieve BSA Boot Loader file");
                            //        return false;
                            //    }
                            //    if (COFF_File.Load(coffList[coffList.Count - 1].Data, PTGeneration.Gen2, "BSA", PT_Model_Types.NotDefined, PT_Code_Type.Boot_Loader) == false)
                            //    {
                            //        aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load BSA Boot Loader file");
                            //        return false;
                            //    }
                            //    //
                            //    status = CAN2_Commands.BSA_Reset_Code(can, side, secRec, false, true, SART_Common.Start_Process, SART_Common.End_Process);
                            //}
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
                SART_Common.Create_Event(WorkOrder_Events.Reset_BSAA + (side - CAN_CU_Sides.A), status ? Event_Statuses.Passed : Event_Statuses.Failed, obj.ID);

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
