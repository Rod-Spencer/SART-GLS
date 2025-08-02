using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using NLog;
using Segway.Database.Objects;
using Segway.Login.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.SART.Objects;
using Segway.Service.AppSettings.Helper;
using Segway.Service.Authentication.Objects;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.Modules.AddWindow;
using Segway.Service.SART.Client.REST;
using Segway.Syteline.Client.REST;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;

namespace Segway.Modules.WorkOrder.Services
{
    public class Work_Order_Events
    {
        private static Logger logger = Logger_Helper.GetCurrentLogger();
        //private static String FieldPairsPath = "Field Pairs.xml";
        private static String AutoSaveFolder = "Auto Save";



        #region Token

        private static AuthenticationToken _Token;

        /// <summary>Property Token of type AuthenticationToken</summary>
        public static AuthenticationToken Token
        {
            get
            {
                if (_Token == null)
                {
                    if (InfrastructureModule.Container.IsRegistered<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName) == true)
                    {
                        _Token = InfrastructureModule.Container.Resolve<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName);
                    }
                }
                return _Token;
            }
        }

        #endregion

        #region LoginContext

        private static Login_Context _LoginContext = null;
        /// <summary>Property LoginContext of type Login_Context</summary>
        public static Login_Context LoginContext
        {
            get
            {
                if (_LoginContext == null)
                {
                    if (Token != null) _LoginContext = Token.LoginContext;
                }
                return _LoginContext;
            }
        }
        #endregion


        ///// <summary>Property FieldPairs of type Dictionary<String,String></summary>
        //public static Dictionary<String, String> FieldPairs
        //{
        //    get
        //    {
        //        Dictionary<String, String> fp = Serialization.DeserializeFromFile<Dictionary<String, String>>(FieldPairsPath);
        //        if (fp == null)
        //        {
        //            fp = Load_FieldPairs();
        //            Serialization.SerializeToFile<Dictionary<String, String>>(fp, FieldPairsPath);
        //        }
        //        return fp;
        //    }
        //}


        //#region PassFail_FieldPairs

        //private static Dictionary<String, String> _PassFail_FieldPairs;

        ///// <summary>Property PassFail_FieldPairs of type Dictionary<String, String></summary>
        //public static Dictionary<String, String> PassFail_FieldPairs
        //{
        //    get
        //    {
        //        if (_PassFail_FieldPairs == null) _PassFail_FieldPairs = Load_PassFail_FieldPairs();
        //        return _PassFail_FieldPairs;
        //    }
        //}

        //#endregion



        public static void Save_WO_Status_Event(Login_Context logContext, Open_Mode mode)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (InfrastructureModule.Current_Work_Order == null) return;


                //SART_Events statusEvent = new SART_Events();
                //statusEvent.Work_Order_ID = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                //statusEvent.Timestamp = DateTime.Now;
                //statusEvent.User_Name = logContext.UserName;
                //statusEvent.StatusType = Event_Statuses.Passed;

                WorkOrder_Events woEvent = WorkOrder_Events.Unknown;
                if (mode == Open_Mode.Read_Only)
                {
                    woEvent = WorkOrder_Events.View_Work_Order;
                }
                else if (mode == Open_Mode.Cancel)
                {
                    woEvent = WorkOrder_Events.Canceled_Work_Order;
                }
                else if (mode == Open_Mode.Read_Write)
                {
                    //if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Opened_By) == false) //Work_Order_WorkingStatuses.Opened.ToString())
                    //{
                    woEvent = WorkOrder_Events.Opened_Work_Order;
                    //}
                }
                else if (mode == Open_Mode.Close)
                {
                    woEvent = WorkOrder_Events.Closed_Work_Order;
                }
                //else
                //{
                //    woEvent= WorkOrder_Events.Unknown;
                //}

                //statusEvent = SART_2012_Web_Service_Client.Insert_SART_Events_Key(InfrastructureModule.Token, statusEvent);

                if (SART_Common.Create_Event(woEvent, Event_Statuses.Passed) == false)
                //if (statusEvent == null)
                {
                    InfrastructureModule.Aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to save Work Order Event");
                }
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
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

        public static ObservableCollection<SART_Events> Retrieve_Events(String workOrder)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                List<SART_Events> elist = SART_Events_Web_Service_Client_REST.Select_SART_Events_WORK_ORDER_ID(InfrastructureModule.Token, workOrder);
                // reverse the list before returning - then entries to the DB are already ordered by ascending time
                // and we want to display them by descending time
                if ((elist != null) && (elist.Count > 0))
                {
                    elist.Reverse();
                    return new ObservableCollection<SART_Events>(elist);
                }
                return new ObservableCollection<SART_Events>();
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

        public static List<SART_Events> Retrieve_Recent_Events(String workOrder, DateTime dt)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                criteria.Add(new FieldData("Work_Order_ID", workOrder));
                criteria.Add(new FieldData("Timestamp", dt, FieldCompareOperator.GreaterThan));
                List<SART_Events> elist = SART_Events_Web_Service_Client_REST.Select_SART_Events_Criteria(InfrastructureModule.Token, criteria);
                if (elist != null) return elist;
                return new List<SART_Events>();
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
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

        public static void Update_Work_Order() //Boolean updateOracle = true)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (InfrastructureModule.Current_Work_Order == null)
                {
                    logger.Warn("InfrastructureModule.Current_Work_Order is NULL");
                    return;
                }
                SART_Work_Order wo = InfrastructureModule.Current_Work_Order;
                if (Syteline_WO_Web_Service_Client_REST.Update_SART_Work_Order_Object(InfrastructureModule.Token, wo) == false)
                {
                    String msg = $"Unable to save Work Order: {InfrastructureModule.Current_Work_Order.Work_Order_ID}";
                    InfrastructureModule.Aggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
                    throw new Exception(msg);
                }
                InfrastructureModule.Aggregator.GetEvent<WorkOrder_Save_Event>().Publish(true);
                InfrastructureModule.Aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Work Order: {0} has been successfully updated", InfrastructureModule.Current_Work_Order.Work_Order_ID));
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



        //private static Dictionary<String, String> Load_FieldPairs()
        //{
        //    Dictionary<String, String> fld = new Dictionary<string, string>();
        //    fld.Add("ADDITIONAL_NOTES", "ADDITIONAL_NOTES");
        //    fld.Add("A_LOGS", "CU_A_LOG_LINK");
        //    fld.Add("A_SIDE_TIME", "A_SIDE_TIME");
        //    fld.Add("BATT_COMMENTS_1", "BATT_COMMENTS_FRONT");
        //    fld.Add("BATT_COMMENTS_2", "BATT_COMMENTS_REAR");
        //    fld.Add("BATT_PASS_FAIL_1", "BATTERY_STATUS_FRONT");
        //    fld.Add("BATT_PASS_FAIL_2", "BATTERY_STATUS_REAR");
        //    fld.Add("BATT_SERIAL_1", "BATTERY_SERIAL_FRONT");
        //    fld.Add("BATT_SERIAL_2", "BATTERY_SERIAL_REAR");
        //    fld.Add("B_LOGS", "CU_B_LOG_LINK");
        //    fld.Add("B_SIDE_TIME", "B_SIDE_TIME");
        //    fld.Add("CUSTOMER", "CUSTOMER_NAME");
        //    fld.Add("DATE_TIME_ENTERED", "DATE_TIME_ENTERED");
        //    fld.Add("DATE_COMPLETED_1", "DATE_COMPLETED_1");
        //    fld.Add("DATE_COMPLETED_2", "DATE_COMPLETED_2");
        //    fld.Add("DATE_CREATED", "DATE_CREATED");
        //    fld.Add("FAILURE_REASON_1", "FAILURE_REASON_1");
        //    fld.Add("FAILURE_REASON_2", "FAILURE_REASON_2");
        //    fld.Add("HOURS", "HOURS");
        //    fld.Add("HYPER_ELCON", "HYPER_ELCON");
        //    fld.Add("MACHINE_PN", "PT_MODEL");
        //    fld.Add("MACHINE_SN", "PT_SERIAL");
        //    fld.Add("MINUTES", "MINUTES");
        //    fld.Add("NEW_PB_PN", "NEW_PB_PN");
        //    fld.Add("NEW_PB_SN", "NEW_PB_SN");
        //    fld.Add("OBSERVATIONS", "TECHNICIAN_OBSERVATION");
        //    fld.Add("ODOMETER_READING", "ODOMETER_READING");
        //    fld.Add("OPS_COMMENTS", "OPS_COMMENTS");
        //    fld.Add("OPS_REP", "ENTERED_BY");
        //    fld.Add("OWNER", "OWNER");
        //    fld.Add("PB_SERVICE_REQUEST", "PB_SERVICE_REQUEST");
        //    fld.Add("PICTURE_LINKS", "PICTURE_LINKS");
        //    fld.Add("PROBLEM_DESC", "CUSTOMER_COMPLAINT");
        //    fld.Add("QUOTE_NOTES", "QUOTE_NOTES");
        //    fld.Add("RBATT_1", "BATTERY_RBAT_FRONT");
        //    fld.Add("RBATT_2", "BATTERY_RBAT_REAR");
        //    fld.Add("RECEIVED_DAMAGED", "REC_DAMAGED");
        //    fld.Add("RECEIVING_BATTERIES", "REC_BATTERIES");
        //    fld.Add("RECEIVING_CHARGEPORT", "REC_CHARGEPORT");
        //    fld.Add("RECEIVING_COMFORTMATS", "REC_COMFORTMATS");
        //    fld.Add("RECEIVING_CONSOLETRIM", "REC_CONSOLETRIM");
        //    fld.Add("RECEIVING_FENDERS", "REC_FENDERS");
        //    fld.Add("RECEIVING_G1KEYS", "REC_G1_KEYS");
        //    fld.Add("RECEIVING_HUBCAPS", "REC_HUBCAPS");
        //    fld.Add("RECEIVING_INFOKEYS", "REC_INFOKEYS");
        //    fld.Add("RECEIVING_KICKSTAND", "REC_KICKSTAND");
        //    fld.Add("RECEIVING_MATS", "REC_MATS");
        //    fld.Add("RECEIVING_NOTES", "UNIT_CONDITION");
        //    fld.Add("RECEIVING_WHEELS", "REC_WHEELS");
        //    fld.Add("REFURB_PB", "REFURB_PB");
        //    fld.Add("REPAIR_PERFORMED", "REPAIR_PERFORMED");
        //    fld.Add("REPAIR_TYPE", "REPAIR_TYPE");
        //    fld.Add("REPORT_NUMBER", "REPORT_NUMBER");
        //    fld.Add("RMA_NUMBER", "RMA_NUMBER");
        //    fld.Add("SALES_ORDER", "SALES_ORDER");
        //    fld.Add("SERVICE_REQUEST", "SERVICE_REQUEST");
        //    fld.Add("SHIPPED_DATE", "SHIPPED_DATE");
        //    fld.Add("START_DATE_1", "START_DATE_1");
        //    fld.Add("START_DATE_2", "START_DATE_2");
        //    fld.Add("TECH_1", "TECHNICIAN_NAME_1");
        //    fld.Add("TECH_2", "TECHNICIAN_NAME_2");
        //    fld.Add("TIRE_PASS_FAIL_2", "TIRE_PASS_FAIL_2");
        //    fld.Add("TIRE_PSI", "TIRE_PSI_LEFT");
        //    fld.Add("TIRE_PSI1", "TIRE_PSI_RIGHT");
        //    fld.Add("TRANSACTION_DATE", "TRANSACTION_DATE");
        //    return fld;
        //}

        //private static PropertyInfo FindProperty(Type ST, String propertyName)
        //{
        //    foreach (PropertyInfo pi in ST.GetProperties())
        //    {
        //        if (pi.Name.ToUpper() == propertyName.ToUpper()) return pi;
        //    }
        //    return null;
        //}

        //private static Dictionary<String, String> Load_PassFail_FieldPairs()
        //{
        //    Dictionary<String, String> _FieldPairs = new Dictionary<string, string>();
        //    _FieldPairs.Add("CSB_DIAG", "DIAG_CSB");
        //    _FieldPairs.Add("CSB_DIAG1", "DIAG_CSB_1");
        //    _FieldPairs.Add("HIPOT_DIAG", "DIAG_HIPOT");
        //    _FieldPairs.Add("HIPOT_DIAG1", "DIAG_HIPOT_1");
        //    _FieldPairs.Add("MOTOR_DIAG", "DIAG_MOTOR");
        //    _FieldPairs.Add("MOTOR_DIAG1", "DIAG_MOTOR_1");
        //    _FieldPairs.Add("POWERBASE_DIAG", "DIAG_POWERBASE");
        //    _FieldPairs.Add("POWERBASE_DIAG1", "DIAG_POWERBASE_1");
        //    _FieldPairs.Add("UI_DIAG", "DIAG_UI");
        //    _FieldPairs.Add("UI_DIAG1", "DIAG_UI_1");
        //    _FieldPairs.Add("NORM_RATIO", "DIAG_NORM_RATIO");
        //    _FieldPairs.Add("NORM_RATIO1", "DIAG_NORM_RATIO_1");

        //    return _FieldPairs;
        //}

        //private static int Compare_Objects(Object o1, Object o2)
        //{
        //    if (o1 == null) o1 = "";
        //    if (o2 == null) o2 = "";
        //    String s1 = o1.ToString();
        //    String s2 = o2.ToString();

        //    return String.Compare(s1, s2);
        //}


        public static void Cancel_Current_Work_Order(IEventAggregator aggregator, IRegionManager regionManager, Boolean Show_Warning_Message)
        {
            try
            {
                logger.Debug("Entered");

                Remove_and_Clear_AutoSave(aggregator);
                if (InfrastructureModule.WorkOrder_OpenMode == Open_Mode.Read_Write)
                {
                    if ((InfrastructureModule.Original_Work_Order != null) &&
                        (SART_Work_Order.Compare(InfrastructureModule.Original_Work_Order, InfrastructureModule.Current_Work_Order, true) == false))
                    {
                        try
                        {
                            logger.Debug("Show_Warning_Message: {0}", Show_Warning_Message);
                            if (Show_Warning_Message == true)
                            {
                                String msg = String.Format("The current work order: {0} has changed. Continuing with the Cancel will cause a loss of data.\n\nDo you wish to continue?", InfrastructureModule.Current_Work_Order.Work_Order_ID);
                                MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Hand);
                                if (result == MessageBoxResult.No)
                                {
                                    return;
                                }
                            }
                        }
                        finally
                        {
                            //Show_Warning_Message = true;
                        }
                    }
                    logger.Debug("Closing Work Order: {0}", InfrastructureModule.Current_Work_Order.Work_Order_ID);
                    if (Syteline_WO_Web_Service_Client_REST.Close_Work_Order(InfrastructureModule.Token, InfrastructureModule.Current_Work_Order.Work_Order_ID) == false)
                    {
                        System.Windows.MessageBox.Show("An error occurred while cancelling the current work order.  Please try again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }
                InfrastructureModule.WorkOrder_OpenMode = Open_Mode.Cancel;

                Work_Order_Events.Save_WO_Status_Event(LoginContext, Open_Mode.Cancel);

                Remove_Registered_Instance_From_Container("Configurations");

                Set_ToolBar(false, false, InfrastructureModule.Aggregator);
                InfrastructureModule.Current_Work_Order = null;
                InfrastructureModule.Aggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Publish(true);
                if (regionManager != null) regionManager.RequestNavigate(RegionNames.MainRegion, Work_Order_Open_Control.Control_Name);
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                InfrastructureModule.Aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
                return;
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


        public static void Remove_and_Clear_AutoSave(IEventAggregator aggregator)
        {
            aggregator.GetEvent<WorkOrder_AutoSave_Event>().Publish(false);
            //Application_Helper.DoEvents();
            //aggregator.GetEvent<WorkOrder_AutoSave_Delete_Event>().Publish(true);
            //Application_Helper.DoEvents();
        }


        public static void Set_ToolBar(Boolean open, Boolean config, IEventAggregator aggregator)
        {
            SART_ToolBar_Group_Manager.IsOpen = open;
            SART_ToolBar_Group_Manager.IsStartConfig = config;

            Show_Toolbar(aggregator);
        }

        private static void Show_Toolbar(IEventAggregator aggregator)
        {
            aggregator.GetEvent<ToolBar_Activate_Permission_Event>().Publish(SART_ToolBar_Group_Manager.GetGroupName());
            //ToolBar_Group_Manager.Select_Group(aggregator, SART_ToolBar_Group_Manager.GetGroupName);
        }


        public static void Close_Work_Order(IEventAggregator aggregator)
        {
            if ((InfrastructureModule.Current_Work_Order != null) && (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Work_Order_ID) == false))
            {
                logger.Debug("Closing: {0}", InfrastructureModule.Current_Work_Order.Work_Order_ID);
                InfrastructureModule.Current_Work_Order.Opened_By = null; // Work_Order_WorkingStatuses.Closed.ToString();// WorkOrderStatuses.Closed;
                InfrastructureModule.Current_Work_Order.Updated_By = LoginContext.UserName;
                InfrastructureModule.Current_Work_Order.Date_Time_Updated = DateTime.Now;
                Work_Order_Events.Update_Work_Order();

                Set_ToolBar(false, false, aggregator);

                Work_Order_Events.Save_WO_Status_Event(LoginContext, Open_Mode.Close);
                aggregator.GetEvent<WO_Config_Clear_Event>().Publish(true);

                String regName = "Configurations";
                Remove_Registered_Instance_From_Container(regName);
            }
        }

        public static void Remove_Registered_Instance_From_Container(String regName)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                foreach (var reg in InfrastructureModule.Container.Registrations)
                {
                    if (reg.Name == regName)
                    {
                        logger.Debug("Removing: {0}", regName);
                        reg.LifetimeManager.RemoveValue();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        public static void Open_WorkOrder(Boolean navigate)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (InfrastructureModule.WorkOrder_OpenMode == Open_Mode.Read_Write)
                {
                    if (InfrastructureModule.Current_Work_Order == null)
                    {
                        SART_Work_Order sro = Syteline_WO_Web_Service_Client_REST.Select_SART_Work_Order_WORK_ORDER_ID(InfrastructureModule.Token, InfrastructureModule.Current_Work_Order.Work_Order_ID);
                        if (String.IsNullOrEmpty(sro.Opened_By) == false)
                        {
                            if (sro.Opened_By != LoginContext.UserName)
                            {
                                if (Application.Current != null)
                                {
                                    if (Application.Current.Dispatcher != null)
                                    {
                                        Application.Current.Dispatcher.Invoke((Action)delegate ()
                                        {
                                            Message_Window.Warning($"SRO: {InfrastructureModule.Current_Work_Order.Work_Order_ID} is already opened by: {sro.Opened_By}").ShowDialog();
                                            return;
                                        });
                                    }
                                }
                            }
                        }
                    }
                    InfrastructureModule.Current_Work_Order.Opened_By = LoginContext.UserName; // Work_Order_WorkingStatuses.Opened.ToString(); //WorkOrderStatuses.Opened;
                    InfrastructureModule.Current_Work_Order.Updated_By = LoginContext.UserName;
                    InfrastructureModule.Current_Work_Order.Date_Time_Updated = DateTime.Now;

                    if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Technician_Name) == true)
                    {
                        InfrastructureModule.Current_Work_Order.Technician_Name = LoginContext.UserName;
                    }
                    //else
                    //{
                    //    InfrastructureModule.Current_Work_Order.Technician_Name_2 = LoginContext.UserName;
                    //}

                    if (InfrastructureModule.Current_Work_Order.Start_Date.HasValue == false) InfrastructureModule.Current_Work_Order.Start_Date = DateTime.Now;

                    Work_Order_Events.Update_Work_Order();
                }

                Work_Order_Events.Save_WO_Status_Event(LoginContext, InfrastructureModule.WorkOrder_OpenMode);


                ///////////////////////////////////////////////////////////////////
                // Select ToolBar Group
                //ToolBar_Group_Manager.Select_Group(InfrastructureModule.Aggregator, SART_ToolBar_Group_Manager.GetGroupName);
                //InfrastructureModule.Aggregator.GetEvent<ToolBar_Activate_Permission_Event>().Publish(SART_ToolBar_Group_Manager.GetGroupName());
                // Select ToolBar Group
                ///////////////////////////////////////////////////////////////////

                SART_ToolBar_Group_Manager.IsOpen = true;
                Boolean config = (InfrastructureModule.Current_Work_Order.Is_Start_Config == true) ||
                    (InfrastructureModule.Current_Work_Order.Config_Start_Override == true) ||
                    (InfrastructureModule.Current_Work_Order.Config_Final_Override == true);
                Boolean open = ((SART_ToolBar_Group_Manager.IsOpen == true) && (InfrastructureModule.Current_Work_Order.Status == "O"));
                Set_ToolBar(open, config, InfrastructureModule.Aggregator);

                //InfrastructureModule.Container.RegisterInstance<SART_Work_Order>("Current_Work_Order", InfrastructureModule.Current_Work_Order);
                InfrastructureModule.Aggregator.GetEvent<WorkOrder_Opened_Event>().Publish(true);
                if (navigate == true)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        InfrastructureModule.RegionManager.RequestNavigate(RegionNames.MainRegion, Work_Order_Summary_Control.Control_Name);
                    });
                }
                InfrastructureModule.Aggregator.GetEvent<WorkOrder_AutoSave_Event>().Publish(true);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                InfrastructureModule.Aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        public static void AutoSave_WorkOrder(object sender, EventArgs e)
        {
            if (InfrastructureModule.Current_Work_Order == null)
            {
                logger.Warn("InfrastructureModule.Current_Work_Order is NULL");
                return;
            }
            if (Configuration_Helper.GetAppSettingBoolean("Auto Save") == false) return;
            if (Syteline_WO_Web_Service_Client_REST.Update_SART_Work_Order_Object(Token, InfrastructureModule.Current_Work_Order) == false)
            {
                logger.Error("Auto Save failed to update Work Order");
            }


            //String path = Path.Combine(Application_Helper.Application_Folder_Name(), AutoSaveFolder, String.Format("{0}.xml", InfrastructureModule.Current_Work_Order.Work_Order_ID));
            //FileInfo autoSave = new FileInfo(path);
            //if (autoSave.Directory.Exists == false) autoSave.Directory.Create();
            //else if (autoSave.Exists) autoSave.Delete();
            //Serialization.SerializeToFile<SART_Work_Order>(InfrastructureModule.Current_Work_Order, autoSave.FullName);
        }

        //public static void Delete_AutoSave()
        //{
        //    if (InfrastructureModule.Current_Work_Order == null) return;
        //    String path = Path.Combine(Application_Helper.Application_Folder_Name(), AutoSaveFolder, String.Format("{0}.xml", InfrastructureModule.Current_Work_Order.Work_Order_ID));
        //    FileInfo autoSave = new FileInfo(path);
        //    if (autoSave.Directory.Exists == false) autoSave.Directory.Create();
        //    else if (autoSave.Exists) autoSave.Delete();
        //}


        //public static Boolean Test_For_AutoSave(String woid)
        //{
        //    String path = Path.Combine(Application_Helper.Application_Folder_Name(), AutoSaveFolder, String.Format("{0}.xml", woid));
        //    FileInfo autoSave = new FileInfo(path);
        //    if (autoSave.Directory.Exists == false)
        //    {
        //        autoSave.Directory.Create();
        //        return false;
        //    }
        //    else if (autoSave.Exists == false)
        //    {
        //        return false;
        //    }
        //    return true;
        //}


        //public static SART_Work_Order Read_AutoSave(String woid)
        //{
        //    String path = Path.Combine(Application_Helper.Application_Folder_Name(), AutoSaveFolder, String.Format("{0}.xml", woid));
        //    FileInfo autoSave = new FileInfo(path);
        //    if (autoSave.Directory.Exists == false)
        //    {
        //        autoSave.Directory.Create();
        //        return null;
        //    }
        //    else if (autoSave.Exists == false)
        //    {
        //        return null;
        //    }
        //    return Serialization.DeserializeFromFile<SART_Work_Order>(autoSave.FullName);
        //}
    }
}
