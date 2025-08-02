using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Forms;

using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;

using Segway.Modules.WorkOrder;
using Segway.Modules.WorkOrder.Services;
using Segway.Modules.ShellControls;
using Segway.Service.Controls.StatusBars;
using Segway.Modules.SART_Infrastructure;
using Segway.Service.Common.LoggerHelp;
using Segway.Service.Controls.ListBoxToolBar;
using Segway.Modules.Login;
using Segway.Service.Objects;
using Segway.SART.Objects;
using Segway.Service.DatabaseHelper;
using Segway.Service.SART2012.Client;
using Segway.Service.Common;
using Segway.Service.WebService.Client;
using Segway.Service.Manufacturing.Client;
using Segway.Service.HeartBeat.Client;
using Segway.Manufacturing.WebService.Client;
using Segway.Service.Login.Client;
using System.Windows.Media.Imaging;
using Segway.Service.Tools.CAN2;
using Segway.Service.G2_Logs.Client;
using System.IO;
using Segway.Service.Modules.AddWindow;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Media;
using Segway.Syteline.Objects;
using Segway.Service.CAN.Enumerations;
using System.Windows.Controls;
using SART_Infrastructure;


namespace Segway.Modules.WorkOrder
{
    public class WorkOrderSummaryViewModel : ViewModelBase, IWorkOrderSummaryViewModel, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;

        //public IEventAggregator Aggregator { get { return eventAggregator; } }

        //   private int LoadCounter = 0;


        public WorkOrderSummaryViewModel(IWorkOrderSummaryView view, IRegionManager regionManager, IUnityContainer container, IEventAggregator aggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = aggregator;

            eventAggregator.GetEvent<WorkOrder_Instance_Event>().Subscribe(Load_Work_Order, true);
            eventAggregator.GetEvent<Shell_Close_Event>().Subscribe(Close_SART, true);
            eventAggregator.GetEvent<WorkOrder_Read_CU_Log_Event>().Subscribe(Read_CU_Log, true);
            eventAggregator.GetEvent<Application_Logout_Event>().Subscribe(ApplicationLogout, true);
            eventAggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Subscribe(Update_Audit, true);
            eventAggregator.GetEvent<WorkOrder_Configuration_Start_Event>().Subscribe(UpdateConfigStart, true);
            eventAggregator.GetEvent<WorkOrder_Configuration_Final_Event>().Subscribe(UpdateConfigFinal, true);
            eventAggregator.GetEvent<WorkOrder_Configuration_Event>().Subscribe(Add_PT_Config, true);
            eventAggregator.GetEvent<WorkOrder_Configuration_Refresh_Event>().Subscribe(Config_Refresh_Handler, true);
            eventAggregator.GetEvent<SART_CU_CodeLoad_A_Event>().Subscribe(Received_CU_CodeLoad_A_Notice, true);
            eventAggregator.GetEvent<SART_CU_CodeLoad_B_Event>().Subscribe(Received_CU_CodeLoad_B_Notice, true);
            eventAggregator.GetEvent<SART_BSA_CodeLoad_Event>().Subscribe(Received_BSA_CodeLoad_Notice, true);

            eventAggregator.GetEvent<SART_MotorTest_Done_Event>().Subscribe(Received_MotorTest_Done_Notice, true);
            eventAggregator.GetEvent<SART_BSATest_Done_Event>().Subscribe(Received_BSATest_Done_Notice, true);
            eventAggregator.GetEvent<SART_RiderDetectTest_Done_Event>().Subscribe(Received_RiderDetectTest_Done_Notice, true);
            eventAggregator.GetEvent<SART_LEDTest_Done_Event>().Subscribe(Received_LEDTest_Done_Notice, true);
            eventAggregator.GetEvent<WorkOrder_Status_Change_Event>().Subscribe(Status_Change_Handler, true);
            eventAggregator.GetEvent<WorkOrder_Cancel_Event>().Subscribe(WorkOrder_Cancel_Handler, true);

            logger.Debug("Entered the NewWorkOrder constructor");

            ObservationSaveCommand = new DelegateCommand(CommandObservationSave, CanCommandObservationSave);
            SaveCommand = new DelegateCommand(CommandSave, CanCommandSave);
            CloseCommand = new DelegateCommand(CommandClose, CanCommandClose);
            CancelCommand = new DelegateCommand(CommandCancel, CanCommandCancel);
            ConfigStartCommand = new DelegateCommand(CommandConfigStart, CanCommandConfigStart);
            ConfigFinalCommand = new DelegateCommand(CommandConfigFinal, CanCommandConfigFinal);
            PictureOpenCommand = new DelegateCommand(CommandPictureOpen, CanCommandPictureOpen);
            PictureAddCommand = new DelegateCommand(CommandPictureAdd, CanCommandPictureAdd);
            PictureDelCommand = new DelegateCommand(CommandPictureDel, CanCommandPictureDel);
            PictureEditCommand = new DelegateCommand(CommandPictureEdit, CanCommandPictureEdit);

            WO_CopyCommand = new DelegateCommand(CommandWO_Copy, CanCommandWO_Copy);
            SN_CopyCommand = new DelegateCommand(CommandSN_Copy, CanCommandSN_Copy);
        }

