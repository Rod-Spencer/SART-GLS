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

using Segway.Modules.Login;
using Segway.Modules.SART_Infrastructure;
using Segway.Service.Controls.StatusBars;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder.Services;
using Segway.Modules.WorkOrder;
using Segway.SART.Objects;
using Segway.Service.Common.LoggerHelp;
using Segway.Service.Common;
using Segway.Service.Controls.ListBoxToolBar;
using Segway.Service.DatabaseHelper;
using Segway.Service.Disclaimer;
using Segway.Service.HeartBeat.Client;
using Segway.Service.Manufacturing.Client;
using Segway.Service.Objects;
using Segway.Service.SART2012.Client;
using Segway.Service.WebService.Client;
using Segway.Modules.Administration;
using Segway.Syteline.Objects;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using SART_Infrastructure;


namespace Segway.Modules.WorkOrder
{
    public class WorkOrderViewModel : ViewModelBase, IWorkOrderViewModel, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;
        private Login_Context loginContext;
        private Dictionary<String, InfoKey_Error_Codes> InfoKey_Codes = null;
        private int LoadCounter = 0;
        private Dictionary<int, String> DealerNames = new Dictionary<int, String>();

        private static String AutoSaveFolder = "Auto Save";
        DispatcherTimer autoSave = null;

        public WorkOrderViewModel(IWorkOrderView view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;
            logger.Debug("Entered the NewWorkOrder constructor");


            FilterCommand = new DelegateCommand(CommandFilter, CanCommandFilter);
            ClearCommand = new DelegateCommand(CommandClear, CanCommandClear);
            UpdateWorkOrderCommand = new DelegateCommand(CommandUpdateWorkOrder, CanCommandUpdateWorkOrder);

            //eventAggregator.GetEvent<SART_Disclaimer_Reject_Event>().Subscribe(Close_On_Disclaimer_Reject, true);
            //eventAggregator.GetEvent<SART_Disclaimer_Accept_Event>().Subscribe(Open_On_Disclaimer_Accept, true);
            //eventAggregator.GetEvent<WorkOrder_Close_Event>().Subscribe(Close_Work_Order, true);
            ////eventAggregator.GetEvent<SART_Infrastructure_Clear_DealerList_Event>().Subscribe(Updated_DealerList, true);
            //eventAggregator.GetEvent<Application_Logout_Event>().Subscribe(ApplicationLogout, true);
            //eventAggregator.GetEvent<WorkOrder_Clear_List_Event>().Subscribe(Clear_WO_List, true);
            //eventAggregator.GetEvent<WorkOrder_AutoSave_Event>().Subscribe(AutoSave_Handler, ThreadOption.UIThread, true);
            //eventAggregator.GetEvent<WorkOrder_AutoSave_Delete_Event>().Subscribe(Delete_AutoSave_Handler, ThreadOption.BackgroundThread, true);

            logger.Debug("Flight Code Models count: 0}", Flight_Code_Model.Count);
        }

        public IEventAggregator Aggregator { get { return eventAggregator; } }


        #region Miscellaneous Propterties


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
                        _Flight_Code_Model = SART_2012_Web_Service_Client.Get_Gen2_Flight_Code_Models();
                        if (_Flight_Code_Model == null) _Flight_Code_Model = new Dictionary<String, String>();
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

        #endregion

        #region Control Properties

        #region Status_Codes

        private Dictionary<String, String> _Status_Codes;

        /// <summary>Property Status_Codes of type Dictionary<String,String></summary>
        public Dictionary<String, String> Status_Codes
        {
            get
            {
                if (_Status_Codes == null)
                {
                    _Status_Codes = SART_2012_Web_Service_Client.Get_Statuses();
                    //if (codes != null)
                    //{
                    //    foreach (FS_Stat_Code code in codes)
                    //    {
                    //        _Status_Codes.Add(code.Description, code.Stat_Code);
                    //    }
                    //}
                    container.RegisterInstance<Dictionary<String, String>>("Status_Codes", _Status_Codes, new ContainerControlledLifetimeManager());
                }
                return _Status_Codes;
            }
            set { _Status_Codes = value; }
        }

        #endregion

        #region SelectedPanel

        private int _SelectedPanel = 1;

