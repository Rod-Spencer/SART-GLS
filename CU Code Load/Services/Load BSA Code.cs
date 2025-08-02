using Segway.Modules.SART_Infrastructure;
using Segway.SART.Objects;
using Segway.Service.CAN;
using Segway.Service.CAN.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.Tools.CAN2;
using System;
using System.Reflection;


namespace Segway.Modules.SART.CodeLoad
{
    public partial class CU_Code_ViewModel
    {
        //BSASoftwareVersionDataManager BSA_SW_dataManager = null;

        /// <summary>Public Method - Load_BSA_Code</summary>
        /// <param name="side">CAN_CU_Sides</param>
        /// <param name="jtags">Security</param>
        /// <param name="obj">SART_Event_Object</param>
        /// <returns>Boolean</returns>
        public Boolean Load_BSA_Code(CAN_CU_Sides side, JTag jtags, String keyCode, SART_Event_Object obj)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                logger.Debug("Establishing CAN connection");
                CAN2 can = SART_Common.EstablishConnection(obj.ID);
                if (can == null)
                {
                    logger.Warn("Establishing CAN connection - Failed");
                    return false;
                }
                try
                {
                    if (CAN2_Commands.Continue_Processing == true)
                    {
                        logger.Debug("Loading BSA code");
                        /////////////////////////////////////////////////////////////////////////////////
                        // BSA Load Code
                        if (CAN2_Commands.BSA_Load_Code(can, side, jtags, keyCode, InfrastructureModule.Current_Work_Order.PT_Serial, SART_Common.Start_Process, SART_Common.End_Process, SART_Common.Calibration_Process) == false)
                        {
                            logger.Warn("Loading BSA code - Failed");
                            return false;
                        }
                        logger.Debug("Loading BSA code - Successful");
                        return true;
                        // BSA Load Code
                        /////////////////////////////////////////////////////////////////////////////////



                        /////////////////////////////////////////////////////////////////////////////////
                        // This section has been moved to the parent routine
                        /////////////////////////////////////////////////////////////////////////////////
                        //if (status == true && side == CAN_CU_Sides.B)
                        //{
                        //    // Extract BSA Code Information
                        //    status &= CAN2_Commands.BSA_Extract_Code_Information(can, SART_Common.Start_Process, SART_Common.End_Process);
                        //}
                        /////////////////////////////////////////////////////////////////////////////////

                    }

                    return false;
                }
                finally
                {
                    logger.Debug("Closing CAN connection");
                    SART_Common.ClosingConnection(obj.ID, can);
                }
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


#if DoNotDo
        private Boolean CleanupWatch(Segway.Service.Tools.CAN2.CAN2_Commands.StartProcess StartIndicator = null, Segway.Service.Tools.CAN2.CAN2_Commands.EndProcess EndIndicator = null, DataRecievedEventHandler handler = null)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                BSA_SW_dataManager.DataRecieved -= handler;
                BSA_SW_dataManager.Unload();
                return true;
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

        private Boolean SetupWatch(CAN2 can, Segway.Service.Tools.CAN2.CAN2_Commands.StartProcess StartIndicator = null, Segway.Service.Tools.CAN2.CAN2_Commands.EndProcess EndIndicator = null, DataRecievedEventHandler handler = null)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                WatchManager wm = new WatchManager(can);
                Diagnostics diag = new Diagnostics(can, wm);
                if (diag.EnterDiagnosticMode(PowerUpModes.POWER_UP_WIRED) == false)
                {
                    String strSub = "";
                    if (!diag.IsAInDiagnosticMode && !diag.IsBInDiagnosticMode) strSub += " Side-A and Side-B";
                    else if (!diag.IsAInDiagnosticMode) strSub += " Side-A";
                    else if (!diag.IsBInDiagnosticMode) strSub += " Side-B";
                    if (EndIndicator != null) EndIndicator(CAN_Processes.Entering_Diagnostic_Mode, false, String.Format("Failed to enter diagnostic mode, {0}. Check Connections", strSub));
                    return false;
                }
                if (EndIndicator != null) EndIndicator(CAN_Processes.Entering_Diagnostic_Mode, true);
                //Application_Helper.DoEvents();

                if (StartIndicator != null) StartIndicator(CAN_Processes.Init_Data_Manager);
                //Application_Helper.DoEvents();

                BSA_SW_dataManager = new BSASoftwareVersionDataManager(wm);
                if (BSA_SW_dataManager.Initialize() == false)
                {
                    if (EndIndicator != null) EndIndicator(CAN_Processes.Init_Data_Manager, false);
                    logger.Debug("Failed to initialize BSASoftwareVersionDataManager");
                    return false;
                }
                if (EndIndicator != null) EndIndicator(CAN_Processes.Init_Data_Manager, true);
                //Application_Helper.DoEvents();

                BSA_SW_dataManager.DataRecieved += handler;
                //Application_Helper.DoEvents();

                return true;
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
#endif
    }
}