        #region Miscellaneous Properties

        public Boolean DisclaimerAccepted { get; set; }


        #endregion


        #region Control Properties


        #endregion


        #region Naviation Implementation


        #endregion



        #region Command Handlers

        #region ObservationSaveCommand

        /// <summary>Delegate Command: ObservationSaveCommand</summary>
        public DelegateCommand ObservationSaveCommand { get; set; }

        private Boolean CanCommandObservationSave()
        {
            if (SelectedTabIndex == 0) return false;
            if (InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write) return false;

            if (SelectedTabIndex == 1)
            {
                if (String.IsNullOrWhiteSpace(Current_Unit_Condition) == true) return false;
            }
            else if (SelectedTabIndex == 2)
            {
                if (String.IsNullOrWhiteSpace(Current_Technician_Observations) == true) return false;
            }
            else if (SelectedTabIndex == 4)
            {
                if (String.IsNullOrWhiteSpace(Current_Segway_Observations) == true) return false;
            }
            else if (SelectedTabIndex == 3)
            {
                if (String.IsNullOrWhiteSpace(Current_Error_Code_Observations) == true) return false;
            }
            return true;
        }

        private void CommandObservationSave()
        {
            if (SelectedTabIndex == 1)
            {
                // Technician Observations
                Summary_UnitCondition += String.Format("\n{0} - {1}:\n{2}\n", LoginContext.UserName, DateTime.Now, Current_Unit_Condition);
                Current_Unit_Condition = "";
            }
            else if (SelectedTabIndex == 2)
            {
                // Technician Observations
                Summary_Technician_Observations += String.Format("\n{0} - {1}:\n{2}\n", LoginContext.UserName, DateTime.Now, Current_Technician_Observations);
                //Work_Order_Events.Update_Work_Order();
                Current_Technician_Observations = "";
            }
            else if (SelectedTabIndex == 4)
            {
                // Segway Observations
                Summary_Segway_Observations += String.Format("\n{0} - {1}:\n{2}\n", LoginContext.UserName, DateTime.Now, Current_Segway_Observations);
                //Work_Order_Events.Update_Work_Order();
                Current_Segway_Observations = "";
            }
            else if (SelectedTabIndex == 3)
            {
                // Segway Observations
                Error_Code_Observations += String.Format("\n{0} - {1}:\n{2}\n", LoginContext.UserName, DateTime.Now, Current_Error_Code_Observations);
                //Work_Order_Events.Update_Work_Order();
                Current_Error_Code_Observations = "";
            }
        }

        #endregion

        #region CloseCommand

        /// <summary>Delegate Command: CloseCommand</summary>
        public DelegateCommand CloseCommand { get; set; }
        private Boolean CanCommandClose() { return InfrastructureModule.WorkOrder_OpenMode == Open_Mode.Read_Write; }