        /// <summary>Property SelectedPanel of type int</summary>
        public int SelectedPanel
        {
            get { return _SelectedPanel; }
            set
            {
                _SelectedPanel = value;
                OnPropertyChanged("SelectedPanel");
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


        #region New Work Order Properties

        //#region PTCannotStart

        //private Boolean _PTCannotStart = false;

        ///// <summary>Property PTCannotStart of type Boolean</summary>
        //public Boolean PTCannotStart
        //{
        //    get { return _PTCannotStart; }
        //    set
        //    {
        //        _PTCannotStart = value;
        //        OnPropertyChanged("PTCannotStart");
        //        UpdateWorkOrderCommand.RaiseCanExecuteChanged();
        //    }
        //}

        //#endregion

        //#region HasErrorCode

        //private Boolean _HasErrorCode = true;

        ///// <summary>Property HasErrorCode of type Boolean</summary>
        //public Boolean HasErrorCode
        //{
        //    get { return _HasErrorCode; }
        //    set
        //    {
        //        _HasErrorCode = value;
        //        if (_HasErrorCode == false) Selected_Code = null;
        //        OnPropertyChanged("HasErrorCode");
        //    }
        //}

        //#endregion

        //#region HasNoErrorCode

        //private Boolean _HasNoErrorCode = false;

        ///// <summary>Property HasNoErrorCode of type Boolean</summary>
        //public Boolean HasNoErrorCode
        //{
        //    get { return _HasNoErrorCode; }
        //    set
        //    {
        //        _HasNoErrorCode = value;
        //        OnPropertyChanged("HasNoErrorCode");
        //        UpdateWorkOrderCommand.RaiseCanExecuteChanged();
        //    }
        //}

        //#endregion


        //#region InfoKey_Error_Codes

        //private ObservableCollection<String> _InfoKey_Error_Codes;

        ///// <summary>Property InfoKey_Error_Codes of type List<String></summary>
        //public ObservableCollection<String> InfoKey_Error_Codes
        //{
        //    get { return _InfoKey_Error_Codes; }
        //    set
        //    {
        //        _InfoKey_Error_Codes = value;
        //        OnPropertyChanged("InfoKey_Error_Codes");
        //    }
        //}

        //#endregion

        //#region Selected_Code

        //private String _Selected_Code;

        ///// <summary>Property Selected_Code of type String</summary>
        //public String Selected_Code
        //{
        //    get { return _Selected_Code; }
        //    set
        //    {
        //        _Selected_Code = value;
        //        if (String.IsNullOrWhiteSpace(_Selected_Code)) Error_Code_Description = "";
        //        else if (InfoKey_Codes.ContainsKey(_Selected_Code) == false) Error_Code_Description = "";
        //        else Error_Code_Description = InfoKey_Codes[_Selected_Code].Display;
        //        OnPropertyChanged("Selected_Code");
        //        UpdateWorkOrderCommand.RaiseCanExecuteChanged();
        //    }
        //}

        //#endregion

        //#region Error_Code_Description

        //private String _Error_Code_Description; // = "Error code description goes here";

        ///// <summary>Property Error_Code_Description of type String</summary>
        //public String Error_Code_Description
        //{
        //    get { return _Error_Code_Description; }
        //    set
        //    {
        //        _Error_Code_Description = value;
        //        OnPropertyChanged("Error_Code_Description");
        //    }
        //}

        //#endregion


        #region PT_Serial_Number

        /// <summary>Property PT_Serial_Number of type String</summary>
        public String PT_Serial_Number
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.PT_Serial;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;
                InfrastructureModule.Current_Work_Order.PT_Serial = value;
                OnPropertyChanged("PT_Serial_Number");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Work_Order_Number

        //private String _Work_Order_Number;

        /// <summary>Property Work_Order_Number of type String</summary>
        public String Work_Order_Number
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Work_Order_ID;
            }
            set
            {
                OnPropertyChanged("Work_Order_Number");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region WorkOrderColor

        /// <summary>Property WorkOrderColor of type Brush</summary>
        public Brush WorkOrderColor
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null)
                {
                    return new BrushConverter().ConvertFromString("#FF67A1DC") as System.Windows.Media.Brush;
                }
                else if (InfrastructureModule.Current_Work_Order.Priority == "Incident")
                {
                    return System.Windows.Media.Brushes.Red;
                }
                else
                {
                    return new BrushConverter().ConvertFromString("#FF67A1DC") as System.Windows.Media.Brush;
                }
            }
            set { OnPropertyChanged("WorkOrderColor"); }
        }

        #endregion


        //#region IsI2Enabled

        //private Boolean _IsI2Enabled = true;

        ///// <summary>Property IsI2Enabled of type Boolean</summary>
        //public Boolean IsI2Enabled
        //{
        //    get { return _IsI2Enabled; }
        //    set
        //    {
        //        _IsI2Enabled = value;
        //        OnPropertyChanged("IsI2Enabled");
        //    }
        //}

        //#endregion

        //#region IsX2Enabled

        //private Boolean _IsX2Enabled = true;

        ///// <summary>Property IsX2Enabled of type Boolean</summary>
        //public Boolean IsX2Enabled
        //{
        //    get { return _IsX2Enabled; }
        //    set
        //    {
        //        _IsX2Enabled = value;
        //        OnPropertyChanged("IsX2Enabled");
        //    }
        //}

        //#endregion

        //public Boolean Model_Valid { get; set; }

        //#region Is_I2

        //private Boolean _Is_I2;

        ///// <summary>Property Is_I2 of type Boolean</summary>
        //public Boolean Is_I2
        //{
        //    get { return _Is_I2; }
        //    set
        //    {
        //        _Is_I2 = value;
        //        if (_Is_I2 == true)
        //        {
        //            Model_Valid = false;
        //            logger.Debug("Searching Stage1_Partnum for PT: {0}", PT_Serial_Number);
        //            List<Stage1_Partnum> partList = Manufacturing_Tables_Web_Service_Client.Select_Stage1_Partnum_UNIT_ID_SERIAL_NUMBER(PT_Serial_Number);
        //            if ((partList == null) || (partList.Count == 0))
        //            {
        //                // TODO: Some sort of error message goes here.
        //                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Could not find model type for PT: {0}", PT_Serial_Number));
        //            }
        //            else
        //            {
        //                Stage1_Partnum model = partList[partList.Count - 1];
        //                logger.Debug("Found Stage1_Partnum record(s) for PT: {0} ({1})", PT_Serial_Number, model.Unit_ID_Partnumber);
        //                if (model.Unit_ID_Partnumber.ToUpper() != "I2")
        //                {
        //                    logger.Warn("Model type: I2 did not match Segway database for PT: {0} ({1})", PT_Serial_Number, model.Unit_ID_Partnumber);
        //                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Model type: I2 did not match Segway database");
        //                }
        //                else
        //                {
        //                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Ready");
        //                    Model_Valid = true;
        //                }
        //            }
        //        }
        //        OnPropertyChanged("Is_I2");
        //        UpdateWorkOrderCommand.RaiseCanExecuteChanged();
        //    }
        //}

        //#endregion

        //#region Is_X2

        //private Boolean _Is_X2;

        ///// <summary>Property Is_X2 of type Boolean</summary>
        //public Boolean Is_X2
        //{
        //    get { return _Is_X2; }
        //    set
        //    {
        //        _Is_X2 = value;
        //        if (_Is_X2 == true)
        //        {
        //            Model_Valid = false;
        //            logger.Debug("Searching Stage1_Partnum for PT: {0}", PT_Serial_Number);
        //            List<Stage1_Partnum> partList = Manufacturing_Tables_Web_Service_Client.Select_Stage1_Partnum_UNIT_ID_SERIAL_NUMBER(PT_Serial_Number);
        //            if ((partList == null) || (partList.Count == 0))
        //            {
        //                // TODO: Some sort of error message goes here.
        //                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Could not find model type for PT: {0}", PT_Serial_Number));
        //            }
        //            else
        //            {
        //                Stage1_Partnum model = partList[partList.Count - 1];
        //                logger.Debug("Found Stage1_Partnum record(s) for PT: {0} ({1})", PT_Serial_Number, model.Unit_ID_Partnumber);
        //                if (model.Unit_ID_Partnumber.ToUpper() != "X2")
        //                {
        //                    logger.Warn("Model type: X2 did not match Segway database for PT: {0} ({1})", PT_Serial_Number, model.Unit_ID_Partnumber);
        //                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Model type: X2 did not match Segway database");
        //                }
        //                else
        //                {
        //                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Ready");
        //                    Model_Valid = true;
        //                }
        //            }
        //        }
        //        OnPropertyChanged("Is_X2");
        //        UpdateWorkOrderCommand.RaiseCanExecuteChanged();
        //    }
        //}

        //#endregion

        #region IsWarranty

        /// <summary>Property IsWarranty of type Boolean</summary>
        public Boolean IsWarranty
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return false;
                return InfrastructureModule.Current_Work_Order.IsWarranty;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;
                InfrastructureModule.Current_Work_Order.IsWarranty = value;
                OnPropertyChanged("IsWarranty");
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Ready");
            }
        }

        #endregion

        public Boolean Condition_Valid { get; set; }

        #region Unit_Condition

        private const String ConditionDefault = "";
        private String _Unit_Condition = ConditionDefault;

        /// <summary>Property Unit_Condition of type String</summary>
        public String Unit_Condition
        {
            get { return _Unit_Condition; }
            set
            {
                _Unit_Condition = value;
                OnPropertyChanged("Unit_Condition");
                if ((String.IsNullOrWhiteSpace(_Unit_Condition) == true) || (_Unit_Condition == ConditionDefault))
                {
                    Condition_Valid = false;
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Description on PT condition must be entered.");
                }
                else
                {
                    Condition_Valid = true;
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Ready");
                }
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        public Boolean Complaint_Valid { get; set; }

        #region Customer_Complaint

        private const String ComplaintDefault = "";
        private String _Customer_Complaint = ComplaintDefault;

        /// <summary>Property Customer_Complaint of type String</summary>
        public String Customer_Complaint
        {
            get { return _Customer_Complaint; }
            set
            {
                _Customer_Complaint = value;
                OnPropertyChanged("Customer_Complaint");
                if ((String.IsNullOrWhiteSpace(Customer_Complaint) == true) || (Customer_Complaint == ComplaintDefault))
                {
                    Complaint_Valid = false;
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Description on customer complaint must be entered.");
                }
                else
                {
                    Complaint_Valid = true;
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Ready");
                }
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion



        #region Popup Controls

        #region PopupMessage

        private String _PopupMessage;

        /// <summary>ViewModel Property: PopupMessage of type: String</summary>
        public String PopupMessage
        {
            get { return _PopupMessage; }
            set
            {
                _PopupMessage = value;
                OnPropertyChanged("PopupMessage");
            }
        }

        #endregion

        #region PopupOpen

        private Boolean _PopupOpen;

        /// <summary>ViewModel Property: PopupOpen of type: Boolean</summary>
        public Boolean PopupOpen
        {
            get { return _PopupOpen; }
            set
            {
                _PopupOpen = value;
                OnPropertyChanged("PopupOpen");
            }
        }

        #endregion

        #region PopupColor

        private Brush _PopupColor;

        /// <summary>ViewModel Property: PopupColor of type: Brush</summary>
        public Brush PopupColor
        {
            get { return _PopupColor; }
            set
            {
                _PopupColor = value;
                OnPropertyChanged("PopupColor");
            }
        }

        #endregion

        #endregion

        #endregion

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

        #region BorderVisibility

        private Visibility _BorderVisibility = Visibility.Visible;

        /// <summary>Property BorderVisibility of type Visibility</summary>
        public Visibility BorderVisibility
        {
            get { return _BorderVisibility; }
            set
            {
                _BorderVisibility = value;
                OnPropertyChanged("BorderVisibility");
            }
        }

        #endregion

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
                _WorkOrderNumber = value;
                OnPropertyChanged("WorkOrderNumber");
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
                    _StatusStrings = new ObservableCollection<string>(Status_Codes.Values);  // MapStatusEnumerations();
                    _StatusStrings.Insert(0, "");
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

        #region LoginContext

        private Login_Context _LoginContext;

        /// <summary>Property LoginContext of type Login_Context</summary>
        public Login_Context LoginContext
        {
            get
            {
                if (_LoginContext == null)
                {
                    _LoginContext = (Login_Context)container.Resolve<Login_Context_Interface>();
                }
                return _LoginContext;
            }
            set { _LoginContext = value; }
        }

        #endregion

        #region DealerInfo

        private Dealer_Info _DealerInfo;

        /// <summary>Property DealerInfo of type Dealer_Info</summary>
        public Dealer_Info DealerInfo
        {
            get
            {
                if (_DealerInfo == null) _DealerInfo = container.Resolve<Dealer_Info>();
                return _DealerInfo;
            }
            set { _DealerInfo = value; }
        }

        #endregion

        #endregion

        #endregion

        #region INavigationAware

        bool INavigationAware.IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ListBoxToolBar_Visibility_Event>().Publish(false);
            eventAggregator.GetEvent<ListBoxToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Work Order", "WorkOrderView"));
        }


        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
            try
            {
                logger.Debug("Successfully navigated to WorkOrderView");
                //    this.eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Ready");

                if (loginContext == null) loginContext = (Login_Context)container.Resolve<Login_Context_Interface>();
                logger.Debug("Passed this into the Work Order VM - User is {0}, level is {1}, group ID is {2}",
                    loginContext.UserName, loginContext.User_Level.ToString(), loginContext.Group_ID.ToString());

                // Select ToolBar Group
                SART_ToolBar_Group_Manager.Level = loginContext.User_Level;
                ToolBar_Group_Manager.Select_Group(eventAggregator, SART_ToolBar_Group_Manager.GetGroupName);


                if ((String.IsNullOrEmpty(loginContext.UserName) == false) && (LoadCounter < 1))
                {
                    CreateDefaultFilterSettings();
                    Requested_Filter(Create_Criteria());
                }
                logger.Debug("WorkOrderViewModel::OnNavigatedTo - Just passed the call to web service");

                //if (InfoKey_Codes == null)
                //{
                //    List<InfoKey_Error_Codes> codes = SART_2012_Web_Service_Client.Select_InfoKey_Error_Codes_Criteria(null);
                //    if (codes != null)
                //    {
                //        codes.Sort(new InfoKey_Error_Codes_Comparer());
                //        ObservableCollection<String> keys = new ObservableCollection<String>();
                //        InfoKey_Codes = new Dictionary<String, InfoKey_Error_Codes>();
                //        foreach (InfoKey_Error_Codes code in codes)
                //        {
                //            InfoKey_Codes.Add(code.Code, code);
                //            keys.Add(code.Code);
                //        }
                //        InfoKey_Error_Codes = (ObservableCollection<String>)keys;
                //    }
                //}

                eventAggregator.GetEvent<ListBoxToolBar_Visibility_Event>().Publish(true);
                eventAggregator.GetEvent<ListBoxToolBar_Enabled_Event>().Publish(true);
                eventAggregator.GetEvent<ListBoxToolBar_Selection_Event>().Publish("Work Order");

                //var itemManager = new ToolBarItemManager(eventAggregator);
                //itemManager.HideToolBarItems(true);
                //if (loginContext.User_Level != UserLevels.Expert)
                //{
                //    // hide the administration view
                //    itemManager.HideAdminView(true);
                //}
                //else
                //{
                //    // may need to put it back if there was a logout / login
                //    itemManager.HideAdminView(false);
                //}

                SelectedPanel = 1;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(ex.Message);
            }
        }

        private void Requested_Filter(SqlBooleanList criteria)
        {
            Aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
            logger.Debug("criteria: {0}", criteria);
            WorkOrderList = SART_2012_Web_Service_Client.Get_Work_Order_List(criteria);
            Aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
        }


        #endregion

        #region Command Handlers

        #region FilterCommand

        /// <summary>Delegate Command: FilterCommand</summary>
        public DelegateCommand FilterCommand { get; set; }

        private Boolean CanCommandFilter() { return true; }

        private void CommandFilter()
        {
            Requested_Filter(Create_Criteria());
            IsExpanded = false;
        }

        #endregion

        #region UpdateWorkOrderCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: UpdateWorkOrderCommand</summary>
        public DelegateCommand UpdateWorkOrderCommand { get; set; }
        private Boolean CanCommandUpdateWorkOrder()
        {
            if (Complaint_Valid && Condition_Valid && Model_Valid &&/* PT_Serial_Valid &&*/
                (String.IsNullOrEmpty(Selected_Code) == false || HasNoErrorCode == true || PTCannotStart == true))
            {
                return true;
            }

            return false;
        }

        private void CommandUpdateWorkOrder()
        {
            eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
            Application_Helper.DoEvents();

            try
            {
                logger.Trace("Entered");
                Login_Context lc = (Login_Context)container.Resolve<Login_Context_Interface>();
                InfrastructureModule.Current_Work_Order.Date_Time_Updated = DateTime.Now;
                InfrastructureModule.Current_Work_Order.IsWarranty = IsWarranty;
                InfrastructureModule.Current_Work_Order.Customer_Complaint = Customer_Complaint;
                InfrastructureModule.Current_Work_Order.Unit_Condition = Unit_Condition;
                if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Technician_Name_1)) InfrastructureModule.Current_Work_Order.Technician_Name_1 = lc.UserName;
                else InfrastructureModule.Current_Work_Order.Technician_Name_2 = lc.UserName;
                InfrastructureModule.Current_Work_Order.Error_Code = Selected_Code;
                if (Is_I2) InfrastructureModule.Current_Work_Order.PT_Model = "i2";
                if (Is_X2) InfrastructureModule.Current_Work_Order.PT_Model = "x2";
                InfrastructureModule.Current_Work_Order.Error_Code_None = HasNoErrorCode == true ? 1 : 0;
                InfrastructureModule.Current_Work_Order.Error_Code_NO_Start = PTCannotStart == true ? 1 : 0;

                SqlBooleanList criteria = new SqlBooleanList();
                criteria.Add(new FieldData("Work_Order_ID", Selected_Work_Order.Work_Order_ID));
                criteria.Add(new FieldData("User_Name", loginContext.UserName));
                criteria.Add(new FieldData("Status", (int)DisclaimerStatuses.Accepted));
                List<SART_Disclaimer> sdList = SART_2012_Web_Service_Client.Select_SART_Disclaimer_Criteria(criteria);
                if ((sdList == null) || (sdList.Count == 0))
                {
                    Open_WorkOrder(InfrastructureModule.WorkOrder_OpenMode);
                    regionManager.RequestNavigate(RegionNames.MainRegion, "Disclaimer_Control");
                    return;
                }
                logger.Debug("Found accepted disclaimer: {0}", criteria);
                Open_WorkOrder(InfrastructureModule.WorkOrder_OpenMode);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                Application_Helper.DoEvents();
                logger.Trace("Leaving");
            }
        }

        /////////////////////////////////////////////
        #endregion




        #region ClearCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearCommand</summary>
        public DelegateCommand ClearCommand { get; set; }
        private Boolean CanCommandClear() { return true; }
        private void CommandClear()
        {
            PTSerialNumber = null;
            WorkOrderNumber = null;
            SelectedStatusString = null;
            UserName = null;
            StartDate = null;
            EndDate = null;
            Group_Name = null;
        }

        /////////////////////////////////////////////
        #endregion


        #endregion

        #region Event Handlers

        private void Close_On_Disclaimer_Reject(String workorder)
        {
            if (Selected_Work_Order != null)
            {
                InfrastructureModule.Current_Work_Order = null;
                SART_Disclaimer notice = new SART_Disclaimer(LoginContext.UserName, Selected_Work_Order.Work_Order_ID, DisclaimerStatuses.Rejected);
                logger.Debug("Inserting disclaimer object: {0}", notice);
                notice = SART_2012_Web_Service_Client.Insert_SART_Disclaimer_Key(notice);
                if (notice == null)
                {
                    Aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to record rejection of disclaimer.");
                }
                else
                {
                    Aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Recorded rejection of disclaimer.");
                }
                regionManager.RequestNavigate(RegionNames.MainRegion, "WorkOrderView");
            }
        }

        private void Open_On_Disclaimer_Accept(String workorder)
        {
            if (Selected_Work_Order == null)
            {
                regionManager.RequestNavigate(RegionNames.MainRegion, "WorkOrderView");
                return;
            }

            SART_Disclaimer notice = new SART_Disclaimer(LoginContext.UserName, Selected_Work_Order.Work_Order_ID, DisclaimerStatuses.Accepted);
            logger.Debug("Inserting disclaimer object: {0}", notice);
            notice = SART_2012_Web_Service_Client.Insert_SART_Disclaimer_Key(notice);
            if (notice == null)
            {
                Aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to record acceptance of disclaimer.");
            }
            else
            {
                Aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Recorded acceptance of disclaimer.");
            }

            Open_WorkOrder(InfrastructureModule.WorkOrder_OpenMode);
        }



        //private void Updated_DealerList(Boolean clear)
        //{
        //    InfrastructureModule.Clear_DealerList();
        //    DealerList = null;
        //}

        private void Clear_WO_List(Boolean clear)
        {
            WorkOrderList.Clear();
            Work_Order_Count = "0";
        }

        private void ApplicationLogout(Boolean close)
        {
            if (close)
            {
                logger.Debug("Work Order VM Received Application_Logout event");
                UserName = null;
                PTSerialNumber = null;
                WorkOrderNumber = null;
                LoadCounter = 0;
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region AutoSave_Handler  -- WorkOrder_AutoSave_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void AutoSave_Handler(Boolean on)
        {
            if (on)
            {
                if (autoSave != null) AutoSave_Handler(false);

                autoSave = new DispatcherTimer(DispatcherPriority.Background); //, Save_WorkOrder, null);
                autoSave.Interval = new TimeSpan(0, 0, 15);// 1 minute
                autoSave.Tick += new EventHandler(AutoSave_WorkOrder);
                autoSave.Start();
            }
            else if (autoSave != null)
            {
                autoSave.Stop();
                autoSave = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Delete_AutoSave_Handler  -- WorkOrder_AutoSave_Delete_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Delete_AutoSave_Handler(Boolean delete)
        {
            if (delete == true)
            {
                Delete_AutoSave();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion

        #region Miscellaneous Private Methods

        private void AutoSave_WorkOrder(object sender, EventArgs e)
        {
            if (InfrastructureModule.Current_Work_Order == null) return;
            String path = Path.Combine(Application_Helper.Application_Folder_Name(), AutoSaveFolder, String.Format("{0}.xml", InfrastructureModule.Current_Work_Order.Work_Order_ID));
            FileInfo autoSave = new FileInfo(path);
            if (autoSave.Directory.Exists == false) autoSave.Directory.Create();
            else if (autoSave.Exists) autoSave.Delete();
            Serialization.SerializeToFile<SART_Work_Order>(InfrastructureModule.Current_Work_Order, autoSave.FullName);
        }

        private void Delete_AutoSave()
        {
            if (InfrastructureModule.Current_Work_Order == null) return;
            String path = Path.Combine(Application_Helper.Application_Folder_Name(), AutoSaveFolder, String.Format("{0}.xml", InfrastructureModule.Current_Work_Order.Work_Order_ID));
            FileInfo autoSave = new FileInfo(path);
            if (autoSave.Directory.Exists == false) autoSave.Directory.Create();
            else if (autoSave.Exists) autoSave.Delete();
        }

        private Boolean Test_For_AutoSave(String woid)
        {
            String path = Path.Combine(Application_Helper.Application_Folder_Name(), AutoSaveFolder, String.Format("{0}.xml", woid));
            FileInfo autoSave = new FileInfo(path);
            if (autoSave.Directory.Exists == false)
            {
                autoSave.Directory.Create();
                return false;
            }
            else if (autoSave.Exists == false)
            {
                return false;
            }
            return true;
        }

        private SART_Work_Order Read_AutoSave(String woid)
        {
            String path = Path.Combine(Application_Helper.Application_Folder_Name(), AutoSaveFolder, String.Format("{0}.xml", woid));
            FileInfo autoSave = new FileInfo(path);
            if (autoSave.Directory.Exists == false)
            {
                autoSave.Directory.Create();
                return null;
            }
            else if (autoSave.Exists == false)
            {
                return null;
            }
            return Serialization.DeserializeFromFile<SART_Work_Order>(autoSave.FullName);
        }

        public SqlBooleanList Create_Criteria()
        {
            SqlBooleanList criteria = new SqlBooleanList();

            if (String.IsNullOrEmpty(PTSerialNumber) == false)
            {
                criteria.Add(new FieldData("PT_Serial", PTSerialNumber, FieldCompareOperator.Contains));
            }

            if (String.IsNullOrEmpty(WorkOrderNumber) == false)
            {
                criteria.Add(new FieldData("Work_Order_ID", WorkOrderNumber, SegwayFieldTypes.StringInsensitive, FieldCompareOperator.Contains));
            }

            if (String.IsNullOrEmpty(UserName) == false)
            {
                criteria.Add(new FieldData("Technician", UserName, SegwayFieldTypes.StringInsensitive, FieldCompareOperator.Contains));
            }

            if ((StartDate != null) && (StartDate.HasValue == true))
            {
                criteria.Add(new FieldData("Create_Date", StartDate.Value, FieldCompareOperator.GreaterThanOrEqual));
            }

            if ((EndDate != null) && (EndDate.HasValue == true))
            {
                criteria.Add(new FieldData("Create_Date", EndDate.Value.AddDays(1), FieldCompareOperator.LessThan));
            }

            if (LoginContext.User_Level != UserLevels.Expert)
            {
                criteria.Add(new FieldData("Customer_Number", LoginContext.Group_ID.ToString().PadLeft(7)));
            }
            else if (String.IsNullOrEmpty(Group_Name) == false)
            {
                if (DealerInfo.Accounts.ContainsKey(Group_Name) == true)
                {
                    criteria.Add(new FieldData("Customer_Number", DealerInfo.Accounts[Group_Name]));
                }
            }

            if (String.IsNullOrEmpty(SelectedStatusString) == false)
            {
                //foreach (String sts in Status_Codes.Keys)
                //{
                //    if (Status_Codes[sts] == SelectedStatusString)
                //    {
                //        //WorkOrderStatuses status = (WorkOrderStatuses)Enum.Parse(typeof(WorkOrderStatuses), SelectedStatusString.Replace(" ", "_"));
                //        criteria.Add(new FieldData("Status", sts));
                //        break;
                //    }
                //}

                if (Status_Codes.ContainsValue(SelectedStatusString))
                {
                    foreach (String key in Status_Codes.Keys)
                    {
                        if (Status_Codes[key] == SelectedStatusString)
                        {
                            criteria.Add(new FieldData("Status", key)); //Status_Codes[SelectedStatusString]));
                            break;
                        }
                    }
                }
            }

            return criteria;
        }


        private void CreateDefaultFilterSettings()
        {
            StartDate = DateTime.Today.AddMonths(-1);

            // only expert level users should see the Organization filter textbox
            if (loginContext.User_Level == UserLevels.Expert)
            {
                UserName = loginContext.UserName;
                GroupVisible = true;
                GroupOpacity = 0.65;
            }
            else
            {
                // not setting UserName for filter so user can initially see all work orders in the organization
                GroupVisible = false;
                GroupOpacity = 0.00;
                //GroupID = loginContext.Group_ID;
            }
            LoadCounter++;   // to prevent overwriting filter settings every time we switch views

            return;
        }


        private void Open_Work_Order(Open_Mode openMode)
        {
            Aggregator.GetEvent<StatusBar_Region1_Event>().Publish("");
            Aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
            Application_Helper.DoEvents();
            try
            {
                SART_Work_Order wo = SART_2012_Web_Service_Client.Select_SART_Work_Order_WORK_ORDER_ID(Selected_Work_Order.Work_Order_ID);
                if (openMode == Open_Mode.Read_Write)
                {
                    if (String.IsNullOrEmpty(wo.Opened_By) == false) //Work_Order_WorkingStatuses.Opened.ToString())
                    {
                        Aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                        Aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Work Order: {0} is already opened by: {1}", Selected_Work_Order.Work_Order_ID, wo.Opened_By));
                        return;
                    }

                    if (Test_For_AutoSave(wo.Work_Order_ID) == true)
                    {
                        InfrastructureModule.Current_Work_Order = Read_AutoSave(wo.Work_Order_ID);
                        if (InfrastructureModule.Current_Work_Order == null) InfrastructureModule.Current_Work_Order = wo;
                    }
                    else
                    {
                        InfrastructureModule.Current_Work_Order = wo;
                    }
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


                IsWarranty = Convert.ToBoolean(InfrastructureModule.Current_Work_Order.Warranty);
                Customer_Complaint = InfrastructureModule.Current_Work_Order.Customer_Complaint;
                Unit_Condition = InfrastructureModule.Current_Work_Order.Unit_Condition;


                ////////////////////////////////////////////////////////////////////////////////
                // Checking for Model
                try
                {
                    logger.Debug("Checking for Model type");
                    if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.PT_Model) == false)
                    {
                        Is_I2 = InfrastructureModule.Current_Work_Order.PT_Model.ToUpper() == "I2" ? true : false;
                        Is_X2 = InfrastructureModule.Current_Work_Order.PT_Model.ToUpper() == "X2" ? true : false;
                    }
                    else if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.PT_Part_Number) == false)
                    {
                        String pn = InfrastructureModule.Current_Work_Order.PT_Part_Number.Replace("-", "");
                        if (Flight_Code_Model.ContainsKey(pn) == true)
                        {
                            InfrastructureModule.Current_Work_Order.PT_Model = Flight_Code_Model[pn];
                        }
                        else
                        {
                            String msg = String.Format("There is no flight code model information available for part number: {0}.", pn);
                            String msg1 = String.Format("{0}  Please CANCEL out of this work order and contact IT to have this information entered.  It should be found under the \"User Defined\" tab of the Item form in Syteline.", msg);
                            PopupColor = Brushes.Pink;
                            PopupMessage = msg1;
                            PopupOpen = true;
                            logger.Warn(msg);
                            //return;
                        }
                    }
                    else
                    {
                        String msg = String.Format("There is no PT Model or PT Part Number associated to work order: {0} (PT: {1})", InfrastructureModule.Current_Work_Order.PT_Serial, InfrastructureModule.Current_Work_Order.PT_Part_Number);
                        String msg1 = String.Format("{0}  Please CANCEL out of this work order and have this information entered before trying to continue.  ", msg);
                        PopupColor = Brushes.Pink;
                        PopupMessage = msg1;
                        PopupOpen = true;
                        logger.Warn(msg);
                    }
                }
                catch (Exception ex)
                {
                    String msg = Exception_Helper.FormatExceptionString("An exception has occurred.  Please upload your log (Shft-F11) and contact Segway Technical Support for further assistance.", ex);
                    PopupColor = Brushes.Pink;
                    PopupMessage = msg;
                    PopupOpen = true;
                    logger.Error(msg);
                }
                finally
                {
                    logger.Debug("Leaving");
                }
                // Checking for Model
                ////////////////////////////////////////////////////////////////////////////////


                ////////////////////////////////////////////////////////////////////////////////
                // Checking for Error Code
                PTCannotStart = false;
                HasNoErrorCode = false;
                HasErrorCode = false;
                if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Error_Code) == false)
                {
                    if (InfoKey_Error_Codes.Contains(InfrastructureModule.Current_Work_Order.Error_Code))
                    {
                        Selected_Code = InfrastructureModule.Current_Work_Order.Error_Code;
                    }
                    HasErrorCode = true;
                }
                if (InfrastructureModule.Current_Work_Order.Error_Code_None.HasValue)
                {
                    if (InfrastructureModule.Current_Work_Order.Error_Code_None.Value != 0)
                    {
                        HasNoErrorCode = true;
                    }
                }
                if (InfrastructureModule.Current_Work_Order.Error_Code_NO_Start.HasValue == true)
                {
                    if (InfrastructureModule.Current_Work_Order.Error_Code_NO_Start.Value != 0)
                    {
                        PTCannotStart = true;
                    }
                }
                // Checking for Error Code
                ////////////////////////////////////////////////////////////////////////////////

                if (openMode == Open_Mode.Read_Only)
                {
                    Open_WorkOrder(openMode);
                }
                else if ((String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Unit_Condition) == false) &&
                    (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Customer_Complaint) == false) &&
                    (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.PT_Model) == false) &&
                    ((String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Error_Code) == false) ||
                    ((InfrastructureModule.Current_Work_Order.Error_Code_None != null) &&
                    (InfrastructureModule.Current_Work_Order.Error_Code_None.Value == 1)) ||
                    ((InfrastructureModule.Current_Work_Order.Error_Code_NO_Start != null) &&
                    (InfrastructureModule.Current_Work_Order.Error_Code_NO_Start.Value == 1))))
                {
                    // Navigate to WorkOrderSummaryView
                    SqlBooleanList criteria = new SqlBooleanList();
                    criteria.Add(new FieldData("Work_Order_ID", Selected_Work_Order.Work_Order_ID));
                    criteria.Add(new FieldData("User_Name", loginContext.UserName));
                    criteria.Add(new FieldData("Status", (int)DisclaimerStatuses.Accepted));
                    List<SART_Disclaimer> sdList = SART_2012_Web_Service_Client.Select_SART_Disclaimer_Criteria(criteria);
                    if ((sdList == null) || (sdList.Count == 0))
                    {
                        regionManager.RequestNavigate(RegionNames.MainRegion, Disclaimer_Control.Control_Name);
                        return;
                    }
                    logger.Debug("Found accepted disclaimer: {0}", criteria);
                    Open_WorkOrder(openMode);
                }
                else
                {
                    // Navigate to WorkOrderUpdateView
                    SelectedPanel = 0;
                }
            }
            catch (Exception e)
            {
                logger.Error(Exception_Helper.FormatExceptionString(e));
            }
            finally
            {
                Aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
            }
        }

        #endregion
    }
}
