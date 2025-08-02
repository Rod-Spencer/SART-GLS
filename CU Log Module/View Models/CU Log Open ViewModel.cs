using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Database.Objects;
using Segway.Login.Objects;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder;
using Segway.SART.CULog.Objects;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace Segway.Modules.CU_Log_Module
{
    public class CU_Log_Open_ViewModel : ViewModelBase, CU_Log_Open_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator aggregator;

        public CU_Log_Open_ViewModel(CU_Log_Open_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            aggregator = eventAggregator;

            #region Event Subscriptions

            aggregator.GetEvent<ApplyLogFilterEvent>().Subscribe(ApplyLogFilterEventHandler, ThreadOption.BackgroundThread, true);
            aggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, ThreadOption.UIThread, true);
            aggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, ThreadOption.UIThread, true);
            aggregator.GetEvent<WorkOrder_Opened_Event>().Subscribe(Work_Order_Opened, ThreadOption.BackgroundThread, true);
            aggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login, ThreadOption.BackgroundThread, true);
            aggregator.GetEvent<SART_Dealers_Loaded_Event>().Subscribe(Updated_DealerList, ThreadOption.BackgroundThread, true);
            aggregator.GetEvent<WorkOrder_Read_CU_Log_Event>().Subscribe(Log_Extracted, ThreadOption.BackgroundThread, true);
            aggregator.GetEvent<SART_UserSettings_Changed_Event>().Subscribe(SART_UserSettings_Changed_Handler, ThreadOption.UIThread, true);

            #endregion

            #region Command Setups

            CULogFilterCommand = new DelegateCommand(CommandCULogFilter, CanCommandCULogFilter);
            OpenLog_Command = new DelegateCommand(Command_OpenLog, CanCommand_OpenLog);
            CULogClearCommand = new DelegateCommand(CommandCULogClear, CanCommandCULogClear);
            LocalSaveCommand = new DelegateCommand(CommandLocalSave, CanCommandLocalSave);

            #endregion

            //      OnSelected();
        }



        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        //#region LoginContext

        //private Login_Context _LoginContext = null;

        ///// <summary>Property LoginContext of type Login_Context</summary>
        //public Login_Context LoginContext
        //{
        //    get
        //    {
        //        if (container.IsRegistered<Login_Context_Interface>(Login_Context.Name) == true)
        //        {
        //            _LoginContext = (Login_Context)container.Resolve<Login_Context_Interface>(Login_Context.Name);
        //        }
        //        return _LoginContext;
        //    }
        //}

        //#endregion


        #region Token

        private AuthenticationToken _Token;

        /// <summary>Property Token of type AuthenticationToken</summary>
        public AuthenticationToken Token
        {
            get
            {
                if (_Token == null)
                {
                    if (container.IsRegistered<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName) == true)
                    {
                        _Token = container.Resolve<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName);
                    }
                }
                return _Token;
            }
        }

        #endregion

        #region LoginContext

        private Login_Context _LoginContext = null;
        /// <summary>Property LoginContext of type Login_Context</summary>
        public Login_Context LoginContext
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

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        #region DealerInfo

        private Dealer_Info _DealerInfo;

        /// <summary>Property DealerInfo of type Dealer_Info</summary>
        public Dealer_Info DealerInfo
        {
            get
            {
                if (_DealerInfo == null) _DealerInfo = container.Resolve<Dealer_Info>(Dealer_Info.Name);
                return _DealerInfo;
            }
            set { _DealerInfo = value; }
        }

        #endregion

        #region Settings Properties

        #region PTSerialNumber

        private String _PTSerialNumber;

        /// <summary>Property PTSerialNumber of type String</summary>
        public String PTSerialNumber
        {
            get { return _PTSerialNumber; }
            set
            {
                _PTSerialNumber = value;
                OnPropertyChanged("PTSerialNumber");
            }
        }

        #endregion

        #region WorkOrderNumber

        private String _WorkOrderNumber;

        /// <summary>Property WorkOrderNumber of type String</summary>
        public String WorkOrderNumber
        {
            get { return _WorkOrderNumber; }
            set
            {
                _WorkOrderNumber = SART_Common.Format_Work_Order_ID(value);
                OnPropertyChanged("WorkOrderNumber");
            }
        }

        #endregion

        #region UserName

        private String _UserName;

        /// <summary>Property UserName of type String</summary>
        public String UserName
        {
            get { return _UserName; }
            set
            {
                _UserName = value;
                OnPropertyChanged("UserName");
            }
        }

        #endregion

        #region StartDate

        private DateTime? _StartDate;

        /// <summary>Property StartDate of type DateTime?</summary>
        public DateTime? StartDate
        {
            get { return _StartDate; }
            set
            {
                _StartDate = value;
                OnPropertyChanged("StartDate");
            }
        }

        #endregion

        #region EndDate

        private DateTime? _EndDate;

        /// <summary>Property EndDate of type DateTime?</summary>
        public DateTime? EndDate
        {
            get { return _EndDate; }
            set
            {
                _EndDate = value;
                OnPropertyChanged("EndDate");
            }
        }

        #endregion

        #region DealerList

        private List<String> _DealerList;

        /// <summary>Property DealerList of type List<String></summary>
        public List<String> DealerList
        {
            get
            {
                if (_DealerList == null)
                {
                    _DealerList = DealerInfo.Dealer_List;
                }
                return _DealerList;
            }
            set
            {
                _DealerList = value;
                OnPropertyChanged("DealerList");
            }
        }

        #endregion

        #region Group_Name

        private String _Group_Name;

        /// <summary>Property Group_Name of type String</summary>
        public String Group_Name
        {
            get { return _Group_Name; }
            set
            {
                _Group_Name = value;
                OnPropertyChanged("Group_Name");
            }
        }

        #endregion

        #region Orgainization_Visibility

        //        private Visibility _Orgainization_Visibility = Visibility.Visible;

        /// <summary>Property Orgainization_Visibility of type Visibility</summary>
        public Visibility Orgainization_Visibility
        {
            get
            {
                var user = (Login_Context)container.Resolve<Login_Context_Interface>(Login_Context.Name); //container.Resolve<Login_Context>();
                if (user.User_Level >= UserLevels.Expert) return Visibility.Visible;
                return Visibility.Collapsed;
            }
            set
            {
                //_Orgainization_Visibility = value;
                OnPropertyChanged("Orgainization_Visibility");
            }
        }

        #endregion

        #endregion

        #region Open Log Properties

        #region CU_Log_List

        private ObservableCollection<SART_CU_Logs> _CU_Log_List;

        /// <summary>Property CU_Log_List of type List<SART_CU_Logs></summary>
        public ObservableCollection<SART_CU_Logs> CU_Log_List
        {
            get { return _CU_Log_List; }
            set
            {
                _CU_Log_List = value;
                OnPropertyChanged("CU_Log_List");
                OnPropertyChanged("Log_Count");
            }
        }

        #endregion

        #region Selected_CU_Log

        private SART_CU_Logs _Selected_CU_Log = null;

        /// <summary>Property Selected_CU_Log of type SART_CU_Logs</summary>
        public SART_CU_Logs Selected_CU_Log
        {
            get { return _Selected_CU_Log; }
            set
            {
                _Selected_CU_Log = value;
                //if (Selected_CU_Log == null) logger.Debug("Cleared selected CU Log");
                //else logger.Debug("Selected: {0}", Selected_CU_Log);
                OnPropertyChanged("Selected_CU_Log");
                OpenLog_Command.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Header_Image

        private BitmapImage _Header_Image;

        /// <summary>Property Header_Image of type BitmapImage</summary>
        public BitmapImage Header_Image
        {
            get
            {
                if (_Header_Image == null)
                {
                    _Header_Image = Image_Helper.ImageFromEmbedded(".Images.open.png");
                }
                return _Header_Image;
            }
            set
            {
                OnPropertyChanged("Header_Image");
            }
        }

        #endregion

        #endregion

        #region IsExpanded

        private Boolean _IsExpanded = false;

        /// <summary>Property IsExpanded of type Boolean</summary>
        public Boolean IsExpanded
        {
            get { return _IsExpanded; }
            set
            {
                _IsExpanded = value;
                OnPropertyChanged("IsExpanded");
                OnPropertyChanged("Orgainization_Visibility");
            }
        }

        #endregion

        #region IsDecodeExpanded

        private Boolean _IsDecodeExpanded;

        /// <summary>Property IsDecodeExpanded of type Boolean</summary>
        public Boolean IsDecodeExpanded
        {
            get { return _IsDecodeExpanded; }
            set
            {
                _IsDecodeExpanded = value;
                OnPropertyChanged("IsDecodeExpanded");
            }
        }

        #endregion

        #region IsDecodeVisibility

        /// <summary>Property IsDecodeVisibility of type Visibility</summary>
        public Visibility IsDecodeVisibility
        {
            get
            {
                if (InfrastructureModule.User_Settings != null)
                {
                    DecodeBasic = InfrastructureModule.User_Settings.Decode_Log_Type == G2_Report_Types.Basic;
                    DecodeAdvanced = InfrastructureModule.User_Settings.Decode_Log_Type == G2_Report_Types.Advanced;
                    ShowRawData = InfrastructureModule.User_Settings.Decode_Show_Raw_Data;
                    IsReverseOrder = InfrastructureModule.User_Settings.Decode_Show_Reverse_Order;

                    if (LoginContext != null)
                    {
                        if (LoginContext.User_Level == UserLevels.Master)
                        {
                            return Visibility.Visible;
                        }
                    }
                }
                return Visibility.Collapsed;
            }
            set
            {
                OnPropertyChanged("IsDecodeVisibility");
            }
        }

        #endregion

        #region DecodeBasic

        private Boolean _DecodeBasic = true;

        /// <summary>Property DecodeBasic of type Boolean</summary>
        public Boolean DecodeBasic
        {
            get { return _DecodeBasic; }
            set
            {
                _DecodeBasic = value;
                OnPropertyChanged("DecodeBasic");
            }
        }

        #endregion

        #region DecodeAdvanced

        private Boolean _DecodeAdvanced = false;

        /// <summary>Property DecodeAdvanced of type Boolean</summary>
        public Boolean DecodeAdvanced
        {
            get { return _DecodeAdvanced; }
            set
            {
                _DecodeAdvanced = value;
                OnPropertyChanged("DecodeAdvanced");
                OnPropertyChanged("ShowRawData_Visibility");
            }
        }

        #endregion

        #region IsReverseOrder

        private Boolean _IsReverseOrder = true;

        /// <summary>Property IsReverseOrder of type Boolean</summary>
        public Boolean IsReverseOrder
        {
            get { return _IsReverseOrder; }
            set
            {
                _IsReverseOrder = value;
                OnPropertyChanged("IsReverseOrder");
            }
        }

        #endregion

        #region Log_Count

        /// <summary>Property Log_Count of type int</summary>
        public int Log_Count
        {
            get
            {
                if (_CU_Log_List == null) return 0;
                return _CU_Log_List.Count;
            }
            set
            {
                OnPropertyChanged("Log_Count");
            }
        }

        #endregion

        #region ShowRawData_Visibility

        /// <summary>Property ShowRawData_Visibility of type Visibility</summary>
        public Visibility ShowRawData_Visibility
        {
            get
            {
                if (LoginContext == null) return Visibility.Collapsed;
                if (LoginContext.User_Level < UserLevels.Expert) return Visibility.Collapsed;
                if (DecodeAdvanced == true) return Visibility.Visible;
                return Visibility.Collapsed;
            }
            set
            {
                OnPropertyChanged("ShowRawData_Visibility");
            }
        }

        #endregion

        #region ShowRawData

        private Boolean _ShowRawData;

        /// <summary>Property ShowRawData of type Boolean</summary>
        public Boolean ShowRawData
        {
            get { return _ShowRawData; }
            set
            {
                _ShowRawData = value;
                OnPropertyChanged("ShowRawData");
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region OpenLog_Command

        /// <summary>Delegate Command: OpenLog_Command</summary>
        public DelegateCommand OpenLog_Command { get; set; }

        private Boolean CanCommand_OpenLog() { return Selected_CU_Log != null; }
        private void Command_OpenLog()
        {
            if ((Selected_CU_Log != null) && (Selected_CU_Log.ID > 0))
            {
                G2_Report_Types type = G2_Report_Types.Basic;
                if (DecodeBasic == true) type = G2_Report_Types.Basic;
                else if (DecodeAdvanced == true) type = G2_Report_Types.Advanced;
                Common.Display_CU_Log(Selected_CU_Log, type, IsReverseOrder, type == G2_Report_Types.Advanced ? ShowRawData : false);
            }
        }

        #endregion

        #region CULogClearCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: CULogClearCommand</summary>
        public DelegateCommand CULogClearCommand { get; set; }
        private Boolean CanCommandCULogClear() { return true; }
        private void CommandCULogClear()
        {
            PTSerialNumber = "";
            WorkOrderNumber = "";
            UserName = "";
            StartDate = null;
            EndDate = null;
            Group_Name = null;
        }

        /////////////////////////////////////////////
        #endregion

        #region CULogFilterCommand

        /// <summary>Delegate Command: CULogFilterCommand</summary>
        public DelegateCommand CULogFilterCommand { get; set; }

        private Boolean CanCommandCULogFilter() { return true; }
        private void CommandCULogFilter()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SqlBooleanCriteria criteria = Select_Criteria();
                Thread back = new Thread(new ParameterizedThreadStart(GetFilteredLogs));
                back.IsBackground = true;
                back.Start(criteria);
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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

        #endregion

        #region LocalSaveCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: LocalSaveCommand</summary>
        public DelegateCommand LocalSaveCommand { get; set; }
        private Boolean CanCommandLocalSave() { return true; }
        private void CommandLocalSave()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (((CU_Log_Open_Control)View).CULog_List.SelectedItems != null)
                {
                    FolderBrowserDialog fbd = new FolderBrowserDialog();
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        DirectoryInfo folder = new DirectoryInfo(fbd.SelectedPath);
                        G2_Report_Types repType = (DecodeBasic == true) ? G2_Report_Types.Basic : G2_Report_Types.Advanced;

                        foreach (SART_CU_Logs item in ((CU_Log_Open_Control)View).CULog_List.SelectedItems)
                        {
                            Common.Write_CU_Log(item, repType, IsReverseOrder, ShowRawData, folder);
                        }
                    }
                }
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

        /////////////////////////////////////////////
        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers


        /////////////////////////////////////////////
        #region Log_Extracted  -- WorkOrder_Read_CU_Log_Event Event Handler
        /////////////////////////////////////////////

        private void Log_Extracted(WorkOrder_Events evnt)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SART_CU_Logs selected = Selected_CU_Log;
                GetFilteredLogs(Select_Criteria());
                foreach (SART_CU_Logs log in CU_Log_List)
                {
                    if (selected == null)
                    {
                        //Selected_CU_Log = log; -- changed to following because list is now in reverse order by default

                        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate ()
                        {
                            Selected_CU_Log = CU_Log_List[0];
                        });
                        break;
                    }
                    else if ((selected == log) || (selected.ID == log.ID))
                    {
                        Selected_CU_Log = log;
                        break;
                    }
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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

        /////////////////////////////////////////////
        #endregion
        /////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Close_Handler  -- Event: SART_WorkOrder_Close_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Close_Handler(Boolean closed)
        {
            Reset();
            SetUserDefault();
            CommandCULogFilter();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Cancel_Handler  -- Event: SART_WorkOrder_Cancel_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Cancel_Handler(Boolean canceled)
        {
            SART_WorkOrder_Close_Handler(canceled);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void ApplyLogFilterEventHandler(SqlBooleanCriteria criteria)
        {
            GetFilteredLogs(criteria);

            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate ()
            {
                Selected_CU_Log = CU_Log_List[CU_Log_List.Count - 1];
            });
        }


        private void Work_Order_Opened(Boolean wo)
        {
            Reset();
            SetSerialDefault();
            CommandCULogFilter();
        }

        private void Application_Login(String username)
        {
            SART_WorkOrder_Close_Handler(true);
            OnPropertyChanged("IsDecodeVisibility");
        }

        private void Updated_DealerList(Boolean clear)
        {
            DealerList = null;
        }



        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_UserSettings_Changed_Handler  -- Event: SART_UserSettings_Changed_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_UserSettings_Changed_Handler(SART_User_Settings obj)
        {
            if (obj !=null)
            {
                DecodeAdvanced = obj.Decode_Log_Type == G2_Report_Types.Advanced;
                DecodeBasic = obj.Decode_Log_Type == G2_Report_Types.Basic;
                IsReverseOrder = obj.Decode_Show_Reverse_Order;
                ShowRawData = obj.Decode_Show_Raw_Data;
                OnPropertyChanged("ShowRawData_Visibility");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            OnPropertyChanged("ShowRawData_Visibility");
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private SqlBooleanCriteria Select_Criteria()
        {
            SqlBooleanCriteria criteria = new SqlBooleanCriteria();
            if (String.IsNullOrEmpty(PTSerialNumber) == false) criteria.Add(new FieldData("PT_Serial", PTSerialNumber, FieldCompareOperator.Contains));
            if (String.IsNullOrEmpty(WorkOrderNumber) == false) criteria.Add(new FieldData("Work_Order", WorkOrderNumber, FieldCompareOperator.Contains));
            if (String.IsNullOrEmpty(UserName) == false) criteria.Add(new FieldData("User_Name", UserName, SegwayFieldTypes.StringInsensitive, FieldCompareOperator.Contains));
            if ((StartDate != null) && StartDate.HasValue) criteria.Add(new FieldData("Date_Time_Extracted", StartDate.Value.Date, SegwayFieldTypes.Date, FieldCompareOperator.GreaterThanOrEqual));
            if ((EndDate != null) && EndDate.HasValue) criteria.Add(new FieldData("Date_Time_Extracted", EndDate.Value.AddDays(1).Date, SegwayFieldTypes.Date, FieldCompareOperator.LessThan));
            if ((LoginContext != null) &&
                (LoginContext.User_Level < UserLevels.Expert) &&
                (LoginContext.User_Level != Service.Objects.UserLevels.NotDefined) &&
                (LoginContext.Customer_ID != null)
                )
                criteria.Add(new FieldData("Customer_ID", LoginContext.Customer_ID.Trim()));
            else if ((Orgainization_Visibility == Visibility.Visible) && (String.IsNullOrEmpty(Group_Name) == false))
            {
                if (DealerInfo.Accounts.ContainsKey(Group_Name) == true)
                {
                    criteria.Add(new FieldData("Customer_ID", DealerInfo.Accounts[Group_Name]));
                }
            }
            return criteria;
        }

        public void SetUserDefault()
        {
            UserName = LoginContext.UserName;
            StartDate = DateTime.Today.AddDays(-30);
        }

        public void SetSerialDefault()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                PTSerialNumber = InfrastructureModule.Current_Work_Order.PT_Serial;
                StartDate = DateTime.Today.AddDays(-30);
                if (LoginContext.User_Level >= UserLevels.Expert)
                {
                    Group_Name = "";
                }
                else if (DealerInfo.Dealers.ContainsKey(LoginContext.Customer_ID))
                {
                    Group_Name = DealerInfo.Dealers[LoginContext.Customer_ID];
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void GetFilteredLogs(Object obj)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                SqlBooleanCriteria criteria = (SqlBooleanCriteria)obj;

                //CU_Log_List = null;
                logger.Debug("Retrieving log records on criteria: {0}", criteria);
                var logList = SART_Log_Web_Service_Client_REST.Select_SART_CU_Logs_Criteria(InfrastructureModule.Token, criteria);
                if (logList == null)
                {
                    logList = new List<SART_CU_Logs>();
                }

                logger.Debug("Retrieved {0} log records", logList.Count);
                logList.Reverse();


                try
                {
                    CU_Log_List = new ObservableCollection<SART_CU_Logs>(logList);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        CU_Log_List = new ObservableCollection<SART_CU_Logs>(logList);
                    });
                }

                IsExpanded = false;
                IsDecodeExpanded = false;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
            finally
            {
                aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private void Reset()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                DealerInfo = null;
                PTSerialNumber = null;
                WorkOrderNumber = null;
                UserName = null;
                StartDate = null;
                EndDate = null;
                DealerList = null;
                Group_Name = null;
                CU_Log_List = null;
                Selected_CU_Log = null;
                IsExpanded = false;
                IsDecodeExpanded = false;
                DecodeBasic = true;
                DecodeAdvanced = false;
                IsReverseOrder = true;
                ShowRawData = false;

                OnPropertyChanged("Orgainization_Visibility");
                OnPropertyChanged("IsDecodeVisibility");
                OnPropertyChanged("Log_Count");
                OnPropertyChanged("ShowRawData_Visibility");
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