        private void CommandClose()
        {
            eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
            Application_Helper.DoEvents();

            try
            {
                logger.Trace("Entered");

                Remove_and_Clear_AutoSave();
                InfrastructureModule.WorkOrder_OpenMode = Open_Mode.Close;

                if ((Current_Work_Order != null) && (String.IsNullOrEmpty(Current_Work_Order.Work_Order_ID) == false))
                {
                    Close_Work_Order();
                    this.regionManager.RequestNavigate(RegionNames.MainRegion, "WorkOrderView");
                }
                else
                {
                    logger.Debug("No Work Order selected");
                }
                //    LoadCounter = 0;


                //var itemManager = new ToolBarItemManager(eventAggregator);
                //itemManager.HideToolBarItems(true);


                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //  TO DO:  Note this will need to be fixed when we have different user levels - all non-experts are getting set to basic every time!
                if ((LoginContext.User_Level != UserLevels.Expert) && (LoginContext.User_Access_ID > 0) && (String.IsNullOrEmpty(LoginContext.UserName) == false))
                {
                    try
                    {
                        logger.Debug("Resetting Non-Expert user to Basic");

                        MSSQL_Segway_Login_Web_Service_Client_SART2012.Initialize();
                        Service_User_Access sua = MSSQL_Segway_Login_Web_Service_Client.Select_Service_User_Access_Key(LoginContext.User_Access_ID);
                        if (sua != null)
                        {
                            sua.Level = UserLevels.Basic;
                            if (MSSQL_Segway_Login_Web_Service_Client.Update_Service_User_Access_Key(sua))
                            {
                                logger.Debug("Successfully reset Non-Expert user to Basic");
                            }
                            else
                            {
                                logger.Warn("Update user's access record failed");
                            }
                        }
                        else
                        {
                            logger.Warn("Did not find user's access record");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(Exception_Helper.FormatExceptionString(ex));
                        throw;
                    }
                    finally
                    {
                        logger.Trace("Leaving");
                    }
                }
                //  TO DO:
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
            }
            finally
            {
                logger.Trace("Leaving");
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                Application_Helper.DoEvents();
            }
        }

        #endregion

        #region CancelCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: CancelCommand</summary>
        public DelegateCommand CancelCommand { get; set; }
        private Boolean CanCommandCancel() { return true; }
        private void CommandCancel()
        {
            Work_Order_Events.Cancel_Current_Work_Order(eventAggregator, regionManager, Show_Warning_Message);
        }


        /////////////////////////////////////////////
        #endregion

        #region SaveCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: SaveCommand</summary>
        public DelegateCommand SaveCommand { get; set; }
        private Boolean CanCommandSave() { return InfrastructureModule.WorkOrder_OpenMode == Open_Mode.Read_Write; }
        private void CommandSave()
        {
            try
            {
                logger.Trace("Entered");
                InfrastructureModule.Current_Work_Order.Updated_By = LoginContext.UserName;
                InfrastructureModule.Current_Work_Order.Date_Time_Updated = DateTime.Now;
                Work_Order_Events.Update_Work_Order();
                InfrastructureModule.Original_Work_Order = InfrastructureModule.Current_Work_Order.Copy(true);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Work Order Saved");
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error: Work Order Not Saved");
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Pink);
            }
            finally
            {
                logger.Trace("Leaving");
            }
        }

        /////////////////////////////////////////////
        #endregion



        #region ConfigStartCommand

        /// <summary>Delegate Command: ConfigStartCommand</summary>
        public DelegateCommand ConfigStartCommand { get; set; }

        private Boolean CanCommandConfigStart()
        {
            if (Current_Work_Order == null) return false;
            if (InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write) return false;
            if (IsConfigStart.HasValue == true) /*&& (IsConfigStart.Value == true))*/ return false;
            return true;
        }

        private void CommandConfigStart()
        {
            logger.Info("Selected Configuration - Start");
            propertyChanges();
            Work_Order_Configuration_ViewModel vm = (Work_Order_Configuration_ViewModel)container.Resolve<Work_Order_Configuration_ViewModel_Interface>();
            if ((vm.PT_Config.Work_Order != Current_Work_Order.Work_Order_ID) || (vm.PT_Config.Serial_Number != Current_Work_Order.PT_Serial) || (vm.PT_Config.ConfigType != ConfigurationTypes.Service_Start))
            {
                if ((vm.StartConfig = Find_Configuration(Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Start)) == null)
                {
                    vm.StartConfig = SART_2012_Web_Service_Client.Select_SART_PT_Configuration_WORK_ORDER(Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Start);
                    if (vm.StartConfig == null)
                    {
                        vm.StartConfig = new SART_PT_Configuration();
                        vm.StartConfig.Work_Order = Current_Work_Order.Work_Order_ID;
                        vm.StartConfig.ConfigType = ConfigurationTypes.Service_Start;
                        vm.StartConfig.Serial_Number = Current_Work_Order.PT_Serial;

                        vm.StartConfig = SART_2012_Web_Service_Client.Insert_SART_PT_Configuration_Key(vm.StartConfig);
                    }
                }
                vm.PT_Config.Copy(vm.StartConfig, true);
            }
            eventAggregator.GetEvent<WorkOrder_ConfigurationType_Event>().Publish(ConfigurationTypes.Service_Start);
            regionManager.RequestNavigate(RegionNames.MainRegion, "Work_Order_Configuration_Control");
        }

