using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Database.Objects;
using Segway.Login.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder.Services;
using Segway.SART.Objects;
using Segway.Service.AppSettings.Helper;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.Controls.StatusBars;
using Segway.Service.Disclaimer;
using Segway.Service.ExceptionHelper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.Modules.AddWindow;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using Segway.Syteline.Client.REST;
using Segway.Syteline.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Segway.Modules.WorkOrder
{
    public class Work_Order_Open_ViewModel : ViewModelBase, Work_Order_Open_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;

        DispatcherTimer autoSave = null;

        public Work_Order_Open_ViewModel(Work_Order_Open_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<SART_Disclaimer_Reject_Event>().Subscribe(SART_Disclaimer_Reject_Handler, true);
            //eventAggregator.GetEvent<WorkOrder_AutoSave_Delete_Event>().Subscribe(WorkOrder_AutoSave_Delete_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<Application_Logout_Event>().Subscribe(Application_Logout_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<WorkOrder_Clear_List_Event>().Subscribe(WorkOrder_Clear_List_Handler, true);
            eventAggregator.GetEvent<WorkOrder_AutoSave_Event>().Subscribe(WorkOrder_AutoSave_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, true);

            #endregion

            #region Command Setups

            OpenWO_Command = new DelegateCommand(CommandOpenWO, CanCommandOpenWO);
            ViewWOCommand = new DelegateCommand(CommandViewWO, CanCommandViewWO);
            FilterCommand = new DelegateCommand(CommandFilter, CanCommandFilter);
            SettingsClearCommand = new DelegateCommand(CommandSettingsClear, CanCommandSettingsClear);
            CopyWorkOrderCommand = new DelegateCommand(CommandCopyWorkOrder, CanCommandCopyWorkOrder);
            CopyPTSerialCommand = new DelegateCommand(CommandCopyPTSerial, CanCommandCopyPTSerial);
            RepairCompleteCommand = new DelegateCommand(CommandRepairComplete, CanCommandRepairComplete);
            ChangeStatusCommand = new DelegateCommand(CommandChangeStatus, CanCommandChangeStatus);

            #endregion
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        #region Flight_Code_Model

        private Dictionary<String, String> _Flight_Code_Model;

        /// <summary>Property Flight_Code_Model of type Dictionary<String,String></summary>
        public Dictionary<String, String> Flight_Code_Model
        {
            get
            {
                if ((_Flight_Code_Model == null) || (_Flight_Code_Model.Count == 0))
                {
                    try
                    {
                        _Flight_Code_Model = Syteline_Items_Web_Service_Client_REST.Get_Gen2_Flight_Code_Models(InfrastructureModule.Token);
                        if (_Flight_Code_Model == null) _Flight_Code_Model = new Dictionary<String, String>();
                    }
                    catch (Authentication_Exception ae)
                    {
                        logger.Warn(Exception_Helper.FormatExceptionString(ae));
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(Exception_Helper.FormatExceptionString(ex));
                        _Flight_Code_Model = new Dictionary<String, String>(); ;
                    }
                }
                return _Flight_Code_Model;
            }
            set
            {
                _Flight_Code_Model = value;
                OnPropertyChanged("Flight_Code_Model");
            }
        }

        #endregion


        #region InfoKey_Codes

        private Dictionary<String, InfoKey_Error_Codes> _InfoKey_Codes;

        /// <summary>Property InfoKey_Codes of type Dictionary<String, InfoKey_Error_Codes></summary>
        public Dictionary<String, InfoKey_Error_Codes> InfoKey_Codes
        {
            get
            {
                if (_InfoKey_Codes == null)
                {
                    if (InfrastructureModule.Token != null)
                    {
                        try
                        {
                            List<InfoKey_Error_Codes> codes = SART_IKErrCod_Web_Service_Client_REST.Select_InfoKey_Error_Codes_All(InfrastructureModule.Token);
                            if (codes != null)
                            {
                                codes.Sort(new InfoKey_Error_Codes_Comparer());
                                _InfoKey_Codes = new Dictionary<String, InfoKey_Error_Codes>();
                                foreach (InfoKey_Error_Codes code in codes)
                                {
                                    _InfoKey_Codes.Add(code.Code, code);
                                }
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
                            eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(Exception_Helper.FormatExceptionString(ex));
                            throw;
                        }
                    }
                }
                return _InfoKey_Codes;
            }
            set { _InfoKey_Codes = value; }
        }

        #endregion


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


        #region Status_Codes

        private Dictionary<String, String> _Status_Codes;

        /// <summary>Property Status_Codes of type Dictionary<String,String></summary>
        public Dictionary<String, String> Status_Codes
        {
            get
            {
                if (_Status_Codes == null)
                {
                    if (InfrastructureModule.Token != null)
                    {
                        _Status_Codes = Common.Get_Statuses(InfrastructureModule.Token);
                    }
                }
                return _Status_Codes;
            }
            set { _Status_Codes = value; }
        }

        #endregion

        public String Last_Selected_Work_Order_ID { get; set; }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties


        #region IsExpanded

        private Boolean _IsExpanded = true;

        /// <summary>Property IsExpanded of type Boolean</summary>
        public Boolean IsExpanded
        {
            get { return _IsExpanded; }
            set
            {
                _IsExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }

        #endregion


        #region Selected_Work_Order

        private SART_Work_Order_View _Selected_Work_Order;

        /// <summary>Property Selected_Work_Order of type SART_Work_Order</summary>
        public SART_Work_Order_View Selected_Work_Order
        {
            get
            {
                //SART_Work_Order_View obj = null;
                //Application.Current.Dispatcher.Invoke((Action)delegate ()
                //{
                //    obj = _Selected_Work_Order;
                //});
                //return obj;
                return _Selected_Work_Order;
            }
            set
            {
                _Selected_Work_Order = value;
                if (_Selected_Work_Order != null)
                {
                    Last_Selected_Work_Order_ID = _Selected_Work_Order.Work_Order_ID;

                    {
                        ContextMenu cmenu = new ContextMenu();
                        Add_Copy_WorkOrder(cmenu);
                        Add_Copy_PTSerial(cmenu);
                        if ((String.IsNullOrEmpty(Selected_Work_Order.Opened_By) == true) &&
                            ((Selected_Work_Order.Status != "Repair Complete") && (Selected_Work_Order.Status != "Closed")))
                        {
                            Add_Repair_Complete(cmenu);
                        }
                        if (InfrastructureModule.Token.LoginContext.User_Level >= UserLevels.Expert)
                        {
                            if ((Selected_Work_Order.State == "O") && (String.IsNullOrEmpty(Selected_Work_Order.Opened_By) == true))
                            {
                                if (InfrastructureModule.Token.LoginContext.User_Level >= UserLevels.Advanced)
                                {
                                    Add_Close_WorkOrder(cmenu);
                                }
                            }
                            else if ((Selected_Work_Order.State == "C") && (Selected_Work_Order.Status == "Closed") && (String.IsNullOrEmpty(Selected_Work_Order.Opened_By) == true))
                            {
                                if (InfrastructureModule.Token.LoginContext.User_Level >= UserLevels.Advanced)
                                {
                                    Add_ReOpen_WorkOrder(cmenu);
                                }
                            }
                            if ((String.IsNullOrEmpty(Selected_Work_Order.Opened_By) == true) && (Selected_Work_Order.Status != "Closed"))
                            {
                                Add_Change_Status(cmenu);
                            }
                        }
                    ((Work_Order_Open_Control)View).WOList.ContextMenu = cmenu;
                    }
                }

                OnPropertyChanged("Selected_Work_Order");
                OpenWO_Command.RaiseCanExecuteChanged();
                ViewWOCommand.RaiseCanExecuteChanged();
                CopyPTSerialCommand.RaiseCanExecuteChanged();
                CopyWorkOrderCommand.RaiseCanExecuteChanged();
                eventAggregator.GetEvent<SART_WorkOrder_Selected_Event>().Publish(_Selected_Work_Order == null ? null : _Selected_Work_Order.Work_Order_ID);
                //});
            }
        }

        #endregion


        #region WorkOrderList

        private List<SART_Work_Order_View> _WorkOrderList;

        /// <summary>Property WorkOrderList of type List<SART_Work_Order></summary>
        public List<SART_Work_Order_View> WorkOrderList
        {
            get { return _WorkOrderList; }
            set
            {
                _WorkOrderList = value;
                Work_Order_Count = _WorkOrderList == null ? "0" : _WorkOrderList.Count.ToString();
                OnPropertyChanged("WorkOrderList");
            }
        }

        #endregion


        #region Work_Order_Count

        private String _Work_Order_Count;

        /// <summary>Property Work_Order_Count of type String</summary>
        public String Work_Order_Count
        {
            get { return _Work_Order_Count; }
            set
            {
                _Work_Order_Count = value;
                OnPropertyChanged("Work_Order_Count");
            }
        }

        #endregion


        ////////////////////////////////////////
        #region Filter Properties

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
                FilterCommand.RaiseCanExecuteChanged();
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
                FilterCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region StatusStrings

        private ObservableCollection<String> _StatusStrings;

        /// <summary>Property StatusStrings of type List String </summary>
        public ObservableCollection<String> StatusStrings
        {
            get
            {
                if (_StatusStrings == null)
                {
                    if (Status_Codes != null)
                    {
                        List<String> codes = new List<String>(Status_Codes.Values);
                        codes.Sort();
                        _StatusStrings = new ObservableCollection<String>(codes);  // MapStatusEnumerations();
                        _StatusStrings.Insert(0, "");
                    }
                }
                return _StatusStrings;
            }
            set
            {
                _StatusStrings = value;
                OnPropertyChanged("StatusStrings");
            }
        }

        #endregion

        #region SelectedStatusString

        private String _SelectedStatusString;

        /// <summary>Property SelectedStatusString of type String, used in GUI</summary>
        public String SelectedStatusString
        {
            get { return _SelectedStatusString; }
            set
            {
                _SelectedStatusString = value;
                OnPropertyChanged("SelectedStatusString");
                FilterCommand.RaiseCanExecuteChanged();
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
                FilterCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region StartDate

        private DateTime? _StartDate;

        /// <summary>Property StartDate of type DateTime</summary>
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

        /// <summary>Property EndDate of type DateTime</summary>
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
                if (_DealerList == null) _DealerList = DealerInfo.Dealer_List;
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

        #region GroupVisible

        private bool _GroupVisible;

        /// <summary>Property GroupVisible of type bool</summary>
        public bool GroupVisible
        {
            get { return _GroupVisible; }
            set
            {
                _GroupVisible = value;
                OnPropertyChanged("GroupVisible");
            }
        }

        #endregion

        #region GroupOpacity

        private Double _GroupOpacity;

        /// <summary>Property GroupOpacity of type String</summary>
        public Double GroupOpacity
        {
            get { return _GroupOpacity; }
            set
            {
                _GroupOpacity = value;
                OnPropertyChanged("GroupOpacity");
            }
        }

        #endregion

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

        #endregion
        ////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region OpenWO_Command

        /// <summary>Delegate Command: OpenWO_CommandCommand</summary>
        public DelegateCommand OpenWO_Command { get; set; }

        private Boolean CanCommandOpenWO()
        {
            if (Selected_Work_Order == null) return false;
            if (Selected_Work_Order.State == "C") return false;
            if (Selected_Work_Order.Status == "Closed") return false;
            if (Selected_Work_Order.Status == "Repair Complete") return false;
            return true;
        }

        private void CommandOpenWO()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Thread back = new Thread(new ThreadStart(CommandOpenWO_Back));
                back.IsBackground = true;
                back.Start();
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


        #region ViewWOCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ViewWOCommand</summary>
        public DelegateCommand ViewWOCommand { get; set; }
        private Boolean CanCommandViewWO() { return Selected_Work_Order != null; }
        private void CommandViewWO()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Open_Work_Order(Open_Mode.Read_Only);
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

        /////////////////////////////////////////////
        #endregion


        #region FilterCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: FilterCommand</summary>
        public DelegateCommand FilterCommand { get; set; }
        private Boolean CanCommandFilter()
        {
            if (String.IsNullOrEmpty(PTSerialNumber) == false) return true;
            if (String.IsNullOrEmpty(WorkOrderNumber) == false) return true;
            if (String.IsNullOrEmpty(UserName) == false) return true;
            if (SelectedStatusString != null) return true;
            return false;
        }

        private void CommandFilter()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (String.IsNullOrEmpty(WorkOrderNumber) == false)
                {
                    Last_10.Update_Most_Recent(Last_10.Last_SRO_FileName, WorkOrderNumber);
                }
                Requested_Filter(Create_Criteria());
                IsExpanded = false;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Message_Window.Error(msg).ShowDialog();
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region SettingsClearCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: SettingsClearCommand</summary>
        public DelegateCommand SettingsClearCommand { get; set; }
        private Boolean CanCommandSettingsClear() { return true; }
        private void CommandSettingsClear()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                PTSerialNumber = null;
                WorkOrderNumber = null;
                SelectedStatusString = null;
                UserName = null;
                StartDate = null;
                EndDate = null;
                Group_Name = null;
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

        /////////////////////////////////////////////
        #endregion


        #region CopyWorkOrderCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: CopyWorkOrderCommand</summary>
        public DelegateCommand CopyWorkOrderCommand { get; set; }
        private Boolean CanCommandCopyWorkOrder()
        {
            return Selected_Work_Order != null;
        }

        private void CommandCopyWorkOrder()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Clipboard.SetText(Selected_Work_Order.Work_Order_ID);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Message_Window.Error(msg).ShowDialog();
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region CopyPTSerialCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: CopyPTSerialCommand</summary>
        public DelegateCommand CopyPTSerialCommand { get; set; }
        private Boolean CanCommandCopyPTSerial()
        {
            return Selected_Work_Order != null;
        }

        private void CommandCopyPTSerial()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Clipboard.SetText(Selected_Work_Order.PT_Serial);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Message_Window.Error(msg).ShowDialog();
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region RepairCompleteCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: RepairCompleteCommand</summary>
        public DelegateCommand RepairCompleteCommand { get; set; }
        private Boolean CanCommandRepairComplete() { return true; }
        private void CommandRepairComplete()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Thread repair = new Thread(new ThreadStart(CommandRepairComplete_Back));
                repair.IsBackground = true;
                repair.Start();
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

        /////////////////////////////////////////////
        #endregion


        #region ChangeStatusCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ChangeStatusCommand</summary>
        public DelegateCommand ChangeStatusCommand { get; set; }

        private Boolean CanCommandChangeStatus() { return true; }
        private void CommandChangeStatus()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Thread change = new Thread(new ThreadStart(CommandChangeStatus_Back));
                change.IsBackground = true;
                change.Start();
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Message_Window.Error(msg).ShowDialog();
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_Disclaimer_Reject_Handler  -- Event: SART_Disclaimer_Reject_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_Disclaimer_Reject_Handler(String wo)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Selected_Work_Order != null)
                {
                    Work_Order_Events.Cancel_Current_Work_Order(eventAggregator, regionManager, false);
                    InfrastructureModule.Current_Work_Order = null;
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //#region WorkOrder_AutoSave_Delete_Handler  -- Event: WorkOrder_AutoSave_Delete_Event Handler
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //private void WorkOrder_AutoSave_Delete_Handler(Boolean delete)
        //{
        //    try
        //    {
        //        logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
        //        if (delete)
        //        {
        //            Work_Order_Events.Delete_AutoSave();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(Exception_Helper.FormatExceptionString(ex));
        //        //throw;
        //    }
        //    finally
        //    {
        //        logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
        //    }
        //}

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //#endregion
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Close_Handler  -- Event: SART_WorkOrder_Close_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Close_Handler(Boolean close)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                CommandFilter();
                InfrastructureModule.Original_Work_Order = null;
                if (WorkOrderList != null)
                {
                    foreach (SART_Work_Order_View wo in WorkOrderList)
                    {
                        if (wo.Work_Order_ID == Last_Selected_Work_Order_ID)
                        {
                            Selected_Work_Order = wo;
                            break;
                        }
                    }
                }

                CAN2_Commands.JTags = null;
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Cancel_Handler  -- Event: SART_WorkOrder_Cancel_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Cancel_Handler(Boolean cancel)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SART_WorkOrder_Close_Handler(cancel);
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Logout_Handler  -- Event: Application_Logout_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Logout_Handler(Boolean close)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                WorkOrderList = null;
                eventAggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Work Order", View.ViewName));
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Clear_List_Handler  -- Event: WorkOrder_Clear_List_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Clear_List_Handler(Boolean clear)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                WorkOrderList = null;
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_AutoSave_Handler  -- Event: WorkOrder_AutoSave_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_AutoSave_Handler(Boolean save)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (save)
                {
                    if (autoSave != null) WorkOrder_AutoSave_Handler(false);

                    if (Configuration_Helper.GetAppSettingBoolean("Auto Save", true) == false)
                    {
                        logger.Info("Auto Save is deactivated");
                        return;
                    }

                    autoSave = new DispatcherTimer(DispatcherPriority.Background); //, Save_WorkOrder, null);
                    autoSave.Interval = new TimeSpan(0, 1, 0);// 1 minute
                    autoSave.Tick += new EventHandler(Work_Order_Events.AutoSave_WorkOrder);
                    autoSave.Start();
                    logger.Info("Auto Save has been started");
                }
                else if (autoSave != null)
                {
                    logger.Info("Auto Save is being stopped");
                    autoSave.Stop();
                    autoSave = null;
                }
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Login_Handler  -- Event: Application_Login_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Login_Handler(String msg)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                _LoginContext = null;
                //Syteline_FSPartner_Web_Service_Client_REST.Insert_Partner(InfrastructureModule.Token);
                CommandSettingsClear();
                //UserName = LoginContext.UserName;
                StartDate = DateTime.Today.AddMonths(-1);
                SART_ToolBar_Group_Manager.Level = LoginContext.User_Level;
                Work_Order_Events.Set_ToolBar(false, false, eventAggregator);
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        public bool IsNavigationTarget(NavigationContext navigationContext) { return true; }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            eventAggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Work Order", Work_Order_Open_Control.Control_Name));
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Selection_Event>().Publish("Work Order");
            //eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("Work Order");

            OnPropertyChanged("StatusStrings");
            //logger.Debug("Status Codes Count: {0}", Status_Codes == null ? 0 : Status_Codes.Count);
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        public void Open_Work_Order(Open_Mode openMode)
        {
            Boolean Is_I2 = false;
            Boolean Is_X2 = false;

            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("");
            eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
            try
            {
                logger.Debug("Retrieving Work Order: {0}", Selected_Work_Order.Work_Order_ID);
                Boolean OpenOnly = LoginContext.User_Level < UserLevels.Expert;
                SART_Work_Order wo = Syteline_WO_Web_Service_Client_REST.Select_SART_Work_Order_WORK_ORDER_ID(InfrastructureModule.Token, Selected_Work_Order.Work_Order_ID/*, OpenOnly*/);
                if (wo == null)
                {
                    String msg = $"An internal error has occurred trying to open the Work Order: {Selected_Work_Order.Work_Order_ID}";
                    logger.Error(msg);
                    Message_Window.Error(msg).ShowDialog();
                    return;
                }

                if (openMode == Open_Mode.Read_Write)
                {
                    if (String.IsNullOrEmpty(wo.Opened_By) == false) //Work_Order_WorkingStatuses.Opened.ToString())
                    {
                        if (String.Compare(wo.Opened_By, LoginContext.UserName, true) == 0)
                        {
                            SART_Common.Create_Event(WorkOrder_Events.ReOpened_Work_Order, Event_Statuses.Passed, WO: wo.Work_Order_ID);
                        }
                        else
                        {
                            String msg = String.Format("Work Order: {0} is already opened by: {1}", Selected_Work_Order.Work_Order_ID, wo.Opened_By);
                            eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
                            logger.Warn(msg);
                            Message_Window.Warning(msg).ShowDialog();
                            return;
                        }
                    }

                    //if (Work_Order_Events.Test_For_AutoSave(wo.Work_Order_ID) == true)
                    //{
                    //    InfrastructureModule.Current_Work_Order = Work_Order_Events.Read_AutoSave(wo.Work_Order_ID);
                    //    if (InfrastructureModule.Current_Work_Order == null) InfrastructureModule.Current_Work_Order = wo;
                    //}
                    //else
                    //{
                    //    InfrastructureModule.Current_Work_Order = wo;
                    //}
                    InfrastructureModule.Current_Work_Order = wo;
                }
                else
                {
                    InfrastructureModule.Current_Work_Order = wo;
                }
                InfrastructureModule.WorkOrder_OpenMode = openMode;

                OnPropertyChanged("PT_Serial_Number");
                OnPropertyChanged("Work_Order_Number");

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                logger.Info("Has Starting Configuration: {0}", InfrastructureModule.Current_Work_Order.Is_Start_Config);
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


                //IsWarranty = Convert.ToBoolean(InfrastructureModule.Current_Work_Order.Warranty);
                //Customer_Complaint = InfrastructureModule.Current_Work_Order.Customer_Complaint;
                //Unit_Condition = InfrastructureModule.Current_Work_Order.Unit_Condition;


                ////////////////////////////////////////////////////////////////////////////////
                // Checking for Model
                try
                {
                    Is_I2 = false;
                    Is_X2 = false;
                    logger.Debug("Checking for Model type");
                    if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.PT_Model) == false)
                    {
                        String ptModel = InfrastructureModule.Current_Work_Order.PT_Model.ToUpper();
                        if (ptModel.StartsWith("SE_") || ptModel.StartsWith("G2_")) ptModel = ptModel.Substring(3, 2);
                        Is_I2 = ptModel == "I2" ? true : false;
                        Is_X2 = ptModel == "X2" ? true : false;
                    }
                    else if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.PT_Part_Number) == false)
                    {
                        String pn = InfrastructureModule.Current_Work_Order.PT_Part_Number.Replace("-", "");
                        if (Flight_Code_Model.ContainsKey(pn) == true)
                        {
                            InfrastructureModule.Current_Work_Order.PT_Model = Flight_Code_Model[pn];
                            Is_I2 = InfrastructureModule.Current_Work_Order.PT_Model.ToUpper() == "I2" ? true : false;
                            Is_X2 = InfrastructureModule.Current_Work_Order.PT_Model.ToUpper() == "X2" ? true : false;
                        }
                        else
                        {
                            String msg = String.Format("There is no flight code model information available for part number: {0}.", pn);
                            String msg1 = String.Format("{0}  Please CANCEL out of this work order and contact Segway Technical Support to have this information entered.  It is found under the \"User Defined\" tab of the Item form in Syteline.", msg);

                            Message_Window.Error(msg1).ShowDialog();
                            logger.Warn(msg);
                        }
                    }
                    else
                    {
                        String msg = String.Format("There is no PT Model or PT Part Number associated to work order: {0} (PT: {1})", InfrastructureModule.Current_Work_Order.PT_Serial, InfrastructureModule.Current_Work_Order.PT_Part_Number);
                        String msg1 = String.Format("{0}  Please CANCEL out of this work order and have this information entered before trying to continue.  ", msg);
                        logger.Warn(msg);
                        Message_Window.Error(msg1).ShowDialog();
                    }
                }
                catch (Authentication_Exception ae)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(ae));
                    eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
                    return;
                }
                catch (Exception ex)
                {
                    String msg = Exception_Helper.FormatExceptionString("An exception has occurred.  Please upload your log (Shft-F11) and contact Segway Technical Support for further assistance.", ex);
                    Message_Window.Error(msg).ShowDialog();
                    logger.Error(msg);
                }
                finally
                {
                    logger.Debug("Done Checking for Model type");
                }
                // Checking for Model
                ////////////////////////////////////////////////////////////////////////////////


                ////////////////////////////////////////////////////////////////////////////////
                // Checking for Error Code

                Boolean PTCannotStart = false;
                Boolean HasNoErrorCode = false;
                Boolean HasErrorCode = false;
                try
                {
                    logger.Debug("Checking for Error Code");
                    if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Error_Code) == false)
                    {
                        if ((InfoKey_Codes != null) && (InfoKey_Codes.ContainsKey(InfrastructureModule.Current_Work_Order.Error_Code)))
                        {
                            HasErrorCode = true;
                        }
                    }
                    //HasNoErrorCode = InfrastructureModule.Current_Work_Order.Error_Code_None;
                    //PTCannotStart = InfrastructureModule.Current_Work_Order.Error_Code_NO_Start;

                    if (InfrastructureModule.Current_Work_Order.Error_Code_None.HasValue)
                    {
                        if (InfrastructureModule.Current_Work_Order.Error_Code_None.Value == true)
                        {
                            HasNoErrorCode = true;
                        }
                    }
                    if (InfrastructureModule.Current_Work_Order.Error_Code_NO_Start.HasValue == true)
                    {
                        if (InfrastructureModule.Current_Work_Order.Error_Code_NO_Start.Value == true)
                        {
                            PTCannotStart = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString(ex));
                    //throw;
                }
                finally
                {
                    logger.Debug("Done checking for Error Code");
                }
                // Checking for Error Code
                ////////////////////////////////////////////////////////////////////////////////


                if (openMode == Open_Mode.Read_Only)
                {
                    Work_Order_Events.Open_WorkOrder(true);
                }
                else if ((String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Unit_Condition) == true) ||
                    (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Customer_Complaint) == true) ||
                    ((Is_I2 ^ Is_X2) == false) ||
                    ((HasErrorCode == false) && (HasNoErrorCode == false) && (PTCannotStart == false))
                    )
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        regionManager.RequestNavigate(RegionNames.MainRegion, Work_Order_Update_Control.Control_Name);
                    });
                }
                else
                {
                    SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                    criteria.Add(new FieldData("Work_Order_ID", Selected_Work_Order.Work_Order_ID));
                    criteria.Add(new FieldData("User_Name", LoginContext.UserName));
                    criteria.Add(new FieldData("Status", (int)Disclaimer_Statuses.Accepted));
                    List<SART_Disclaimer> sdList = SART_DISCLAIMER_Web_Service_Client_REST.Select_SART_Disclaimer_Criteria(InfrastructureModule.Token, criteria);
                    if ((sdList == null) || (sdList.Count == 0))
                    {
                        if ((LoginContext.User_Level >= UserLevels.Expert) && (InfrastructureModule.User_Settings != null) && (InfrastructureModule.User_Settings.Disclaimer == Disclaimer_Statuses.Accepted))
                        {
                            SART_Disclaimer sd = new SART_Disclaimer(LoginContext.UserName, InfrastructureModule.Current_Work_Order.Work_Order_ID);
                            if (SART_DISCLAIMER_Web_Service_Client_REST.Insert_SART_Disclaimer_Key(InfrastructureModule.Token, sd) != null)
                            {
                                logger.Debug("Assigned accepted disclaimer - user: {0}, work order: {1}", LoginContext.UserName, InfrastructureModule.Current_Work_Order.Work_Order_ID);
                                // Open work order and nNavigate to Work Order Summary
                                Work_Order_Events.Open_WorkOrder(true);
                                return;
                            }
                        }
                        Work_Order_Events.Open_WorkOrder(false);

                        // Navigate to Disclaimer
                        eventAggregator.GetEvent<SART_Disclaimer_Accept_Navigate_Event>().Publish(Work_Order_Summary_Control.Control_Name);
                        //eventAggregator.GetEvent<SART_Disclaimer_Accept_Navigate_Event>().Publish(Work_Order_Update_Control.Control_Name);
                        eventAggregator.GetEvent<SART_Disclaimer_Reject_Navigate_Event>().Publish(Work_Order_Open_Control.Control_Name);
                        Application.Current.Dispatcher.Invoke((Action)delegate ()
                        {
                            regionManager.RequestNavigate(RegionNames.MainRegion, Disclaimer_Control.Control_Name);
                        });
                        return;
                    }

                    logger.Debug("Found accepted disclaimer: {0}", criteria);
                    // Open work order and nNavigate to Work Order Summary
                    Work_Order_Events.Open_WorkOrder(true);
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
            }
            catch (Exception e)
            {
                logger.Error(Exception_Helper.FormatExceptionString(e));
            }
            finally
            {
                CAN2_Commands.JTags = null;
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
            }
        }


        private void Requested_Filter(SqlBooleanCriteria criteria)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                logger.Debug("criteria: {0}", criteria);
                if ((criteria == null) || (criteria.FieldData_List == null) || (criteria.FieldData_List.Count == 0) || (InfrastructureModule.Token == null))
                {
                    WorkOrderList = new List<SART_Work_Order_View>();
                }
                else
                {
                    WorkOrderList = Syteline_WO_Web_Service_Client_REST.Get_Work_Order_List(InfrastructureModule.Token, criteria, LoginContext.User_Level >= UserLevels.Expert ? false : true);
                }
            }
            catch (Authentication_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
                return;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        public SqlBooleanCriteria Create_Criteria()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SqlBooleanCriteria criteria = new SqlBooleanCriteria();


                if (String.IsNullOrEmpty(PTSerialNumber) == false)
                {
                    criteria.Add(new FieldData("PT_Serial", PTSerialNumber, FieldCompareOperator.Contains));
                }

                if (String.IsNullOrEmpty(WorkOrderNumber) == false)
                {
                    String woid = WorkOrderNumber.Replace("*", "%");
                    criteria.Add(new FieldData("Work_Order_ID", woid, SegwayFieldTypes.StringInsensitive, FieldCompareOperator.Contains));
                }

                if ((LoginContext != null) && (LoginContext.User_Level < UserLevels.Expert))
                {
                    criteria.Add(new FieldData("Customer_Number", LoginContext.Customer_ID, FieldCompareOperator.EndsWith));
                }


                if (String.IsNullOrEmpty(SelectedStatusString) == false)
                {
                    criteria.Add(new FieldData("Status", SelectedStatusString));
                }

                if (criteria.FieldData_List.Count == 0)
                {
                    if (String.IsNullOrEmpty(UserName) == false)
                    {
                        criteria = new SqlBooleanCriteria(BooleanCompareOperator.Or);
                        criteria.Add(new FieldData("Technician", UserName, SegwayFieldTypes.StringInsensitive, FieldCompareOperator.Contains));
                        criteria.Add(new FieldData("Entered_By", UserName, SegwayFieldTypes.StringInsensitive, FieldCompareOperator.Contains));
                        criteria.Add(new FieldData("Opened_By", UserName, SegwayFieldTypes.StringInsensitive, FieldCompareOperator.Contains));
                    }
                }
                else if (String.IsNullOrEmpty(UserName) == false)
                {
                    criteria.Add(new FieldData("Technician", UserName, SegwayFieldTypes.StringInsensitive, FieldCompareOperator.Contains));
                }

                return criteria;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                return null;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void Add_Copy_WorkOrder(ContextMenu menu)
        {
            if (Selected_Work_Order != null)
            {
                MenuItem mi = new MenuItem();
                mi.Name = "I" + Selected_Work_Order.Work_Order_ID;
                mi.Header = "Copy Work Order to ClipBoard";
                mi.Click += new RoutedEventHandler(CopyWorkOrder_Click);
                menu.Items.Add(mi);
            }
        }

        private void CopyWorkOrder_Click(object sender, RoutedEventArgs e)
        {
            CommandCopyWorkOrder();
        }

        private void Add_Copy_PTSerial(ContextMenu menu)
        {
            if (Selected_Work_Order != null)
            {
                MenuItem mi = new MenuItem();
                mi.Name = "I" + Selected_Work_Order.Work_Order_ID;
                mi.Header = "Copy PT Serial to ClipBoard";
                mi.Click += new RoutedEventHandler(CopyPTSerial_Click);
                menu.Items.Add(mi);
            }
        }

        private void CopyPTSerial_Click(object sender, RoutedEventArgs e)
        {
            CommandCopyPTSerial();
        }



        private void Add_Repair_Complete(ContextMenu menu)
        {
            if (Selected_Work_Order != null)
            {
                MenuItem mi = new MenuItem();
                mi.Name = "I" + Selected_Work_Order.Work_Order_ID;
                mi.Header = "Change Status to Repair Complete";
                mi.Click += new RoutedEventHandler(Repair_Complete_Click);
                menu.Items.Add(mi);
            }
        }

        private void Repair_Complete_Click(object sender, RoutedEventArgs e)
        {
            CommandRepairComplete();
        }


        private void Add_ReOpen_WorkOrder(ContextMenu menu)
        {
            if (Selected_Work_Order != null)
            {
                MenuItem mi = new MenuItem();
                mi.Name = "I" + Selected_Work_Order.Work_Order_ID;
                mi.Header = "ReOpen Work Order";
                mi.Click += new RoutedEventHandler(ReOpen_WorkOrder_Click);
                menu.Items.Add(mi);
            }
        }


        private void Add_Close_WorkOrder(ContextMenu menu)
        {
            if (Selected_Work_Order != null)
            {
                MenuItem mi = new MenuItem();
                mi.Name = "I" + Selected_Work_Order.Work_Order_ID;
                mi.Header = "Change State of Work Order to Close";
                mi.Click += new RoutedEventHandler(Close_WorkOrder_Click);
                menu.Items.Add(mi);
            }
        }


        private void Add_Change_Status(ContextMenu menu)
        {
            if (Selected_Work_Order != null)
            {
                MenuItem mi = new MenuItem();
                mi.Name = "I" + Selected_Work_Order.Work_Order_ID;
                mi.Header = "Change Work Order Status";
                mi.Click += new RoutedEventHandler(Change_Status_Click);
                menu.Items.Add(mi);
            }
        }

        private void Change_Status_Click(object sender, RoutedEventArgs e)
        {
            CommandChangeStatus();
        }

        private void Close_WorkOrder_Click(object sender, RoutedEventArgs e)
        {
            Task back = new Task(Close_WorkOrder_Back);
            back.ContinueWith(ExceptionHander, TaskContinuationOptions.OnlyOnFaulted);
            back.ContinueWith(Close_Completed_Hander, TaskContinuationOptions.OnlyOnRanToCompletion);
            back.Start();
            logger.Debug("Started background process: Close_WorkOrder_Back");
        }

        private void ReOpen_WorkOrder_Click(object sender, RoutedEventArgs e)
        {
            Task back = new Task(ReOpen_WorkOrder_Back);
            back.ContinueWith(ExceptionHander, TaskContinuationOptions.OnlyOnFaulted);
            back.ContinueWith(ReOpen_Completed_Hander, TaskContinuationOptions.OnlyOnRanToCompletion);
            back.Start();
            logger.Debug("Started background process: ReOpen_Back");
        }

        private void ReOpen_WorkOrder_Back()
        {
            if (Syteline_FSSRO_Web_Service_Client_REST.ReOpen_WorkOrder(Token, Selected_Work_Order.Work_Order_ID) == false)
            {
                throw new Exception(String.Format("Work Order: {0} could not be Closed Out", Selected_Work_Order.Work_Order_ID));
            }
        }

        private void Close_WorkOrder_Back()
        {
            if (Syteline_FSSRO_Web_Service_Client_REST.CloseOut_WorkOrder(Token, Selected_Work_Order.Work_Order_ID) == false)
            {
                throw new Exception(String.Format("Work Order: {0} could not be Closed Out", Selected_Work_Order.Work_Order_ID));
            }
        }



        private void CommandChangeStatus_Back()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate ()
            {
                try
                {
                    logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                    Entry_Window ew = new Entry_Window(Window_Add_Types.CustomList);
                    List<String> statuses = new List<String>(Status_Codes.Values);
                    statuses.Remove("Repair Complete");
                    statuses.Sort();
                    ew.Set_List(statuses);
                    if (ew.ShowDialog() == true)
                    {
                        eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                        Change_Work_Order_WorkingStatus(ew.SelectedString);
                        var store = Selected_Work_Order;

                        CommandFilter();

                        foreach (var item in WorkOrderList)
                        {
                            if (item.Work_Order_ID == store.Work_Order_ID)
                            {
                                Selected_Work_Order = item;
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    String msg = Exception_Helper.FormatExceptionString(ex);
                    logger.Error(msg);
                    Message_Window.Error(msg).ShowDialog();
                }
                finally
                {
                    logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
                }
            });
            eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
        }

        private void Change_Work_Order_WorkingStatus(String sts)
        {
            logger.Debug("Selected status: {0}", sts);
            var wo = Syteline_WO_Web_Service_Client_REST.Select_SART_Work_Order_WORK_ORDER_ID(InfrastructureModule.Token, Selected_Work_Order.Work_Order_ID);
            if (wo == null)
            {
                throw new Exception(String.Format("Could not retrieve Work Order: {0}", Selected_Work_Order.Work_Order_ID));
            }
            foreach (var status in Status_Codes)
            {
                if (status.Value == sts)
                {
                    wo.Stat_Code = status.Key;
                    break;
                }
            }
            if (Syteline_WO_Web_Service_Client_REST.Update_SART_Work_Order_Object(InfrastructureModule.Token, wo) == false)
            {
                throw new Exception($"Unable to save Work Order: {Selected_Work_Order.Work_Order_ID}");
            }
        }


        private void CommandRepairComplete_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                String WO_ID = null;
                String PT_Serial = null;

                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    WO_ID = Selected_Work_Order.Work_Order_ID;
                    PT_Serial = Selected_Work_Order.PT_Serial;
                }
                );

                try
                {
                    Update_Assembly_Table(WO_ID, PT_Serial);

                    Change_Work_Order_WorkingStatus("Repair Complete");

                    eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Publish(false);
                    //Application.Current.Dispatcher.Invoke((Action)delegate ()
                    //{
                    //    var store = Selected_Work_Order;

                    //    CommandFilter();

                    //    foreach (var item in WorkOrderList)
                    //    {
                    //        if (item.Work_Order_ID == store.Work_Order_ID)
                    //        {
                    //            Selected_Work_Order = item;
                    //            return;
                    //        }
                    //    }
                    //});
                }
                catch
                {
                    throw;
                }
                finally
                {
                }
            }
            catch (Repair_Exception re)
            {
                logger.Warn(re.Message);
                List<String> msgLines = new List<string>(Strings.Split(re.Message, "\r\n", StringSplitOptions.RemoveEmptyEntries));
                StringBuilder sb = new StringBuilder("The following condition");
                if (msgLines.Count > 1) sb.Append("s were");
                else sb.Append(" was");
                sb.AppendLine(" found in the repair items and must be fixed before this process can be performed:");
                foreach (var m in msgLines) sb.AppendLine(String.Format("\t- {0}", m));
                Message_Window.Warning(sb.ToString().TrimEnd()).ShowDialog();
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Message_Window.Error(msg).ShowDialog();
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private void Update_Assembly_Table(String work_order_id, String pt_serial)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SART_PT_Configuration config = Retrieve_Last_Configuration(work_order_id, pt_serial);

                List<SART_WO_Components> lineItems = Retrieve_Repair_Line_Items(work_order_id);
                if (lineItems != null)
                {
                    foreach (var item in lineItems)
                    {
                        var mcaNew = Manufacturing_MCA_Web_Service_Client_REST.Select_Manufacturing_Component_Assemblies_CHILD_SERIAL_Active(Token, item.Serial_Number_New);
                        if (mcaNew != null)
                        {
                            throw new Repair_Exception(String.Format("Serialized Component: {0} ({1}) is already assigned to: {2}", mcaNew.Child_Serial, mcaNew.Part_Type, mcaNew.Parent_Serial));
                        }

                        var mca = Manufacturing_MCA_Web_Service_Client_REST.Select_Manufacturing_Component_Assemblies_PARENT_CHILD_TYPE_LOCATION(Token, pt_serial, item.Serial_Number_Old, item.Part_Number, item.Location);
                        if (mca != null)
                        {
                            mca = Manufacturing_MCA_Web_Service_Client_REST.Terminate_Assembly(Token, mca, LoginContext.UserName);
                        }

                        mca.Removed_By = null;
                        mca.Date_Time_Removed = null;
                        mca.ID = Guid.Empty;
                        mca.Child_Serial = item.Serial_Number_New;

                        mca = Manufacturing_MCA_Web_Service_Client_REST.Insert_Manufacturing_Component_Assemblies_Object(Token, mca);
                        if (mca == null)
                        {
                            throw new Repair_Exception(String.Format("Could not assign component Assembly: {0} ({1}) to: {2}", mcaNew.Child_Serial, mcaNew.Part_Type, mcaNew.Parent_Serial));
                        }
#if false

                        switch (item.Part_Number)
                        {
                            // Motor
                            case "2225200001":
                                Replace_Motor(item, pt_serial, work_order_id);
                                break;

                            // CU Board
                            case "2324400001":
                                Replace_CU(item, config, pt_serial, work_order_id);
                                break;

                            // BSA
                            case "1945700003":
                                Replace_BSA(item, config, pt_serial, work_order_id);
                                break;

                            // Pivot Base - G2
                            case "2124900001":
                            //break;

                            //Pivot Base - SE
                            case "2336100001":
                                Replace_PivotBase(item, config, pt_serial, work_order_id);
                                break;

                            //UI Electronics - G2 
                            case "2013000001":
                                Replace_UIC(item, config, pt_serial, work_order_id);
                                break;

                            //Radio Board - G2
                            case "1945500002":
                                break;

                            //Radio Board - SE
                            case "2345100001":
                            //break;

                            //AC Filter Board 
                            case "2340500001":
                            //break;

                            //Charger Power Supply    
                            case "2337100001":
                                Replace_SE_PivotBoards(item, pt_serial, work_order_id);
                                break;

                        }
#endif
                    }
                }
            }
            catch (Repair_Exception re)
            {
                logger.Info(re.Message);
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

        private List<SART_WO_Components> Retrieve_Repair_Line_Items(String work_order_id)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Boolean found = false;

                var clist = SART_WOComp_Web_Service_Client_REST.Select_SART_WO_Components_WORK_ORDER_ID(InfrastructureModule.Token, work_order_id);
                if (clist != null)
                {
                    Validate_Repair_Lines(clist);

                    var asmParts = InfrastructureModule.Assembly_Table_Parts;
                    if (asmParts != null)
                    {
                        List<SART_WO_Components> comps = new List<SART_WO_Components>();
                        foreach (var item in clist)
                        {
                            if (asmParts.ContainsKey(item.Part_Number) == true)
                            {
                                if (asmParts[item.Part_Number].Ignore_Validation == true) continue;
                                found = true;
                                comps.Add(item);
                            }
                        }
                        if (found == true)
                        {
                            logger.Debug("Returning {0} components", comps.Count);
                            return comps;
                        }
                    }
                }
                return null;
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


        private SART_PT_Configuration Retrieve_Last_Configuration(String work_order_id, String pt_serial)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Boolean error = false;
                String errMsg = null;
                List<SART_PT_Configuration> configs = null;
                if (container.IsRegistered<List<SART_PT_Configuration>>("Configurations") == true)
                {
                    try
                    {
                        logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                        configs = container.Resolve<List<SART_PT_Configuration>>("Configurations");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(Exception_Helper.FormatExceptionString(ex));
                        configs = PTConfiguration.Get_PT_Configuration(pt_serial, work_order_id, out error, out errMsg);
                    }
                }
                else configs = PTConfiguration.Get_PT_Configuration(pt_serial, work_order_id, out error, out errMsg);
                if (error == true)
                {
                    String msg = String.Format("The following error(s) occurred while retrieving configuration data:\n{0}", errMsg);
                    logger.Error(msg);
                    Message_Window.Error(msg).ShowDialog();
                    return null;
                }
                if (configs == null)
                {
                    logger.Warn("Retrieval of configurations for PT: {0} returned a null", pt_serial);
                    return null;
                }
                if (configs.Count == 0)
                {
                    logger.Warn("Retrieval of configurations for PT: {0} returned 0 configurations", pt_serial);
                    return null;
                }
                logger.Debug("Found {0} configurations", configs.Count);
                configs.Sort(new SART_PT_Configuration_Created_Comparer());
                SART_PT_Configuration config = new SART_PT_Configuration();
                foreach (var cnf in configs)
                {
                    config.Update(cnf);
                }
                logger.Debug("Returning the older configuration");
                return config;
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


        private String Find_Assembly_Part_Number(String pn)
        {
            if (String.IsNullOrEmpty(pn) == false)
            {
                if (InfrastructureModule.Assembly_Table_Parts.ContainsKey(pn) == true)
                {
                    return InfrastructureModule.Assembly_Table_Parts[pn].Assembly_Part_Number;
                }
            }
            return null;
        }


        private String Find_Assembly_Part_Type(String pn)
        {
            if (String.IsNullOrEmpty(pn) == false)
            {
                if (InfrastructureModule.Assembly_Table_Parts.ContainsKey(pn) == true)
                {
                    return InfrastructureModule.Assembly_Table_Parts[pn].Assembly_Part_Type;
                }
            }
            return null;
        }


#if false
        private void Replace_CU(SART_WO_Components repair, SART_PT_Configuration config, String pt_serial, String work_order_id)
        {
            if (config == null) throw new Exception("No configuration data");


            var PT = Manufacturing_Web_Service_Client.Select_Manufacturing_Component_Serialized_SERIAL_NUMBER(pt_serial);
            String repairSite = App_Settings_Helper.GetConfigurationValue("ToolSite", "");
            if (String.IsNullOrEmpty(repairSite) == true)
            {
                if (InfrastructureModule.Token.LoginContext.User_Level >= UserLevels.Expert)
                {
                    repairSite = "S";
                }
                else
                {
                    repairSite = "F";
                }
                Configuration_Helper.SetConfigurationValue("ToolSite", repairSite);
            }

            Type t = typeof(SART_PT_Configuration);
            foreach (String cuLocation in new String[] { "A", "B" })
            {
                var prop = t.GetProperty(String.Format("CU{0}_Serial", cuLocation)); //, BindingFlags.Public);
                if (prop != null)
                {
                    Object obj = prop.GetValue(config, null);
                    if (obj != null)
                    {
                        String propertyData = obj.ToString();

                        var cuboard = Manufacturing_Web_Service_Client.Select_Manufacturing_Component_Serialized_SERIAL_NUMBER(propertyData);
                        if (cuboard != null)
                        {
                            logger.Debug("Found CU: {0}", propertyData);
                            if (cuboard.Assembly_Status == PT_Assembly_Statuses.Assigned)
                            {
                                if (String.IsNullOrEmpty(cuboard.Parent_Serial) == false)
                                {
                                    if (cuboard.Parent_Serial == PT.Serial_Number)
                                    {
                                        if (cuboard.Location == cuLocation)
                                        {
                                            logger.Info("CU: {0} is already assigned to PT: {1} to side: {2}", propertyData, PT.Serial_Number, cuLocation);
                                            continue;
                                        }
                                    }
                                }
                                else if (cuboard.Parent_ID.HasValue)
                                {
                                    if (cuboard.Parent_ID.Value == PT.ID)
                                    {
                                        if (cuboard.Location == cuLocation)
                                        {
                                            logger.Info("CU: {0} is already assigned to PT: {1} to side: {2}", propertyData, PT.Serial_Number, cuLocation);
                                            continue;
                                        }
                                    }
                                }
                                Manufacturing_Web_Service_Client.Terminate_SubAssembly(cuboard, InfrastructureModule.Token.LoginContext.UserName);
                            }
                        }

                        var cus = Manufacturing_Web_Service_Client.Select_Descendent_Assemblies_Serial(pt_serial, String.Format("CU{0}_Board", cuLocation), "Part_Type");
                        if ((cus != null) && (cus.Count > 0))
                        {
                            Boolean alreadyAssigned = false;
                            foreach (var cu in cus)
                            {
                                if (cu.Serial_Number != propertyData)
                                {
                                    Manufacturing_Web_Service_Client.Terminate_SubAssembly(cu, InfrastructureModule.Token.LoginContext.UserName);
                                }
                                else alreadyAssigned = true;
                            }

                            if (alreadyAssigned == false)
                            {
                                Production_Line_Assembly cu = new Production_Line_Assembly();
                                cu.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                                cu.Location = cuLocation;
                                cu.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                                cu.Part_Type = "CU_Board";
                                cu.Serial_Number = propertyData;
                                //cu.Work_Order = work_order_id;

                                Manufacturing_Web_Service_Client.Add_Child_Assembly(PT, cu, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                            }
                        }
                        else
                        {
                            Production_Line_Assembly cu = new Production_Line_Assembly();
                            cu.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                            cu.Location = cuLocation;
                            cu.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                            cu.Part_Type = "CU_Board";
                            cu.Serial_Number = propertyData;
                            //cu.Work_Order = work_order_id;

                            Manufacturing_Web_Service_Client.Add_Child_Assembly(PT, cu, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                        }
                    }
                }
            }
        }


        private void Replace_BSA(SART_WO_Components repair, SART_PT_Configuration config, String pt_serial, String work_order_id)
        {
            if (config == null) throw new Exception("No configuration data");


            var PT = Manufacturing_Web_Service_Client.Select_Assembly_Serial(pt_serial);
            String repairSite = App_Settings_Helper.GetConfigurationValue("ToolSite", "");


            Type t = typeof(SART_PT_Configuration);
            var prop = t.GetProperty("BSA_A_Serial");
            if (prop != null)
            {
                Object obj = prop.GetValue(config, null);
                if (obj != null)
                {
                    String propertyData = obj.ToString();

                    var bsaBoard = Manufacturing_Web_Service_Client.Select_Assembly_Serial(propertyData);
                    if (bsaBoard != null)
                    {
                        logger.Debug("Found BSA: {0}", propertyData);
                        if (bsaBoard.Assembly_Status == PT_Assembly_Statuses.Assigned)
                        {
                            if (String.IsNullOrEmpty(bsaBoard.Parent_Serial) == false)
                            {
                                if (bsaBoard.Parent_Serial == PT.Serial_Number)
                                {
                                    logger.Info("BSA: {0} is already assigned to PT: {1}", propertyData, PT.Serial_Number);
                                    return;
                                }
                            }
                            else if (bsaBoard.Parent_ID.HasValue)
                            {
                                if (bsaBoard.Parent_ID.Value == PT.ID)
                                {
                                    logger.Info("BSA: {0} is already assigned to PT: {1}", propertyData, PT.Serial_Number);
                                    return;
                                }
                            }
                            Assembly_Line_Web_Service_Client.Terminate_SubAssembly(bsaBoard, InfrastructureModule.Token.LoginContext.UserName);
                        }
                    }

                    var bsas = Manufacturing_Web_Service_Client.Select_Descendent_Assemblies_Serial(pt_serial, "BSA", "Part_Type");
                    if ((bsas != null) && (bsas.Count > 0))
                    {
                        Boolean alreadyAssigned = false;
                        foreach (var bsa in bsas)
                        {
                            if (bsa.Serial_Number != propertyData)
                            {
                                Manufacturing_Web_Service_Client.Terminate_SubAssembly(bsa, InfrastructureModule.Token.LoginContext.UserName);
                            }
                            else alreadyAssigned = true;
                        }

                        if (alreadyAssigned == false)
                        {
                            Production_Line_Assembly bsa = new Production_Line_Assembly();
                            bsa.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                            bsa.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                            bsa.Part_Type = "BSA";
                            bsa.Serial_Number = propertyData;
                            //cu.Work_Order = work_order_id;

                            Manufacturing_Web_Service_Client.Add_Child_Assembly(PT, bsa, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                        }
                    }
                    else
                    {
                        Production_Line_Assembly bsa = new Production_Line_Assembly();
                        bsa.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                        bsa.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                        bsa.Part_Type = "BSA";
                        bsa.Serial_Number = propertyData;
                        //cu.Work_Order = work_order_id;

                        Manufacturing_Web_Service_Client.Add_Child_Assembly(PT, bsa, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                    }

                    //String propertyName = prop.Name.ToUpper();

                    //else if (propertyName == "BSA_A_SERIAL")
                    //{
                    //    Object obj = prop.GetValue(config, null);
                    //    if (obj == null) continue;
                    //}
                    //else if (propertyName == "UIC_SERIAL")
                    //{
                    //    Object obj = prop.GetValue(config, null);
                    //    if (obj == null) continue;
                    //}
                    //else if (propertyName == "PIVOT_SERIAL")
                    //{
                    //    Object obj = prop.GetValue(config, null);
                    //    if (obj == null) continue;
                    //}
                }
            }
        }


        private void Replace_PivotBase(SART_WO_Components repair, SART_PT_Configuration config, String pt_serial, String work_order_id)
        {
            if (config == null) throw new Exception("No configuration data");


            var PT = Manufacturing_Web_Service_Client.Select_Assembly_Serial(pt_serial);
            String repairSite = App_Settings_Helper.GetConfigurationValue("ToolSite", "");


            Type t = typeof(SART_PT_Configuration);
            var prop = t.GetProperty("Pivot_Serial");
            if (prop != null)
            {
                String configSerial = null;
                String pivotSerial = repair.Serial_Number_New;
                Object obj = prop.GetValue(config, null);
                if (obj != null)
                {
                    configSerial = obj.ToString();
                }

                if ((String.IsNullOrEmpty(configSerial) == true) && (String.IsNullOrEmpty(pivotSerial) == true))
                {
                    throw new Repair_Exception("There's no serial number in the repair item or the configuration for: PivotBase");
                }
                else if ((String.IsNullOrEmpty(configSerial) == false) && (String.IsNullOrEmpty(pivotSerial) == false))
                {
                    if (configSerial != pivotSerial)
                    {
                        throw new Repair_Exception("There's a serial number in the repair item and the configuration, but they don't match for: PivotBase");
                    }
                }
                else if ((String.IsNullOrEmpty(configSerial) == true) && (String.IsNullOrEmpty(pivotSerial) == false))
                {
                    configSerial = pivotSerial;
                }


                var pivot = Manufacturing_Web_Service_Client.Select_Assembly_Serial(configSerial);
                if (pivot != null)
                {
                    logger.Debug("Found Pivot: {0}", configSerial);
                    if (pivot.Assembly_Status == PT_Assembly_Statuses.Assigned)
                    {
                        if (String.IsNullOrEmpty(pivot.Parent_Serial) == false)
                        {
                            if (pivot.Parent_Serial == PT.Serial_Number)
                            {
                                logger.Info("Pivot: {0} is already assigned to PT: {1}", configSerial, PT.Serial_Number);
                                return;
                            }
                            throw new Repair_Exception(String.Format("Pivot: {0} is already assigned to PT: {1}", configSerial, pivot.Parent_Serial));
                        }
                        else if (pivot.Parent_ID.HasValue)
                        {
                            if (pivot.Parent_ID.Value == PT.ID)
                            {
                                logger.Info("Pivot: {0} is already assigned to PT: {1}", configSerial, PT.Serial_Number);
                                return;
                            }
                            var pt = Manufacturing_Web_Service_Client.Select_Production_Line_Assembly_Key(pivot.Parent_ID.Value);
                            if (pt == null)
                            {
                                throw new Repair_Exception(String.Format("Pivot: {0} is already assigned to a non existent PT", configSerial));
                            }
                            else
                            {
                                throw new Repair_Exception(String.Format("Pivot: {0} is already assigned to PT: {1}", configSerial, pt.Serial_Number));
                            }
                        }
                    }
                }


                String parttype = Find_Assembly_Part_Type(repair.Part_Number);
                var pivots = Manufacturing_Web_Service_Client.Select_Descendent_Assemblies_Serial(pt_serial, parttype, "Part_Type");
                if ((pivots == null) || (pivots.Count == 0))
                {
                    Production_Line_Assembly pvt = new Production_Line_Assembly();
                    pvt.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                    pvt.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                    pvt.Part_Type = parttype;
                    pvt.Serial_Number = configSerial;
                    //cu.Work_Order = work_order_id;

                    Manufacturing_Web_Service_Client.Add_Child_Assembly(PT, pvt, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                }
                else if (pivots.Count == 1)
                {
                    if (pivots[0].Serial_Number == configSerial)
                    {
                        logger.Info("Pivot: {0} is already assigned to PT: {1}", configSerial, PT.Serial_Number);
                        return;
                    }
                    else
                    {
                        Production_Line_Assembly pvt = new Production_Line_Assembly();
                        pvt.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                        pvt.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                        pvt.Part_Type = parttype;
                        pvt.Serial_Number = configSerial;

                        logger.Debug("Replacing Pivot: {0} with Pivot: {1}", pivots[0].Serial_Number, configSerial);
                        pvt = Manufacturing_Web_Service_Client.Replace_Child_Assembly(pivots[0], pvt, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                        logger.Debug("Transferring children");
                        var original = Manufacturing_Web_Service_Client.Select_SubAssembly(pivots[0].Serial_Number);
                        Manufacturing_Web_Service_Client.Transfer_Child_Assemblies(original, pvt, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);

                        //var list = Manufacturing_Web_Service_Client.Select_SubAssembly_Parent_Current(pivots[0].ID);
                        //if (list != null)
                        //{
                        //    foreach (var child in list)
                        //    {
                        //        Assembly_Line_Web_Service_Client.Terminate_SubAssembly(child, InfrastructureModule.Token.LoginContext.UserName, null);
                        //        var chld = Assembly_Line_Web_Service_Client.Select_SubAssembly(child.Serial_Number);
                        //        Assembly_Line_Web_Service_Client.Add_Child_Assembly(pvt, chld, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                        //    }
                        //}
                    }
                }
                else
                {
                    throw new Repair_Exception(String.Format("PT: {0} contains more than one {1} ({2})", pt_serial, parttype, pivots.Count));
                }
            }
        }

        private void Replace_SE_PivotBoards(SART_WO_Components repair, String pt_serial, String work_order_id)
        {
            if (repair == null) throw new ArgumentNullException("Parameter repair (SART_WO_Component) can not be null.");

            if (String.IsNullOrEmpty(repair.Serial_Number_New) == true)
            {
                throw new Repair_Exception(String.Format("The repair item for part: {0} does not contain a new serial number", repair.Part_Number));
            }

            String repairSite = App_Settings_Helper.GetConfigurationValue("ToolSite", "");
            var PT = Manufacturing_Web_Service_Client.Select_Assembly_Serial(pt_serial);
            var parttype = Find_Assembly_Part_Type(repair.Part_Number);

            logger.Debug("Searching for pivot");
            Production_Line_Assembly pivot = null;
            var plas = Manufacturing_Web_Service_Client.Select_Descendent_Assemblies_Serial(pt_serial, PT_Assemblies.PivotBaseSE.ToString());
            if (plas == null) throw new Repair_Exception(String.Format("PT: {0} does not have a PivotBase assembly.", pt_serial));
            else if (plas.Count == 0) throw new Repair_Exception(String.Format("PT: {0} does not have a PivotBase assembly.", pt_serial));
            else if (plas.Count > 1) throw new Repair_Exception(String.Format("PT: {0} has more than one PivotBase assembly.", pt_serial));
            else pivot = plas[0];


            logger.Debug("Does {0}: {1} exist and is it assigned", parttype, repair.Serial_Number_New);
            var board = Manufacturing_Web_Service_Client.Select_Assembly_Serial(repair.Serial_Number_New);
            if (board != null)
            {
                if (board.Assembly_Status == PT_Assembly_Statuses.Assigned)
                {
                    if (board.Master_ID.HasValue == true)
                    {
                        if (board.Master_ID.Value != PT.ID)
                        {
                            var assignPT = Manufacturing_Web_Service_Client.Select_Production_Line_Assembly_Key(board.Master_ID.Value);
                            if (assignPT.Serial_Number != PT.Serial_Number)
                            {
                                throw new Repair_Exception(String.Format("{1}: {0} has already been assigned to PT:{2}.", repair.Serial_Number_New, parttype, assignPT.Serial_Number));
                            }
                            else
                            {
                                logger.Debug("{0}: {1} is already assigned", parttype, repair.Serial_Number_New);
                                return;
                            }
                        }
                        else
                        {
                            logger.Debug("{0}: {1} is already assigned as a descendant to PT: {2}", parttype, repair.Serial_Number_New, PT.Serial_Number);
                            return;
                        }
                    }
                    else if (String.IsNullOrEmpty(board.Master_Serial) == false)
                    {
                        if (board.Master_Serial != PT.Serial_Number)
                        {
                            throw new Repair_Exception(String.Format("{1}: {0} has already been assigned to PT:{2}.", repair.Serial_Number_New, parttype, board.Master_Serial));
                        }
                        else
                        {
                            logger.Debug("{0}: {1} is already assigned", parttype, repair.Serial_Number_New);
                            return;
                        }
                    }
                }
            }

            plas = Manufacturing_Web_Service_Client.Select_Descendent_Assemblies_PLA(PT, parttype);
            if ((plas == null) || (plas.Count == 0))
            {
                logger.Debug("Did not find a child of type: {0} for PT: {1}", parttype, PT.Serial_Number);
                board = new Production_Line_Assembly();
                board.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                board.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                board.Part_Type = parttype;
                board.Serial_Number = repair.Serial_Number_New;
                Manufacturing_Web_Service_Client.Add_Child_Assembly(pivot, board, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
            }
            else if (plas.Count == 1)
            {
                board = new Production_Line_Assembly();
                board.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                board.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                board.Part_Type = parttype;
                board.Serial_Number = repair.Serial_Number_New;

                logger.Debug("Replacing {0}", parttype);
                Manufacturing_Web_Service_Client.Replace_Child_Assembly(plas[0], board, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
            }
            else
            {
                String msg = String.Format("PT: {0} contains more than one {1} ({2})", pt_serial, parttype, plas.Count);
                logger.Error(msg);
                throw new Repair_Exception(msg);
                //foreach (var pla in plas)
                //{
                //    if (pla.Serial_Number != repair.Serial_Number_New)
                //    {
                //        Assembly_Line_Web_Service_Client.Terminate_SubAssembly(pla, InfrastructureModule.Token.LoginContext.UserName);
                //    }
                //}
                //board = new Production_Line_Assembly();
                //board.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                //board.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                //board.Part_Type = parttype;
                //board.Serial_Number = repair.Serial_Number_New;
                //Manufacturing_Web_Service_Client.Add_Child_Assembly(pivot, board, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
            }
        }

        private void Replace_UIC(SART_WO_Components repair, SART_PT_Configuration config, String pt_serial, String work_order_id)
        {
            if (config == null) throw new Exception("No configuration data");


            Production_Line_Assembly pivot = null;
            var PT = Manufacturing_Web_Service_Client.Select_Assembly_Serial(pt_serial);
            String repairSite = App_Settings_Helper.GetConfigurationValue("ToolSite", "");
            var plas = Manufacturing_Web_Service_Client.Select_Descendent_Assemblies_Serial(pt_serial, "PivotBase", "Part_Type");
            if ((plas != null) && (plas.Count > 0))
            {
                pivot = plas[plas.Count - 1];
            }

            Type t = typeof(SART_PT_Configuration);
            var prop = t.GetProperty("UIC_Serial");
            if (prop != null)
            {
                String configSerial = null;
                String uicSerial = repair.Serial_Number_New;
                Object obj = prop.GetValue(config, null);
                if (obj != null)
                {
                    configSerial = obj.ToString();
                }

                if ((String.IsNullOrEmpty(configSerial) == true) && (String.IsNullOrEmpty(uicSerial) == true))
                {
                    throw new Repair_Exception("There's no serial number in the repair item or the configuration for: UIC");
                }
                else if ((String.IsNullOrEmpty(configSerial) == false) && (String.IsNullOrEmpty(uicSerial) == false))
                {
                    if (configSerial != uicSerial)
                    {
                        throw new Repair_Exception("There's a serial number in the repair item and the configuration, but they don't match for: UIC");
                    }
                }
                else if ((String.IsNullOrEmpty(configSerial) == true) && (String.IsNullOrEmpty(uicSerial) == false))
                {
                    configSerial = uicSerial;
                }

                {
                    var uic = Manufacturing_Web_Service_Client.Select_Assembly_Serial(configSerial);
                    if (uic != null)
                    {
                        logger.Debug("Found UIC: {0}", configSerial);
                        if (uic.Assembly_Status == PT_Assembly_Statuses.Assigned)
                        {
                            if (String.IsNullOrEmpty(uic.Parent_Serial) == false)
                            {
                                ///////////////////////////////////////
                                // Already assigned to the current PT
                                if (uic.Parent_Serial == PT.Serial_Number)
                                {
                                    logger.Info("UIC: {0} is already assigned to PT: {1}", configSerial, PT.Serial_Number);
                                    return;
                                }

                                ///////////////////////////////////////
                                // Already assigned to another PT
                                else
                                {
                                    /////////////////////////////////////////////
                                    // Should this condition throw an exception?
                                    //throw new Repair_Exception(String.Format("UIC: {0} is already assigned to PT: {1}", configSerial, uic.Parent_Serial));
                                    Manufacturing_Web_Service_Client.Remove_SubAssembly(uic, InfrastructureModule.Token.LoginContext.UserName);
                                }
                            }
                            else if (uic.Parent_ID.HasValue)
                            {
                                ///////////////////////////////////////
                                // Already assigned to the current PT
                                if (uic.Parent_ID.Value == PT.ID)
                                {
                                    logger.Info("UIC: {0} is already assigned to PT: {1}", configSerial, PT.Serial_Number);
                                    return;
                                }

                                ///////////////////////////////////////
                                // Already assigned to another PT
                                else
                                {
                                    /////////////////////////////////////////////
                                    // Should this condition throw an exception?
                                    //var otherPT = Manufacturing_Web_Service_Client.Select_Production_Line_Assembly_Key(uic.Parent_ID.Value);
                                    //throw new Repair_Exception(String.Format("UIC: {0} is already assigned to PT: {1}", configSerial, otherPT.Serial_Number));
                                    Manufacturing_Web_Service_Client.Remove_SubAssembly(uic, InfrastructureModule.Token.LoginContext.UserName);
                                }
                            }
                        }
                    }
                }

                String parttype = Find_Assembly_Part_Type(repair.Part_Number);
                var uics = Manufacturing_Web_Service_Client.Select_Descendent_Assemblies_Serial(pt_serial, parttype, "Part_Type");
                if ((uics == null) || (uics.Count == 0))
                {
                    Production_Line_Assembly uicons = new Production_Line_Assembly();
                    uicons.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                    uicons.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                    uicons.Part_Type = parttype;
                    uicons.Serial_Number = configSerial;

                    Manufacturing_Web_Service_Client.Add_Child_Assembly(PT, uicons, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                }
                else if (uics.Count == 1)
                {
                    var uic = uics[0];
                    if (uic.Serial_Number == configSerial)
                    {
                        logger.Info("UIC: {0} is already assigned to PT: {1}", configSerial, PT.Serial_Number);
                        return;
                    }
                    Production_Line_Assembly uicons = new Production_Line_Assembly();
                    uicons.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                    uicons.Part_Number = Find_Assembly_Part_Number(repair.Part_Number);
                    uicons.Part_Type = parttype;
                    uicons.Serial_Number = configSerial;
                    Manufacturing_Web_Service_Client.Replace_Child_Assembly(uic, uicons, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                }
                else
                {
                    throw new Repair_Exception(String.Format("PT: {0} contains more than one {1} ({2})", pt_serial, parttype, uics.Count));
                }
            }
        }


        private void Replace_Motor(SART_WO_Components item, String pt_serial, String work_order_id)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (String.IsNullOrEmpty(item.Serial_Number_New) == true)
                {
                    throw new Exception("The new serial number of the motor was not provided.");
                }
                else if (Serial_Validation.Is_G2_Motor_Serial(item.Serial_Number_New) == false)
                {
                    throw new Exception("The new serial number of the motor is not formatted properly.");
                }
                logger.Debug("The new serial number exists: {0} in repair item record", item.Serial_Number_New);



                var motorold = Manufacturing_Web_Service_Client.Select_Manufacturing_Component_Serialized_SERIAL_NUMBER(item.Serial_Number_Old);
                if (motorold == null)
                {
                    var pla = Manufacturing_Web_Service_Client.Select_Production_Line_Assembly_SERIAL_NUMBER_Active(item.Serial_Number_Old);
                    if (pla == null) throw new Exception(String.Format("Unable to find the serialized component: {0}", item.Serial_Number_Old));
                    motorold = new Manufacturing_Component_Serialized();
                    motorold.Copy(pla);
                    motorold = Manufacturing_Web_Service_Client.Insert_Manufacturing_Component_Serialized_Key(motorold);
                }

                var motornew = Manufacturing_Web_Service_Client.Select_Manufacturing_Component_Serialized_SERIAL_NUMBER(item.Serial_Number_New);
                if (motornew == null)
                {
                    var pla = Manufacturing_Web_Service_Client.Select_Production_Line_Assembly_SERIAL_NUMBER_Active(item.Serial_Number_New);
                    if (pla == null)
                    {
                        motornew = new Manufacturing_Component_Serialized(motorold);
                        motornew.Serial_Number = item.Serial_Number_New;
                    }
                    else
                    {
                        motornew = new Manufacturing_Component_Serialized();
                        motornew.Copy(pla);
                    }
                    motornew = Manufacturing_Web_Service_Client.Insert_Manufacturing_Component_Serialized_Key(motorold);
                }


                String location = null;
                DateTime dt = DateTime.Now;
                var mca = Manufacturing_Web_Service_Client.Select_Manufacturing_Component_Assemblies_PARENT_CHILD_TYPE_LOCATION(pt_serial, item.Serial_Number_Old, motorold.Part_Type);
                if (mca != null)
                {
                    location = mca.Position;
                    mca.Date_Time_Removed = dt;
                    mca.Removed_By = InfrastructureModule.Token.LoginContext.UserName;
                    Manufacturing_Web_Service_Client.Update_Manufacturing_Component_Assemblies_Key(mca);
                }


                mca = Manufacturing_Web_Service_Client.Select_Manufacturing_Component_Assemblies_PARENT_CHILD_TYPE_LOCATION(pt_serial, item.Serial_Number_New, motorold.Part_Type);
                if (mca != null)
                {
                    mca.Date_Time_Removed = dt;
                    mca.Removed_By = InfrastructureModule.Token.LoginContext.UserName;
                    Manufacturing_Web_Service_Client.Update_Manufacturing_Component_Assemblies_Key(mca);
                }

                mca = new Manufacturing_Component_Assemblies()
                {
                    Child_Serial = item.Serial_Number_New,
                    Parent_Serial = pt_serial,
                    Created_By = InfrastructureModule.Token.LoginContext.UserName,
                    Date_Time_Created = dt,
                    Part_Type = motornew.Part_Type,
                    Site = "S",
                    Work_Order = work_order_id,
                    Position = location
                };
                Manufacturing_Web_Service_Client.Update_Manufacturing_Component_Assemblies_Key(mca);

#if false
                //List<Manufacturing_Component_Assemblies> oldMotorList = null;
                if (String.IsNullOrEmpty(item.Serial_Number_Old) == false)
                {
                    logger.Debug("The old serial number exists: {0} in repair item record", item.Serial_Number_Old);
                    var oldMotor = Manufacturing_Web_Service_Client.Select_Manufacturing_Component_Assemblies_PARENT_CHILD_TYPE_LOCATION(pt_serial, item.Serial_Number_Old);
                    //Assembly_Line_Web_Service_Client.Select_Descendent_Assemblies_Serial(pt_serial, item.Serial_Number_Old, "Serial_Number");

                    if (oldMotor != null)
                    {
                        if (oldMotorList.Count == 0)
                        {
                            logger.Debug("Did not find the old motor in the production assembly table");
                        }
                        else if (oldMotorList.Count == 1)
                        {
                            logger.Debug("Found the motor in the production assembly table");
                            Production_Line_Assembly oldMotor = oldMotorList[0];

                            if (oldMotor.Serial_Number != item.Serial_Number_New)
                            {
                                Production_Line_Assembly newMotor = new Production_Line_Assembly();
                                newMotor.Serial_Number = item.Serial_Number_New;
                                newMotor.Part_Number = Find_Assembly_Part_Number(item.Part_Number);
                                newMotor.Part_Type = "Motor";
                                //oldMotor.Updated_By = newMotor.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                                newMotor.Location = item.Location;
                                newMotor.Master_Serial = newMotor.Parent_Serial = pt_serial;
                                String repairSite = App_Settings_Helper.GetConfigurationValue("ToolSite", "");
                                logger.Debug("Replacing the old motor with the new one");
                                newMotor = Assembly_Line_Web_Service_Client.Replace_Child_Assembly(oldMotor, newMotor, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                                if ((newMotor == null) || (newMotor.ID == 0))
                                {
                                }
                            }
                            return;
                        }
                        else
                        {
                            logger.Warn("Found multiple motor in the production assembly table with the serial number: {0}", item.Serial_Number_Old);
                        }
                    }
                }


                var pt = Assembly_Line_Web_Service_Client.Select_SubAssembly(pt_serial);
                oldMotorList = Assembly_Line_Web_Service_Client.Select_SubAssembly_Type(pt.ID, "Motor");
                var oldMotorBList = Assembly_Line_Web_Service_Client.Select_SubAssembly_Type(pt.ID, "MotorBlack");
                var oldMotorWList = Assembly_Line_Web_Service_Client.Select_SubAssembly_Type(pt.ID, "MotorWhite");

                if (oldMotorList == null) oldMotorList = new List<Production_Line_Assembly>();
                if (oldMotorBList != null) oldMotorList.AddRange(oldMotorBList);
                if (oldMotorWList != null) oldMotorList.AddRange(oldMotorWList);

                if (oldMotorList.Count == 0)
                {
                    Production_Line_Assembly newMotor = new Production_Line_Assembly();
                    newMotor.Serial_Number = item.Serial_Number_New;
                    newMotor.Part_Number = Find_Assembly_Part_Number(item.Part_Number);
                    newMotor.Part_Type = "Motor";
                    //oldMotor.Updated_By = newMotor.Created_By = InfrastructureModule.Token.LoginContext.UserName;
                    newMotor.Location = item.Location;
                    newMotor.Master_Serial = newMotor.Parent_Serial = pt_serial;
                    String repairSite = App_Settings_Helper.GetConfigurationValue("ToolSite", "");
                    Assembly_Line_Web_Service_Client.Add_Child_Assembly(pt, newMotor, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                    return;
                }
                else if (oldMotorList.Count == 1)
                {
                    Production_Line_Assembly oldMotor = oldMotorList[0];
                    if ((String.IsNullOrEmpty(item.Location) == false) && (String.IsNullOrEmpty(oldMotor.Location) == false))
                    {
                        if (item.Location != oldMotor.Location)
                        {
                            Production_Line_Assembly newMotor = new Production_Line_Assembly();
                            newMotor.Serial_Number = item.Serial_Number_New;
                            newMotor.Part_Number = Find_Assembly_Part_Number(item.Part_Number);
                            newMotor.Part_Type = "Motor";
                            newMotor.Location = item.Location;
                            newMotor.Master_Serial = newMotor.Parent_Serial = pt_serial;
                            String repairSite = App_Settings_Helper.GetConfigurationValue("ToolSite", "");
                            Assembly_Line_Web_Service_Client.Add_Child_Assembly(pt, newMotor, InfrastructureModule.Token.LoginContext.UserName, repairSite, work_order_id);
                            return;
                        }
                    }
                }
#endif
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


#endif

        private void Validate_Repair_Lines(List<SART_WO_Components> items)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                StringBuilder sb = new StringBuilder();
                foreach (var item in items)
                {
                    if (InfrastructureModule.Assembly_Table_Parts.ContainsKey(item.Part_Number) == true)
                    {
                        if (InfrastructureModule.Assembly_Table_Parts[item.Part_Number].Ignore_Validation == true) continue;
                    }
                    if (String.IsNullOrEmpty(item.Installed) == true)
                    {
                        sb.AppendLine(String.Format("Part: {0} [{1}] does not have an Installed indicator", item.Part_Number, item.Part_Name));
                    }
                    else if (String.IsNullOrEmpty(item.Change_Approval) == true)
                    {
                        sb.AppendLine(String.Format("Part: {0} [{1}] does not have an Approval indicator", item.Part_Number, item.Part_Name));
                    }
                    else if (item.Installed == "Yes")
                    {
                        if (item.Change_Approval == "Declined")
                        {
                            sb.AppendLine(String.Format("Part: {0} [{1}] has been installed, yet the customer declined the repair", item.Part_Number, item.Part_Name));
                        }
                    }
                    else if (item.Installed == "No")
                    {
                        if (item.Change_Approval == "Approved")
                        {
                            sb.AppendLine(String.Format("Part: {0} [{1}] has not been installed, yet the customer approved the repair", item.Part_Number, item.Part_Name));
                        }
                    }
                }

                if (sb.Length > 0)
                {
                    throw new Repair_Exception(sb.ToString());
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


        private void CommandOpenWO_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                Open_Work_Order(Open_Mode.Read_Write);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        ////////////////////////////////////////
        // Close Exception Handler
        private void ExceptionHander(Task obj)
        {
            CommandFilter();
            String msg = Exception_Helper.FormatExceptionString(obj.Exception);
            logger.Error(msg);

            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        Message_Window.Error(msg).ShowDialog();
                    });
                }
            }
        }
        // Close Exception Handler
        ////////////////////////////////////////

        ////////////////////////////////////////
        // Close Completion Handler
        private void Close_Completed_Hander(Task obj)
        {
            CommandFilter();
            String msg = "Work Order was closed Successfully";
            logger.Info(msg);

            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        Message_Window.Success(msg).ShowDialog();
                    });
                }
            }
        }
        // Close Completion Handler
        ////////////////////////////////////////


        ////////////////////////////////////////
        // ReOpen Completion Handler
        private void ReOpen_Completed_Hander(Task obj)
        {
            CommandFilter();
            String msg = "Work Order re-opened Successfully";
            logger.Info(msg);

            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        Message_Window.Success(msg).ShowDialog();
                    });
                }
            }

        }
        // ReOpen Completion Handler
        ////////////////////////////////////////



        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