        #endregion

        #region ConfigFinalCommand

        /// <summary>Delegate Command: ConfigFinalCommand</summary>
        public DelegateCommand ConfigFinalCommand { get; set; }

        private Boolean CanCommandConfigFinal()
        {
            if (Current_Work_Order == null) return false;
            if (InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write) return false;
            if (IsConfigStart.HasValue == true) return true;
            return false;
        }

        private void CommandConfigFinal()
        {
            logger.Info("Selected Configuration - Final");
            propertyChanges();
            Work_Order_Configuration_ViewModel vm = (Work_Order_Configuration_ViewModel)container.Resolve<Work_Order_Configuration_ViewModel_Interface>();
            if ((vm.PT_Config.Work_Order != Current_Work_Order.Work_Order_ID) || (vm.PT_Config.Serial_Number != Current_Work_Order.PT_Serial) || (vm.PT_Config.ConfigType != ConfigurationTypes.Service_Final))
            {
                if ((vm.FinalConfig = Find_Configuration(Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Final)) == null)
                {
                    vm.FinalConfig = SART_2012_Web_Service_Client.Select_SART_PT_Configuration_WORK_ORDER(Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Final);
                    if (vm.FinalConfig == null)
                    {
                        vm.FinalConfig = new SART_PT_Configuration();
                        vm.FinalConfig.Work_Order = Current_Work_Order.Work_Order_ID;
                        vm.FinalConfig.ConfigType = ConfigurationTypes.Service_Final;
                        vm.FinalConfig.Serial_Number = Current_Work_Order.PT_Serial;

                        vm.FinalConfig = SART_2012_Web_Service_Client.Insert_SART_PT_Configuration_Key(vm.FinalConfig);
                    }
                }
                vm.PT_Config.Copy(vm.FinalConfig, true);
            }

            eventAggregator.GetEvent<WorkOrder_ConfigurationType_Event>().Publish(ConfigurationTypes.Service_Final);
            regionManager.RequestNavigate(RegionNames.MainRegion, "Work_Order_Configuration_Control");
        }

        #endregion

        #region PictureOpenCommand

        /// <summary>Delegate Command: PictureOpenCommand</summary>
        public DelegateCommand PictureOpenCommand { get; set; }

        private Boolean CanCommandPictureOpen() { return Selected_Picture != null; }
        private void CommandPictureOpen()
        {
            eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
            eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
            Application_Helper.DoEvents();

            try
            {
                logger.Trace("Entered");
                foreach (Seg_SART_Pictures_Nodata pic in ((WorkOrderSummaryView)View).listPic.SelectedItems)
                {
                    FileInfo fi = Format_Cache_Filename(pic);
                    if (fi.Exists == false)
                    {
                        logger.Debug("Picture: {0} does not exist in cache", fi.FullName);
                        Seg_SART_Pictures picture = SART_2012_Web_Service_Client.Select_SART_Pictures_Key(pic.Row_Pointer);
                        if (picture != null)
                        {
                            logger.Debug("Writing picture to cache");
                            Write_Picture_To_Cache(fi, picture);
                            fi.Refresh();
                        }
                        //Seg_SART_Pictures_Nodata current = Selected_Picture;
                        //Picture_List = new ObservableCollection<Seg_SART_Pictures_Nodata>(Picture_List);
                        //Selected_Picture = current;
                    }

                    if (fi.Exists == true) ProcessHelper.Run(fi.FullName, null, 0, true, false, false, false);
                }
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Pink);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error: Unable to upload picture(s)");
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                Application_Helper.DoEvents();
                logger.Trace("Leaving");
            }
        }

        #endregion

        #region PictureAddCommand

        /// <summary>Delegate Command: PictureAddCommand</summary>
        public DelegateCommand PictureAddCommand { get; set; }

        private Boolean CanCommandPictureAdd() { return InfrastructureModule.WorkOrder_OpenMode == Open_Mode.Read_Write; }
        private void CommandPictureAdd()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (LoginContext.User_Level == UserLevels.Expert) ofd.Multiselect = true;
            else ofd.Multiselect = false;
            ofd.CheckFileExists = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                String picDesc = null;
                Entry_Window ew = null;
                while (String.IsNullOrEmpty(picDesc) == true)
                {
                    ew = new Entry_Window();
                    ew.Width = 500;

                    Boolean? result = ew.ShowDialog();
                    if (result == true)
                    {
                        if (String.IsNullOrEmpty(ew.Text) == false) break;
                    }
                    if (result == false)
                    {
                        return;
                    }
                }

                picDesc = ew.Text;
                if (picDesc.Length > 300) picDesc = picDesc.Substring(0, 300);

                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
                Application_Helper.DoEvents();

                try
                {
                    logger.Trace("Entered");
                    int picCount = 0;
                    foreach (String fname in ofd.FileNames)
                    {
                        FileInfo fi = new FileInfo(fname);
                        FileStream fs = fi.OpenRead();
                        Byte[] picdata = new Byte[fi.Length];
                        int read = fs.Read(picdata, 0, (int)fi.Length);
                        fs.Close();
                        if (read == (int)fi.Length)
                        {
                            Seg_SART_Pictures pic = new Seg_SART_Pictures();
                            pic.Description = picDesc;
                            pic.Name = fi.Name;
                            pic.Picture_Data = picdata;
                            pic.Unique_Name = GUID_Helper.Encode(Guid.NewGuid()) + Path.GetExtension(fi.Name);
                            pic.User_Name = LoginContext.UserName;
                            pic.Created_By = LoginContext.UserName;
                            pic.Updated_By = LoginContext.UserName;
                            pic.Create_Date = DateTime.Now;
                            pic.Record_Date = DateTime.Now;
                            pic.Work_Order_ID = Current_Work_Order.Work_Order_ID;
                            pic = SART_2012_Web_Service_Client.Insert_SART_Pictures_Key(pic);
                            if (pic != null)
                            {
                                Add_Picture_To_List(SART_2012_Web_Service_Client.Select_Seg_SART_Pictures_Nodata_Key(pic.Row_Pointer));
                                FileInfo cacheFI = Format_Cache_Filename(Selected_Picture);
                                Write_Picture_To_Cache(cacheFI, pic);

                                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Uploaded picture: {0} of {1}", ++picCount, ofd.FileNames.Length));
                                Application_Helper.DoEvents();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString(ex));
                    eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Pink);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error: Unable to upload picture(s)");
                }
                finally
                {
                    eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                    Application_Helper.DoEvents();
                    logger.Trace("Leaving");
                }

            }
        }

        #endregion

        #region PictureDelCommand

        /// <summary>Delegate Command: PictureDelCommand</summary>
        public DelegateCommand PictureDelCommand { get; set; }

        private Boolean CanCommandPictureDel()
        {
            if (InfrastructureModule.WorkOrder_OpenMode == Open_Mode.Read_Write) return false;
            return Selected_Picture != null;
        }

        private void CommandPictureDel()
        {
            if (Selected_Picture != null)
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
                Application_Helper.DoEvents();

                try
                {
                    logger.Trace("Entered");
                    List<Seg_SART_Pictures_Nodata> pics = new List<Seg_SART_Pictures_Nodata>();
                    foreach (Seg_SART_Pictures_Nodata pic in ((WorkOrderSummaryView)View).listPic.SelectedItems)
                    {
                        pics.Add(pic);
                    }
                    int picCount = 0;
                    foreach (Seg_SART_Pictures_Nodata pic in pics)
                    {
                        if (SART_2012_Web_Service_Client.Delete_SART_Pictures_Key(pic.Row_Pointer) == true)
                        {
                            FileInfo fi = Format_Cache_Filename(pic);
                            if (fi.Exists) fi.Delete();
                            Picture_List.Remove(pic);
                            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Deleted picture: {0} of {1}", ++picCount, pics.Count));
                            Application_Helper.DoEvents();
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString(ex));
                    eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Pink);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error: Unable to delete picture(s)");
                }
                finally
                {
                    eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                    logger.Trace("Leaving");
                }
            }
        }

        #endregion

        #region PictureEditCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PictureEditCommand</summary>
        public DelegateCommand PictureEditCommand { get; set; }
        private Boolean CanCommandPictureEdit()
        {
            if (InfrastructureModule.WorkOrder_OpenMode == Open_Mode.Read_Write) return false;
            return Selected_Picture != null;
        }

        private void CommandPictureEdit()
        {
            try
            {
                Entry_Window ew = null;
                while (true)
                {
                    ew = new Entry_Window();
                    ew.Width = 500;
                    ew.Text = Selected_Picture.Description;
                    Boolean? result = ew.ShowDialog();
                    if (result == true)
                    {
                        if (String.IsNullOrEmpty(ew.Text) == false) break;
                    }
                    if (result == false)
                    {
                        return;
                    }
                }

                Selected_Picture.Description = ew.Text;
                if (Selected_Picture.Description.Length > 300) Selected_Picture.Description = Selected_Picture.Description.Substring(0, 300);

                if (SART_2012_Web_Service_Client.Update_Seg_SART_Pictures_Nodata_Key(Selected_Picture) == false)
                {
                    eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Pink);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error: Unable to update picture description");
                }
                else
                {
                    eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Updated picture description");
                }

                Seg_SART_Pictures_Nodata store = Selected_Picture;
                Picture_List = new ObservableCollection<Seg_SART_Pictures_Nodata>(Picture_List);
                Selected_Picture = store;
            }
            catch (Exception e)
            {
                logger.Error(Exception_Helper.FormatExceptionString(e));
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Pink);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error: Unable to edit picture description");
            }
        }

        /////////////////////////////////////////////
        #endregion

        #region WO_CopyCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: WO_CopyCommand</summary>
        public DelegateCommand WO_CopyCommand { get; set; }
        private Boolean CanCommandWO_Copy() { return true; }
        private void CommandWO_Copy()
        {
            if (InfrastructureModule.Current_Work_Order != null)
            {
                if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Work_Order_ID) == false)
                {
                    System.Windows.Clipboard.SetText(InfrastructureModule.Current_Work_Order.Work_Order_ID);
                }
            }
        }

        /////////////////////////////////////////////
        #endregion

        #region SN_CopyCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: SN_CopyCommand</summary>
        public DelegateCommand SN_CopyCommand { get; set; }
        private Boolean CanCommandSN_Copy() { return true; }
        private void CommandSN_Copy()
        {
            if (InfrastructureModule.Current_Work_Order != null)
            {
                if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.PT_Serial) == false)
                {
                    System.Windows.Clipboard.SetText(InfrastructureModule.Current_Work_Order.PT_Serial);
                }
            }
        }

        /////////////////////////////////////////////
        #endregion


        #endregion


        #region Support Methods


        private Boolean All_Conditions_Met()
        {
            if (Current_Work_Order.Is_CUA_Extracted == null) return false;
            if (Current_Work_Order.Is_CUA_Extracted.Value == false) return false;

            if (Current_Work_Order.Is_CUB_Extracted == null) return false;
            if (Current_Work_Order.Is_CUB_Extracted.Value == false) return false;

            if (Current_Work_Order.Is_CUA_Loaded == null) return false;
            if (Current_Work_Order.Is_CUA_Loaded.Value == false) return false;

            if (Current_Work_Order.Is_CUB_Loaded == null) return false;
            if (Current_Work_Order.Is_CUB_Loaded.Value == false) return false;

            if (Current_Work_Order.Is_LED_Test == null) return false;
            if (Current_Work_Order.Is_LED_Test.Value == false) return false;

            if (Current_Work_Order.Is_BSA_Code_Loaded == null) return false;
            if (Current_Work_Order.Is_BSA_Code_Loaded.Value == false) return false;

            if (Current_Work_Order.Is_NormalMotor_Test == null) return false;
            if (Current_Work_Order.Is_NormalMotor_Test.Value == false) return false;

            if (Current_Work_Order.Is_BSA_Test == null) return false;
            if (Current_Work_Order.Is_BSA_Test.Value == false) return false;

            if (Current_Work_Order.Is_RiderDetect_Test == null) return false;
            if (Current_Work_Order.Is_RiderDetect_Test.Value == false) return false;

            return true;
        }

        #endregion
    }
}

