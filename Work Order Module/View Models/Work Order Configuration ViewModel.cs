using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Login.Objects;
using Segway.Manufacturing.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.Modules.AddWindow;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using Segway.Syteline.Client.REST;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Segway.Modules.WorkOrder
{
    public partial class Work_Order_Configuration_ViewModel : ViewModelBase, Work_Order_Configuration_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator aggregator;


        public Work_Order_Configuration_ViewModel(Work_Order_Configuration_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.aggregator = eventAggregator;


            #region Event Subscriptions

            aggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);
            aggregator.GetEvent<WorkOrder_Opened_Event>().Subscribe(Store_Current_Work_Order, ThreadOption.BackgroundThread, true);
            aggregator.GetEvent<SART_EventLog_Add_Event>().Subscribe(Add_Event, ThreadOption.UIThread, true);
            aggregator.GetEvent<SART_EventLog_Update_Event>().Subscribe(Update_Event, ThreadOption.UIThread, true);
            aggregator.GetEvent<WO_Config_Clear_Event>().Subscribe(Clear_Event, ThreadOption.UIThread, true);
            aggregator.GetEvent<WO_Config_EventID_Event>().Subscribe(Retrieve_EventID, true);

            aggregator.GetEvent<SART_Configuration_CUA_Display_Event>().Subscribe(Display_CUA, ThreadOption.UIThread, true);
            aggregator.GetEvent<SART_Configuration_CUB_Display_Event>().Subscribe(Display_CUB, ThreadOption.UIThread, true);
            aggregator.GetEvent<SART_Configuration_BSAA_Display_Event>().Subscribe(Display_BSAA, ThreadOption.UIThread, true);
            aggregator.GetEvent<SART_Configuration_BSAB_Display_Event>().Subscribe(Display_BSAB, ThreadOption.UIThread, true);
            aggregator.GetEvent<SART_Configuration_UISID_Display_Event>().Subscribe(Display_UI_SID, ThreadOption.UIThread, true);
            aggregator.GetEvent<SART_Configuration_UICSerial_Display_Event>().Subscribe(Display_UIC_Serial, ThreadOption.UIThread, true);
            aggregator.GetEvent<WorkOrder_ConfigurationType_Event>().Subscribe(ConfigurationType_Handler, ThreadOption.BackgroundThread, true);
            aggregator.GetEvent<WorkOrder_Configuration_ClearLog_Event>().Subscribe(WorkOrder_Configuration_ClearLog_Handler, ThreadOption.UIThread, true);
            //eventAggregator.GetEvent<WorkOrder_Configuration_Return_Event>().Subscribe(WorkOrder_Configuration_Return_Handler, ThreadOption.UIThread, true);

            eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, ThreadOption.UIThread, true);

            #endregion

            #region Command Delegates

            ReturnCommand = new DelegateCommand(CommandReturn, CanCommandReturn);
            RetrieveCommand = new DelegateCommand(CommandRetrieve, CanCommandRetrieve);
            AuthorizeCommand = new DelegateCommand(CommandAuthorize, CanCommandAuthorize);
            CancelCommand = new DelegateCommand(CommandCancel, CanCommandCancel);
            ClearCommand = new DelegateCommand(CommandClear, CanCommandClear);
            ClearCUACommand = new DelegateCommand(CommandClearCUA, CanCommandClearCUA);
            ClearCUBCommand = new DelegateCommand(CommandClearCUB, CanCommandClearCUB);
            ClearBSAACommand = new DelegateCommand(CommandClearBSAA, CanCommandClearBSAA);
            ClearBSABCommand = new DelegateCommand(CommandClearBSAB, CanCommandClearBSAB);
            ClearUISIDCommand = new DelegateCommand(CommandClearUISID, CanCommandClearUISID);
            ClearPivotCommand = new DelegateCommand(CommandClearPivot, CanCommandClearPivot);
            ClearMotorLCommand = new DelegateCommand(CommandClearMotorL, CanCommandClearMotorL);
            ClearMotorRCommand = new DelegateCommand(CommandClearMotorR, CanCommandClearMotorR);
            ClearUICSerialCommand = new DelegateCommand(CommandClearUICSerial, CanCommandClearUICSerial);
            YesCommand = new DelegateCommand(CommandYes, CanCommandYes);
            NoCommand = new DelegateCommand(CommandNo, CanCommandNo);

            BlankCUACommand = new DelegateCommand(CommandBlankCUA, CanCommandBlankCUA);
            BlankCUBCommand = new DelegateCommand(CommandBlankCUB, CanCommandBlankCUB);
            BlankBSAACommand = new DelegateCommand(CommandBlankBSAA, CanCommandBlankBSAA);
            BlankBSABCommand = new DelegateCommand(CommandBlankBSAB, CanCommandBlankBSAB);
            BlankUISIDCommand = new DelegateCommand(CommandBlankUISID, CanCommandBlankUISID);

            OpenPopupCommand = new DelegateCommand(CommandOpenPopup, CanCommandOpenPopup);

            #endregion
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        #region PT_Config

        private SART_PT_Configuration _PT_Config;

        /// <summary>Property PT_Config of type SART_PT_Configuration</summary>
        public SART_PT_Configuration PT_Config
        {
            get
            {
                if (_PT_Config == null) { _PT_Config = new SART_PT_Configuration(); }
                return _PT_Config;
            }
            set { _PT_Config = value; }
        }

        #endregion

        public SART_PT_Configuration StartConfig { get; set; }
        public SART_PT_Configuration FinalConfig { get; set; }

        private int EventID { get; set; }

        private ConfigurationTypes Config_Type { get; set; }

        #region SelectedLogEntry

        private SART_Event_Log_Entry _SelectedLogEntry;

        /// <summary>Property SelectedLogEntry of type SART_Event_Log_Entry</summary>
        public SART_Event_Log_Entry SelectedLogEntry
        {
            get { return _SelectedLogEntry; }
            set
            {
                _SelectedLogEntry = value;
                //OnPropertyChanged("SelectedLogEntry");
            }
        }

        #endregion


        #region Part_Number_XRefs

        private Dictionary<String, Segway_Part_Number_Production_Xref> _Part_Number_XRefs;

        /// <summary>Property Part_Number_XRefs of type Dictionary<String, Segway_Part_Number_Production_Xref></summary>
        public Dictionary<String, Segway_Part_Number_Production_Xref> Part_Number_XRefs
        {
            get
            {
                if (_Part_Number_XRefs == null)
                {
                    var xrefs = Manufacturing_SPNPX_Web_Service_Client_REST.Select_Segway_Part_Number_Production_Xref_All(InfrastructureModule.Token);
                    if ((xrefs == null) || (xrefs.Count == 0)) return null;
                    _Part_Number_XRefs = new Dictionary<String, Segway_Part_Number_Production_Xref>();
                    foreach (var xref in xrefs)
                    {
                        _Part_Number_XRefs[xref.Part_Number] = xref;
                    }
                }
                return _Part_Number_XRefs;
            }
            //set
            //{
            //    _Part_Number_XRefs = value;
            //}
        }

        #endregion


        #region Part_Number_Info

        private Dictionary<String, Segway_Part_Number_Information> _Part_Number_Info;

        /// <summary>Property Part_Number_Info of type Dictionary<String,Segway_Part_Number_Information></summary>
        public Dictionary<String, Segway_Part_Number_Information> Part_Number_Info
        {
            get
            {
                if (_Part_Number_Info == null)
                {
                    var pninfos = Manufacturing_SPNI_Web_Service_Client_REST.Select_Segway_Part_Number_Information_All(InfrastructureModule.Token);
                    if ((pninfos == null) || (pninfos.Count == 0)) return null;
                    _Part_Number_Info = new Dictionary<String, Segway_Part_Number_Information>();
                    foreach (var pninfo in pninfos)
                    {
                        _Part_Number_Info[pninfo.Part_Number] = pninfo;
                    }

                }
                return _Part_Number_Info;
            }
            //set
            //{
            //    _Part_Number_Info = value;
            //}
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

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        #region Work Order Info Bar Data

        #region Work_Order_Num

        /// <summary>Property Work_Order_Num of type String</summary>
        public String Work_Order_Num
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Work_Order_ID;
            }
            set { OnPropertyChanged("Work_Order_Num"); }
        }

        #endregion

        #region PTSerial

        /// <summary>Property PTSerial of type String</summary>
        public String PTSerial
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.PT_Serial;
            }
            set { OnPropertyChanged("PTSerial"); }
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

        #endregion

        #region ExtractionInProgress

        private Boolean _ExtractionInProgress;

        /// <summary>Property Is_CUB of type Boolean</summary>
        public Boolean ExtractionInProgress
        {
            get { return _ExtractionInProgress; }
            set
            {
                _ExtractionInProgress = value;
                OnPropertyChanged("ExtractionInProgress");
                AuthorizeCommand.RaiseCanExecuteChanged();
                ReturnCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
                RetrieveCommand.RaiseCanExecuteChanged();
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
                    _Header_Image = Image_Helper.ImageFromEmbedded(".Images.Configuration2.png");
                }
                return _Header_Image;
            }
            set
            {
                OnPropertyChanged("Header_Image");
            }
        }

        #endregion

        #region User_Info

        private Login_Context _User_Info = null;

        /// <summary>Property User_Info of type Login_Context</summary>
        public Login_Context User_Info
        {
            get
            {
                if (container.IsRegistered<AuthenticationToken_Interface>(AuthenticationToken.ApplicationGlobalInstanceName) == true)
                {
                    AuthenticationToken at = container.Resolve<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName);
                    _User_Info = at.LoginContext;
                }
                return _User_Info;
            }
            set { _User_Info = value; }
        }

        #endregion

        #region ProgressLog

        private ObservableCollection<SART_Event_Log_Entry> _ProgressLog;

        /// <summary>Property ProgressLog of type List<SART.Objects.SART_Event_Log_Entry</summary>
        public ObservableCollection<SART_Event_Log_Entry> ProgressLog
        {
            get
            {
                if (_ProgressLog == null) _ProgressLog = new ObservableCollection<SART_Event_Log_Entry>();
                return _ProgressLog;
            }
            set
            {
                _ProgressLog = value;
                OnPropertyChanged("ProgressLog");
                if (_ProgressLog != null)
                {
                    if (_ProgressLog.Count > 0)
                    {
                        SelectedLogEntry = _ProgressLog[_ProgressLog.Count - 1];
                    }
                }
            }
        }

        #endregion

        #region CUA_Serial

        /// <summary>Property CUA_Serial of type String</summary>
        public String CUA_Serial
        {
            get
            {
                if (PT_Config.CUA_Serial == String.Empty) return "<Blank>";
                return PT_Config.CUA_Serial;
            }
            set
            {
                PT_Config.CUA_Serial = value;
                OnPropertyChanged("CUA_Serial");
                AuthorizeCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region CUB_Serial

        /// <summary>Property CUB_Serial of type String</summary>
        public String CUB_Serial
        {
            get
            {
                if (PT_Config.CUB_Serial == String.Empty) return "<Blank>";
                return PT_Config.CUB_Serial;
            }
            set
            {
                PT_Config.CUB_Serial = value;
                OnPropertyChanged("CUB_Serial");
                AuthorizeCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region BSA_A_Serial

        /// <summary>Property BSA_A_Serial of type String</summary>
        public String BSA_A_Serial
        {
            get
            {
                if (PT_Config.BSA_A_Serial == String.Empty) return "<Blank>";
                return PT_Config.BSA_A_Serial;
            }
            set
            {
                PT_Config.BSA_A_Serial = value;
                OnPropertyChanged("BSA_A_Serial");
                AuthorizeCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region BSA_B_Serial

        /// <summary>Property BSA_B_Serial of type String</summary>
        public String BSA_B_Serial
        {
            get
            {
                if (PT_Config.BSA_B_Serial == String.Empty) return "<Blank>";
                return PT_Config.BSA_B_Serial;
            }
            set
            {
                PT_Config.BSA_B_Serial = value;
                OnPropertyChanged("BSA_B_Serial");
                AuthorizeCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region UIC_Serial

        /// <summary>Property UIC_Serial of type String</summary>
        public String UIC_Serial
        {
            get { return PT_Config.UIC_Serial; }
            set
            {
                PT_Config.UIC_Serial = value;
                OnPropertyChanged("UIC_Serial");
                AuthorizeCommand.RaiseCanExecuteChanged();
                if ((PT_Config.UIC_Serial != null) && (PT_Config.UIC_Serial.Length >= 8))
                {
                    IsPivotSerialFocused = false;
                    IsPivotSerialFocused = true;
                }
            }
        }

        #endregion

        #region UIC_SID

        /// <summary>Property UIC_SID of type String</summary>
        public String UIC_SID
        {
            get
            {
                if (PT_Config.UIC_SID == String.Empty) return "<Blank>";
                return PT_Config.UIC_SID;
            }
            set
            {
                PT_Config.UIC_SID = value;
                OnPropertyChanged("UIC_SID");
            }
        }

        #endregion

        #region Pivot_Serial

        /// <summary>Property Pivot_Serial of type String</summary>
        public String Pivot_Serial
        {
            get { return PT_Config.Pivot_Serial; }
            set
            {
                PT_Config.Pivot_Serial = value;
                OnPropertyChanged("Pivot_Serial");
                AuthorizeCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Pivot_Serial

        private Boolean _isPivotSerialFocused = false;
        /// <summary>Property Pivot_Serial of type String</summary>
        public Boolean IsPivotSerialFocused
        {
            get { return _isPivotSerialFocused; }
            set
            {
                _isPivotSerialFocused = value;
                OnPropertyChanged("IsPivotSerialFocused");
            }
        }

        #endregion

        #region MotorLeft_Serial

        /// <summary>Property MotorLeft_Serial of type String</summary>
        public String MotorLeft_Serial
        {
            get { return PT_Config.Motorl_Serial; }
            set
            {
                PT_Config.Motorl_Serial = value;
                OnPropertyChanged("MotorLeft_Serial");
            }
        }

        #endregion

        #region MotorRight_Serial

        /// <summary>Property MotorRight_Serial of type String</summary>
        public String MotorRight_Serial
        {
            get { return PT_Config.Motorr_Serial; }
            set
            {
                PT_Config.Motorr_Serial = value;
                OnPropertyChanged("MotorRight_Serial");
            }
        }

        #endregion

        #region MotorLeft_Visibility

        private Visibility _MotorLeft_Visibility = Visibility.Collapsed;

        /// <summary>Property MotorLeft_Visibility of type Visibility</summary>
        public Visibility MotorLeft_Visibility
        {
            get { return _MotorLeft_Visibility; }
            set
            {
                _MotorLeft_Visibility = value;
                OnPropertyChanged("MotorLeft_Visibility");
            }
        }

        #endregion

        #region MotorRight_Visibility

        private Visibility _MotorRight_Visibility = Visibility.Collapsed;

        /// <summary>Property MotorRight_Visibility of type Visibility</summary>
        public Visibility MotorRight_Visibility
        {
            get
            {
                if (_MotorRight_Visibility == Visibility.Collapsed)
                {
                    ((Work_Order_Configuration_Control)View).RightMotorRow.Height = GridLength.Auto;
                }
                else
                {
                    ((Work_Order_Configuration_Control)View).RightMotorRow.Height = new GridLength(35);
                }

                return _MotorRight_Visibility;
            }
            set
            {
                _MotorRight_Visibility = value;
                OnPropertyChanged("MotorRight_Visibility");
            }
        }

        #endregion

        #region CancelVisibility

        private Visibility _CancelVisibility = Visibility.Collapsed;

        /// <summary>Property CancelVisibility of type Visibility</summary>
        public Visibility CancelVisibility
        {
            get { return _CancelVisibility; }
            set
            {
                _CancelVisibility = value;
                OnPropertyChanged("CancelVisibility");
            }
        }

        #endregion

        #region RetrieveVisibility

        private Visibility _RetrieveVisibility = Visibility.Visible;

        /// <summary>Property RetrieveVisibility of type Visibility</summary>
        public Visibility RetrieveVisibility
        {
            get { return _RetrieveVisibility; }
            set
            {
                _RetrieveVisibility = value;
                OnPropertyChanged("RetrieveVisibility");
            }
        }

        #endregion

        #region ConfigurationOverridePopupOpen

        private Boolean _ConfigurationOverridePopupOpen = false;

        /// <summary>Property ConfigurationOverridePopupOpen of type Boolean</summary>
        public Boolean ConfigurationOverridePopupOpen
        {
            get { return _ConfigurationOverridePopupOpen; }
            set
            {
                _ConfigurationOverridePopupOpen = value;
                OnPropertyChanged("ConfigurationOverridePopupOpen");
            }
        }

        #endregion

        #region ConfigurationOverrideMessage

        private String _ConfigurationOverrideMessage;

        /// <summary>Property ConfigurationOverrideMessage of type Boolean</summary>
        public String ConfigurationOverrideMessage
        {
            get { return _ConfigurationOverrideMessage; }
            set
            {
                _ConfigurationOverrideMessage = value;
                OnPropertyChanged("ConfigurationOverrideMessage");
            }
        }

        #endregion

        #region ContinueOnError

        private Boolean? _ContinueOnError = null;

        /// <summary>Property ContinueOnError of type Boolean</summary>
        public Boolean? ContinueOnError
        {
            get { return _ContinueOnError; }
            set
            {
                _ContinueOnError = value;
            }
        }

        #endregion


        #region UIC_Serial_Visibility

        private Visibility _UIC_Serial_Visibility = Visibility.Visible;

        /// <summary>Property UIC_Serial_Background of type SolidColorBrush</summary>
        public Visibility UIC_Serial_Visibility
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order != null)
                {
                    if (Is_PT_SE(InfrastructureModule.Current_Work_Order.PT_Part_Number) == true)
                    {
                        return Visibility.Collapsed;
                    }
                }
                return _UIC_Serial_Visibility;
            }
            set
            {
                OnPropertyChanged("UIC_Serial_Visibility");
            }
        }

        #endregion

        ///////////////////////////////////////
        #region Popups

        #region CUA_Serial_Popup_Open

        private Boolean _CUA_Serial_Popup_Open = false;

        /// <summary>Property CUA_Serial_Popup_Open of type Boolean</summary>
        public Boolean CUA_Serial_Popup_Open
        {
            get { return _CUA_Serial_Popup_Open; }
            set
            {
                _CUA_Serial_Popup_Open = value;
                OnPropertyChanged("CUA_Serial_Popup_Open");
            }
        }

        #endregion

        #region CUB_Serial_Popup_Open

        private Boolean _CUB_Serial_Popup_Open = false;

        /// <summary>Property CUB_Serial_Popup_Open of type Boolean</summary>
        public Boolean CUB_Serial_Popup_Open
        {
            get { return _CUB_Serial_Popup_Open; }
            set
            {
                _CUB_Serial_Popup_Open = value;
                OnPropertyChanged("CUB_Serial_Popup_Open");
            }
        }

        #endregion

        #region BSA_A_Serial_Popup_Open

        private Boolean _BSA_A_Serial_Popup_Open = false;

        /// <summary>Property BSA_A_Serial_Popup_Open of type Boolean</summary>
        public Boolean BSA_A_Serial_Popup_Open
        {
            get { return _BSA_A_Serial_Popup_Open; }
            set
            {
                _BSA_A_Serial_Popup_Open = value;
                OnPropertyChanged("BSA_A_Serial_Popup_Open");
            }
        }

        #endregion

        #region BSA_B_Serial_Popup_Open

        private Boolean _BSA_B_Serial_Popup_Open = false;

        /// <summary>Property BSA_B_Serial_Popup_Open of type Boolean</summary>
        public Boolean BSA_B_Serial_Popup_Open
        {
            get { return _BSA_B_Serial_Popup_Open; }
            set
            {
                _BSA_B_Serial_Popup_Open = value;
                OnPropertyChanged("BSA_B_Serial_Popup_Open");
            }
        }

        #endregion

        #region UIC_SID_Popup_Open

        private Boolean _UIC_SID_Popup_Open = false;

        /// <summary>Property UIC_SID_Popup_Open of type Boolean</summary>
        public Boolean UIC_SID_Popup_Open
        {
            get { return _UIC_SID_Popup_Open; }
            set
            {
                _UIC_SID_Popup_Open = value;
                OnPropertyChanged("UIC_SID_Popup_Open");
            }
        }

        #endregion

        #region UIC_Serial_Popup_Open

        private Boolean _UIC_Serial_Popup_Open;

        /// <summary>Property UIC_Serial_Popup_Open of type Boolean</summary>
        public Boolean UIC_Serial_Popup_Open
        {
            get { return _UIC_Serial_Popup_Open; }
            set
            {
                _UIC_Serial_Popup_Open = value;
                OnPropertyChanged("UIC_Serial_Popup_Open");
            }
        }

        #endregion

        #region Pivot_Serial_Popup_Open

        private Boolean _Pivot_Serial_Popup_Open = false;

        /// <summary>Property Pivot_Serial_Popup_Open of type Boolean</summary>
        public Boolean Pivot_Serial_Popup_Open
        {
            get { return _Pivot_Serial_Popup_Open; }
            set
            {
                _Pivot_Serial_Popup_Open = value;
                OnPropertyChanged("Pivot_Serial_Popup_Open");
            }
        }

        #endregion

        #region MotorLeft_Serial_Popup_Open

        private Boolean _MotorLeft_Serial_Popup_Open = false;

        /// <summary>Property MotorLeft_Serial_Popup_Open of type Boolean</summary>
        public Boolean MotorLeft_Serial_Popup_Open
        {
            get { return _MotorLeft_Serial_Popup_Open; }
            set
            {
                _MotorLeft_Serial_Popup_Open = value;
                OnPropertyChanged("MotorLeft_Serial_Popup_Open");
            }
        }

        #endregion

        #region MotorRight_Serial_Popup_Open

        private Boolean _MotorRight_Serial_Popup_Open = false;

        /// <summary>Property MotorRight_Serial_Popup_Open of type Boolean</summary>
        public Boolean MotorRight_Serial_Popup_Open
        {
            get { return _MotorRight_Serial_Popup_Open; }
            set
            {
                _MotorRight_Serial_Popup_Open = value;
                OnPropertyChanged("MotorRight_Serial_Popup_Open");
            }
        }

        #endregion


        #region PopupMessage

        private String _PopupMessage;

        /// <summary>Property PopupMessage of type String</summary>
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

        #region PopupIsOpen

        private Boolean _PopupIsOpen;

        /// <summary>Property PopupIsOpen of type Boolean</summary>
        public Boolean PopupIsOpen
        {
            get { return _PopupIsOpen; }
            set
            {
                _PopupIsOpen = value;
                OnPropertyChanged("PopupIsOpen");
            }
        }

        #endregion

        #region PopupColor

        private Brush _PopupColor = System.Windows.Media.Brushes.Pink;

        /// <summary>Property PopupColor of type Brush</summary>
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
        ///////////////////////////////////////


        ///////////////////////////////////////
        #region Borders

        #region MotorRight_Serial_Border

        /// <summary>Property MotorRight_Serial_Border of type Brush</summary>
        public Brush MotorRight_Serial_Border
        {
            get
            {
                try
                {
                    if (MotorRight_Serial_Error == true) return System.Windows.Media.Brushes.Red;
                    return (LinearGradientBrush)System.Windows.Application.Current.FindResource("TextBoxBorderBrush");
                }
                catch (Exception e)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(e));
                    return null;
                }
            }
            set { OnPropertyChanged("MotorRight_Serial_Border"); }
        }

        #endregion

        #region MotorLeft_Serial_Border

        /// <summary>Property MotorLeft_Serial_Border of type Brush</summary>
        public Brush MotorLeft_Serial_Border
        {
            get
            {
                try
                {
                    if (MotorLeft_Serial_Error == true) return System.Windows.Media.Brushes.Red;
                    return (LinearGradientBrush)System.Windows.Application.Current.FindResource("TextBoxBorderBrush");
                }
                catch (Exception e)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(e));
                    return null;
                }
            }
            set { OnPropertyChanged("MotorLeft_Serial_Border"); }
        }

        #endregion

        #region Pivot_Serial_Border

        /// <summary>Property Pivot_Serial_Border of type Brush</summary>
        public Brush Pivot_Serial_Border
        {
            get
            {
                try
                {
                    if (Pivot_Serial_Error == true) return System.Windows.Media.Brushes.Red;
                    return (LinearGradientBrush)System.Windows.Application.Current.FindResource("TextBoxBorderBrush");
                }
                catch (Exception e)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(e));
                    return null;
                }
            }
            set { OnPropertyChanged("Pivot_Serial_Border"); }
        }

        #endregion

        #region UIC_Serial_Border

        /// <summary>Property UIC_Serial_Border of type Brush</summary>
        public Brush UIC_Serial_Border
        {
            get
            {
                try
                {
                    if (UIC_Serial_Error == true) return System.Windows.Media.Brushes.Red;
                    return (LinearGradientBrush)System.Windows.Application.Current.FindResource("TextBoxBorderBrush");
                }
                catch (Exception e)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(e));
                    return null;
                }
            }
            set { OnPropertyChanged("UIC_Serial_Border"); }
        }

        #endregion

        #region UIC_SID_Border

        /// <summary>Property UIC_SID_Border of type Brush</summary>
        public Brush UIC_SID_Border
        {
            get
            {
                try
                {
                    if (UIC_SID_Error == true) return System.Windows.Media.Brushes.Red;
                    return (LinearGradientBrush)System.Windows.Application.Current.FindResource("TextBoxBorderBrush");
                }
                catch (Exception e)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(e));
                    return null;
                }
            }
            set { OnPropertyChanged("UIC_SID_Border"); }
        }

        #endregion

        #region CUA_Serial_Border

        /// <summary>Property CUA_Serial_Border of type Brush</summary>
        public Brush CUA_Serial_Border
        {
            get
            {
                try
                {
                    if (CUA_Serial_Error == true) return System.Windows.Media.Brushes.Red;
                    return (LinearGradientBrush)System.Windows.Application.Current.FindResource("TextBoxBorderBrush");
                }
                catch (Exception e)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(e));
                    return null;
                }
            }
            set { OnPropertyChanged("CUA_Serial_Border"); }
        }

        #endregion

        #region CUB_Serial_Border

        /// <summary>Property CUB_Serial_Border of type Brush</summary>
        public Brush CUB_Serial_Border
        {
            get
            {
                try
                {
                    if (CUB_Serial_Error == true) return System.Windows.Media.Brushes.Red;
                    return (LinearGradientBrush)System.Windows.Application.Current.FindResource("TextBoxBorderBrush");
                }
                catch (Exception e)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(e));
                    return null;
                }
            }
            set { OnPropertyChanged("CUB_Serial_Border"); }
        }

        #endregion

        #region BSA_B_Serial_Border

        /// <summary>Property BSA_B_Serial_Border of type Brush</summary>
        public Brush BSA_B_Serial_Border
        {
            get
            {
                try
                {
                    if (BSA_B_Serial_Error == true) return System.Windows.Media.Brushes.Red;
                    return (LinearGradientBrush)System.Windows.Application.Current.FindResource("TextBoxBorderBrush");
                }
                catch (Exception e)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(e));
                    return null;
                }
            }
            set { OnPropertyChanged("BSA_B_Serial_Border"); }
        }

        #endregion

        #region BSA_A_Serial_Border

        /// <summary>Property BSA_B_Serial_Border of type Brush</summary>
        public Brush BSA_A_Serial_Border
        {
            get
            {
                try
                {
                    if (BSA_A_Serial_Error == true) return System.Windows.Media.Brushes.Red;
                    return (LinearGradientBrush)System.Windows.Application.Current.FindResource("TextBoxBorderBrush");
                }
                catch (Exception e)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(e));
                    return null;
                }
            }
            set { OnPropertyChanged("BSA_A_Serial_Border"); }
        }

        #endregion

        #endregion
        ///////////////////////////////////////


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////
        #region Error Flags

        #region CUA_Serial_Error

        private Boolean _CUA_Serial_Error;

        /// <summary>Property CUA_Serial_Error of type Boolean</summary>
        public Boolean CUA_Serial_Error
        {
            get { return _CUA_Serial_Error; }
            set
            {
                _CUA_Serial_Error = value;
                OnPropertyChanged("CUA_Serial_Border");
                CUA_Serial_Popup_Open = value;
                OnPropertyChanged("CUA_Serial_Popup_Open");
            }
        }

        #endregion

        #region CUB_Serial_Error

        private Boolean _CUB_Serial_Error;

        /// <summary>Property CUB_Serial_Error of type Boolean</summary>
        public Boolean CUB_Serial_Error
        {
            get { return _CUB_Serial_Error; }
            set
            {
                _CUB_Serial_Error = value;
                OnPropertyChanged("CUB_Serial_Border");
                CUB_Serial_Popup_Open = value;
                OnPropertyChanged("CUB_Serial_Popup_Open");
            }
        }

        #endregion

        #region BSA_A_Serial_Error

        private Boolean _BSA_A_Serial_Error;

        /// <summary>Property BSA_B_Serial_Error of type Boolean</summary>
        public Boolean BSA_A_Serial_Error
        {
            get { return _BSA_A_Serial_Error; }
            set
            {
                _BSA_A_Serial_Error = value;
                OnPropertyChanged("BSA_A_Serial_Border");
                BSA_A_Serial_Popup_Open = value;
                OnPropertyChanged("BSA_A_Serial_Popup_Open");
            }
        }

        #endregion

        #region BSA_B_Serial_Error

        private Boolean _BSA_B_Serial_Error;

        /// <summary>Property BSA_B_Serial_Error of type Boolean</summary>
        public Boolean BSA_B_Serial_Error
        {
            get { return _BSA_B_Serial_Error; }
            set
            {
                _BSA_B_Serial_Error = value;
                OnPropertyChanged("BSA_B_Serial_Border");
                BSA_B_Serial_Popup_Open = value;
                OnPropertyChanged("BSA_B_Serial_Popup_Open");
            }
        }

        #endregion

        #region UIC_SID_Error

        private Boolean _UIC_SID_Error;

        /// <summary>Property UIC_SID_Error of type Boolean</summary>
        public Boolean UIC_SID_Error
        {
            get { return _UIC_SID_Error; }
            set
            {
                _UIC_SID_Error = value;
                OnPropertyChanged("UIC_SID_Border");
                UIC_SID_Popup_Open = value;
                OnPropertyChanged("UIC_SID_Popup_Open");
            }
        }

        #endregion

        #region UIC_Serial_Error

        private Boolean _UIC_Serial_Error;

        /// <summary>Property UIC_Serial_Error of type Boolean</summary>
        public Boolean UIC_Serial_Error
        {
            get { return _UIC_Serial_Error; }
            set
            {
                _UIC_Serial_Error = value;
                OnPropertyChanged("UIC_Serial_Border");
                UIC_Serial_Popup_Open = value;
                OnPropertyChanged("UIC_Serial_Popup_Open");
            }
        }

        #endregion

        #region Pivot_Serial_Error

        private Boolean _Pivot_Serial_Error;

        /// <summary>Property Pivot_Serial_Error of type Boolean</summary>
        public Boolean Pivot_Serial_Error
        {
            get { return _Pivot_Serial_Error; }
            set
            {
                _Pivot_Serial_Error = value;
                OnPropertyChanged("Pivot_Serial_Border");
                Pivot_Serial_Popup_Open = value;
                OnPropertyChanged("Pivot_Serial_Popup_Open");
            }
        }

        #endregion

        #region MotorLeft_Serial_Error

        private Boolean _MotorLeft_Serial_Error;

        /// <summary>Property MotorLeft_Serial_Error of type Boolean</summary>
        public Boolean MotorLeft_Serial_Error
        {
            get { return _MotorLeft_Serial_Error; }
            set
            {
                _MotorLeft_Serial_Error = value;
                OnPropertyChanged("MotorLeft_Serial_Border");
                MotorLeft_Serial_Popup_Open = value;
                OnPropertyChanged("MotorLeft_Serial_Popup_Open");
            }
        }

        #endregion

        #region MotorRight_Serial_Error

        private Boolean _MotorRight_Serial_Error;

        /// <summary>Property MotorRight_Serial_Error of type Boolean</summary>
        public Boolean MotorRight_Serial_Error
        {
            get { return _MotorRight_Serial_Error; }
            set
            {
                _MotorRight_Serial_Error = value;
                OnPropertyChanged("MotorRight_Serial_Border");
                MotorRight_Serial_Popup_Open = value;
                OnPropertyChanged("MotorRight_Serial_Popup_Open");
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region ReturnCommand

        /// <summary>Delegate Command: ReturnCommand</summary>
        public DelegateCommand ReturnCommand { get; set; }
        private Boolean CanCommandReturn() { return ExtractionInProgress == false; }
        private void CommandReturn()
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, Work_Order_Summary_Control.Control_Name);
        }

        #endregion


        #region CancelCommand

        /// <summary>Delegate Command: CancelCommand</summary>
        public DelegateCommand CancelCommand { get; set; }
        private Boolean CanCommandCancel() { return ExtractionInProgress == true; }
        private void CommandCancel()
        {
            CAN2_Commands.Continue_Processing = false;
        }

        #endregion


        #region RetrieveCommand

        /// <summary>Delegate Command: RetrieveCommand</summary>
        public DelegateCommand RetrieveCommand { get; set; }
        private Boolean CanCommandRetrieve() { return ExtractionInProgress == false; }

        private void CommandRetrieve()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Thread configThread = new Thread(new ThreadStart(Retrieve_Configuration_Back));
                configThread.IsBackground = true;
                configThread.Start();
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


        #region AuthorizeCommand

        /// <summary>Delegate Command: AuthorizeCommand</summary>
        public DelegateCommand AuthorizeCommand { get; set; }
        private Boolean CanCommandAuthorize()
        {
            //if (String.IsNullOrEmpty(CUA_Serial)) return false;
            //if (String.IsNullOrEmpty(CUB_Serial)) return false;
            //if (String.IsNullOrEmpty(BSA_A_Serial)) return false;
            //if (String.IsNullOrEmpty(BSA_B_Serial)) return false;
            //if (String.IsNullOrEmpty(UIC_SID)) return false;
            //if (String.IsNullOrEmpty(UIC_Serial)) return false;
            ////if (String.IsNullOrEmpty(Pivot_Serial)) return false;
            return !ExtractionInProgress;
        }

        private void CommandAuthorize()
        {
            Thread back = new Thread(new ThreadStart(CommandAuthorize_Back));
            back.IsBackground = true;
            back.Start();
        }

        #endregion


        #region YesCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: YesCommand</summary>
        public DelegateCommand YesCommand { get; set; }
        private Boolean CanCommandYes() { return true; }
        private void CommandYes()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                ContinueOnError = true;
                ConfigurationOverridePopupOpen = false;
                logger.Debug("Failed to extract Start-Configuration. User selected to set override.");
                SART_Work_Order wo = Syteline_WO_Web_Service_Client_REST.Select_SART_Work_Order_WORK_ORDER_ID(InfrastructureModule.Token, Work_Order_Num);
                if (wo != null)
                {
                    wo.Config_Start_Override = true;
                    if (Syteline_WO_Web_Service_Client_REST.Update_SART_Work_Order_Object(InfrastructureModule.Token, wo))
                    {
                        SART_Common.Create_Event(WorkOrder_Events.Set_StartConfiguration_Override, Event_Statuses.Passed, 0, "Setting override at failed Start-Configuration");

                        //SART_Events evnt = new SART_Events();
                        //evnt.StatusType = Event_Statuses.Passed;
                        //evnt.EventType = WorkOrder_Events.Set_StartConfiguration_Override;
                        //evnt.Timestamp = DateTime.Now;
                        //evnt.Message = "Setting override at failed Start-Configuration";
                        //evnt.User_Name = User_Info.UserName;
                        //evnt.Work_Order_ID = Work_Order_Num;
                        //SART_2012_Web_Service_Client.Insert_SART_Events_Key(InfrastructureModule.Token, evnt);
                        aggregator.GetEvent<WorkOrder_Clear_List_Event>().Publish(true);
                        aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Override set for Work Order: {0}", Work_Order_Num));
                    }
                    else
                    {
                        aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Failed to set override for Work Order: {0}", Work_Order_Num));
                        logger.Error(String.Format("Failed to set override for Work Order: {0}", Work_Order_Num));
                    }
                }
                else
                {
                    String msg = $"Failed to set override for Work Order: {Work_Order_Num} - Not Found";
                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
                    logger.Error(msg);
                }
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
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


        #region NoCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: NoCommand</summary>
        public DelegateCommand NoCommand { get; set; }
        private Boolean CanCommandNo() { return true; }
        private void CommandNo()
        {
            ContinueOnError = false;
            ConfigurationOverridePopupOpen = false;
            logger.Debug("Failed to extract Start-Configuration. User did not select to set override.");
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
            CommandClearBSAA();
            CommandClearBSAB();
            CommandClearCUA();
            CommandClearCUB();
            CommandClearMotorL();
            CommandClearMotorR();
            CommandClearPivot();
            CommandClearUICSerial();
            CommandClearUISID();


            OnPropertyChanged("UIC_SID");
            OnPropertyChanged("UIC_Serial");
            OnPropertyChanged("CUA_Serial");
            OnPropertyChanged("CUB_Serial");
            OnPropertyChanged("BSA_A_Serial");
            OnPropertyChanged("BSA_B_Serial");
            OnPropertyChanged("Pivot_Serial");
        }

        /////////////////////////////////////////////
        #endregion


        #region ClearCUACommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearCUACommand</summary>
        public DelegateCommand ClearCUACommand { get; set; }
        private Boolean CanCommandClearCUA() { return true; }
        private void CommandClearCUA()
        {
            PT_Config.CUA_Date = null;
            PT_Config.CUA_Part_Number = null;
            PT_Config.CUA_SW_Version = null;
            PT_Config.CUA_User = null;
            CUA_Serial_Error = false;
            CUA_Serial = null;
        }

        /////////////////////////////////////////////
        #endregion


        #region ClearCUBCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearCUBCommand</summary>
        public DelegateCommand ClearCUBCommand { get; set; }
        private Boolean CanCommandClearCUB() { return true; }
        private void CommandClearCUB()
        {
            PT_Config.CUB_Date = null;
            PT_Config.CUB_Part_Number = null;
            PT_Config.CUB_SW_Version = null;
            PT_Config.CUB_User = null;
            CUB_Serial_Error = false;
            CUB_Serial = null;
        }

        /////////////////////////////////////////////
        #endregion


        #region ClearBSAACommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearBSAACommand</summary>
        public DelegateCommand ClearBSAACommand { get; set; }
        private Boolean CanCommandClearBSAA() { return true; }
        private void CommandClearBSAA()
        {
            PT_Config.BSA_A_Date = null;
            PT_Config.BSA_A_Part_Number = null;
            PT_Config.BSA_A_SW_Version = null;
            PT_Config.BSA_A_User = null;
            BSA_A_Serial_Error = false;
            BSA_A_Serial = null;
        }

        /////////////////////////////////////////////
        #endregion


        #region ClearBSABCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearBSABCommand</summary>
        public DelegateCommand ClearBSABCommand { get; set; }
        private Boolean CanCommandClearBSAB() { return true; }
        private void CommandClearBSAB()
        {
            PT_Config.BSA_B_Date = null;
            PT_Config.BSA_B_Part_Number = null;
            PT_Config.BSA_B_SW_Version = null;
            PT_Config.BSA_B_User = null;
            BSA_B_Serial_Error = false;
            BSA_B_Serial = null;
        }

        /////////////////////////////////////////////
        #endregion


        #region ClearUISIDCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearUISIDCommand</summary>
        public DelegateCommand ClearUISIDCommand { get; set; }
        private Boolean CanCommandClearUISID() { return true; }
        private void CommandClearUISID()
        {
            UIC_SID = null;
            UIC_SID_Error = false;
        }

        /////////////////////////////////////////////
        #endregion


        #region ClearMotorLCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearMotorLCommand</summary>
        public DelegateCommand ClearMotorLCommand { get; set; }
        private Boolean CanCommandClearMotorL() { return true; }
        private void CommandClearMotorL()
        {
            PT_Config.Motorl_Date = null;
            PT_Config.Motorl_Part_Number = null;
            PT_Config.Motorl_Type = null;
            PT_Config.Motorl_User = null;
            MotorLeft_Serial = null;
            MotorLeft_Serial_Error = false;
        }

        /////////////////////////////////////////////
        #endregion


        #region ClearMotorRCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearMotorRCommand</summary>
        public DelegateCommand ClearMotorRCommand { get; set; }
        private Boolean CanCommandClearMotorR() { return true; }
        private void CommandClearMotorR()
        {
            PT_Config.Motorr_Date = null;
            PT_Config.Motorr_Part_Number = null;
            PT_Config.Motorr_Type = null;
            PT_Config.Motorr_User = null;
            MotorRight_Serial = null;
            MotorRight_Serial_Error = false;
        }

        /////////////////////////////////////////////
        #endregion


        #region ClearUICSerialCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearUICSerialCommand</summary>
        public DelegateCommand ClearUICSerialCommand { get; set; }
        private Boolean CanCommandClearUICSerial() { return true; }
        private void CommandClearUICSerial()
        {
            PT_Config.UIC_Date = null;
            PT_Config.UIC_Part_Number = null;
            PT_Config.UIC_SW_Version = null;
            PT_Config.UIC_Type = null;
            PT_Config.UIC_User = null;
            UIC_Serial_Error = false;
            UIC_Serial = null;
        }

        /////////////////////////////////////////////
        #endregion


        #region ClearPivotCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearPivotCommand</summary>
        public DelegateCommand ClearPivotCommand { get; set; }
        private Boolean CanCommandClearPivot() { return true; }
        private void CommandClearPivot()
        {
            PT_Config.Pivot_Date = null;
            PT_Config.Pivot_Part_Number = null;
            PT_Config.Pivot_User = null;
            Pivot_Serial_Error = false;
            Pivot_Serial = null;
        }

        /////////////////////////////////////////////
        #endregion


        #region BlankCUACommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BlankCUACommand</summary>
        public DelegateCommand BlankCUACommand { get; set; }
        private Boolean CanCommandBlankCUA() { return true; }
        private void CommandBlankCUA()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                CUA_Serial = String.Empty;
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


        #region BlankCUBCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BlankCUBCommand</summary>
        public DelegateCommand BlankCUBCommand { get; set; }
        private Boolean CanCommandBlankCUB() { return true; }
        private void CommandBlankCUB()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                CUB_Serial = String.Empty;
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


        #region BlankBSAACommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BlankBSAACommand</summary>
        public DelegateCommand BlankBSAACommand { get; set; }
        private Boolean CanCommandBlankBSAA() { return true; }
        private void CommandBlankBSAA()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                BSA_A_Serial = String.Empty;
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


        #region BlankBSABCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BlankBSABCommand</summary>
        public DelegateCommand BlankBSABCommand { get; set; }
        private Boolean CanCommandBlankBSAB() { return true; }
        private void CommandBlankBSAB()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                BSA_B_Serial = String.Empty;
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


        #region BlankUISIDCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BlankUISIDCommand</summary>
        public DelegateCommand BlankUISIDCommand { get; set; }
        private Boolean CanCommandBlankUISID() { return true; }
        private void CommandBlankUISID()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                UIC_SID = String.Empty;
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


        #region OpenPopupCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: OpenPopupCommand</summary>
        public DelegateCommand OpenPopupCommand { get; set; }
        private Boolean CanCommandOpenPopup() { return true; }
        private void CommandOpenPopup()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = Brushes.Pink;
                PopupMessage = msg;
                PopupIsOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Store_Current_Work_Order  -- WorkOrder_Instance_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Store_Current_Work_Order(Boolean wo)
        {
            PT_Config = null;
            //PT_Config = PT_Config;
            //logger.Debug("PT Config: {1}, {2}, {0}, {3}", PT_Config.Date_Time_Entered, PT_Config.Work_Order, PT_Config.Serial_Number, PT_Config.ConfigType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Add_Event  -- SART_EventLog_Add_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Add_Event(SART_Event_Log_Entry log)
        {
            if (log.Object_ID == EventID)
            {
                ProgressLog.Add(log);
                SelectedLogEntry = log;
                OnPropertyChanged("ProgressLog");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Update_Event  -- SART_EventLog_Update_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Update_Event(SART_Event_Log_Entry log)
        {
            if (log.Object_ID == EventID)
            {
                ProgressLog.Remove(log);
                ProgressLog.Add(log);
                SelectedLogEntry = log;
                OnPropertyChanged("ProgressLog");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Clear_Event  -- WO_Config_Clear_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Clear_Event(Boolean clearRequested)
        {
            if (clearRequested)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    if (ProgressLog != null)
                    {
                        ProgressLog.Clear();
                        OnPropertyChanged("ProgressLog");
                    }
                });
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Retrieve_EventID  -- WO_Config_EventID_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Retrieve_EventID(int id)
        {
            EventID = id;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Display_CUA  -- SART_Configuration_CUA_Display_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Display_CUA(String data)
        {
            CUA_Serial = data;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Display_CUB  -- SART_Configuration_CUB_Display_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Display_CUB(String data)
        {
            CUB_Serial = data;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Display_BSAA  -- SART_Configuration_BSAA_Display_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Display_BSAA(String data)
        {
            BSA_A_Serial = data;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Display_BSAB  -- SART_Configuration_BSAB_Display_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Display_BSAB(String data)
        {
            BSA_B_Serial = data;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Display_UI_SID  -- SART_Configuration_UISID_Display_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Display_UI_SID(String data)
        {
            UIC_SID = data;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Display_UIC_Serial  -- SART_Configuration_UICSerial_Display_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Display_UIC_Serial(String data)
        {
            UIC_Serial = data;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region ConfigurationType_Handler  -- WorkOrder_ConfigurationType_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void ConfigurationType_Handler(ConfigurationTypes ctype)
        {
            Config_Type = ctype;
            switch (ctype)
            {
                case ConfigurationTypes.Service_Start:
                    StartConfig = Find_Configuration(InfrastructureModule.Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Start);
                    if ((StartConfig == null) || ((StartConfig.Date_Time_Entered.HasValue == true) && (StartConfig.Date_Time_Entered.Value < DateTime.Today)))
                    {
                        StartConfig = SART_PTCnf_Web_Service_Client_REST.Select_SART_PT_Configuration_WORK_ORDER(InfrastructureModule.Token, InfrastructureModule.Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Start);
                        if ((StartConfig == null) || ((StartConfig.Date_Time_Entered.HasValue == true) && (StartConfig.Date_Time_Entered.Value < DateTime.Today)))
                        {
                            StartConfig = new SART_PT_Configuration();
                            StartConfig.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                            StartConfig.ConfigType = ConfigurationTypes.Service_Start;
                            StartConfig.Serial_Number = InfrastructureModule.Current_Work_Order.PT_Serial;
                            StartConfig.Date_Time_Entered = DateTime.Now;
                            StartConfig = SART_PTCnf_Web_Service_Client_REST.Insert_SART_PT_Configuration_Key(InfrastructureModule.Token, StartConfig);
                        }
                    }
                    PT_Config.Update(StartConfig, true);
                    break;
                case ConfigurationTypes.Service_Final:
                    FinalConfig = Find_Configuration(InfrastructureModule.Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Final);
                    if ((FinalConfig == null) || ((FinalConfig.Date_Time_Entered.HasValue == true) && (FinalConfig.Date_Time_Entered.Value < DateTime.Today)))
                    {
                        FinalConfig = SART_PTCnf_Web_Service_Client_REST.Select_SART_PT_Configuration_WORK_ORDER(InfrastructureModule.Token, InfrastructureModule.Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Final);
                        if ((FinalConfig == null) || ((FinalConfig.Date_Time_Entered.HasValue == true) && (FinalConfig.Date_Time_Entered.Value < DateTime.Today)))
                        {
                            FinalConfig = new SART_PT_Configuration();
                            FinalConfig.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                            FinalConfig.ConfigType = ConfigurationTypes.Service_Final;
                            FinalConfig.Serial_Number = InfrastructureModule.Current_Work_Order.PT_Serial;
                            FinalConfig.Date_Time_Entered = DateTime.Now;
                            FinalConfig = SART_PTCnf_Web_Service_Client_REST.Insert_SART_PT_Configuration_Key(InfrastructureModule.Token, FinalConfig);
                        }
                    }
                    PT_Config.Update(FinalConfig, true);
                    break;
                default:
                    PT_Config = null;
                    break;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Configuration_ClearLog_Handler  -- Event: WorkOrder_Configuration_ClearLog_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Configuration_ClearLog_Handler(Boolean clear)
        {
            ProgressLog.Clear();
            OnPropertyChanged("ProgressLog");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Close_Handler  -- Event: SART_WorkOrder_Close_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Close_Handler(Boolean close)
        {
            Reset();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Cancel_Handler  -- Event: SART_WorkOrder_Cancel_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Cancel_Handler(Boolean cancel)
        {
            Reset();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Configuration_Return_Handler  -- Event: WorkOrder_Configuration_Return_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Configuration_Return_Handler(Boolean ret)
        {
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Login_Handler  -- Event: Application_Login_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Login_Handler(String name)
        {
            _Token = null;
            _LoginContext = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("");
            aggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Work Order", Work_Order_Configuration_Control.Control_Name));
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            aggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            aggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);
            OnPropertyChanged("CUA_Serial");
            OnPropertyChanged("CUB_Serial");
            OnPropertyChanged("BSA_A_Serial");
            OnPropertyChanged("BSA_B_Serial");
            OnPropertyChanged("UIC_Serial");
            OnPropertyChanged("UIC_SID");
            OnPropertyChanged("Pivot_Serial");
            OnPropertyChanged("MotorLeft_Serial");
            OnPropertyChanged("MotorRight_Serial");
            OnPropertyChanged("PTSerial");
            OnPropertyChanged("Work_Order_Num");
            OnPropertyChanged("WorkOrderColor");
            OnPropertyChanged("UIC_Serial_Visibility");
            Clear_Errors();
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods
        ////////////////////////////////////////////////////////////////////////////////////////////////

        private void Clear_Errors()
        {
            CUA_Serial_Error =
            CUB_Serial_Error =
            BSA_A_Serial_Error =
            BSA_B_Serial_Error =
            UIC_SID_Error =
            UIC_Serial_Error =
            Pivot_Serial_Error =
            MotorLeft_Serial_Error =
            MotorRight_Serial_Error =
            false;
        }


        private void Retrieve_Configuration_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Clear_Errors();
                aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                ExtractionInProgress = true;
                CAN2_Commands.Continue_Processing = true;
                //SART_Common.Initialize_Timeouts_and_Delays();


                if (PT_Config.Date_Time_Created.HasValue == false)
                {
                    PT_Config.Date_Time_Created = DateTime.Now;
                }
                else if (PT_Config.Date_Time_Created.Value.Date != DateTime.Today)
                {
                    PT_Config.Date_Time_Created = DateTime.Now;
                }
                PT_Config.Date_Time_Entered = DateTime.Now;
                CancelVisibility = Visibility.Visible;
                RetrieveVisibility = Visibility.Collapsed;

                logger.Debug("Retrieving Security table information");
                List<Security> secList = Manufacturing_Security_Web_Service_Client_REST.Select_Security_UNIT_ID_SERIAL_NUMBER(InfrastructureModule.Token, InfrastructureModule.Current_Work_Order.PT_Serial);
                if ((secList != null) && (secList.Count > 0))
                {
                    Security sec = new Security();
                    foreach (Security secRec in secList)
                    {
                        sec.Update(secRec);
                    }

                    logger.Debug("Extracting configuration");
                    //////////////////////////////////////////////////////////////////////////////////
                    Extract_Configuration(sec, PT_Config);
                    //////////////////////////////////////////////////////////////////////////////////
                }
                else
                {
                    PopupMessage = String.Format("Could not find Security information for PT: {0}", InfrastructureModule.Current_Work_Order.PT_Serial);
                    PopupIsOpen = true;
                    PopupColor = System.Windows.Media.Brushes.LightGoldenrodYellow;
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
            }
            catch (Exception e)
            {
                String msg = Exception_Helper.FormatExceptionString(e);
                logger.Error(msg);
                PopupMessage = msg;
                PopupIsOpen = true;
                PopupColor = System.Windows.Media.Brushes.Pink;
            }
            finally
            {
                aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                ExtractionInProgress = false;
                CancelVisibility = Visibility.Collapsed;
                RetrieveVisibility = Visibility.Visible;

                OnPropertyChanged("UIC_SID");
                OnPropertyChanged("UIC_Serial");
                OnPropertyChanged("BSA_A_Serial");
                OnPropertyChanged("BSA_B_Serial");
                OnPropertyChanged("CUA_Serial");
                OnPropertyChanged("CUB_Serial");
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void Reset()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                PT_Config = null;
                StartConfig = null;
                FinalConfig = null;
                EventID = 0;
                OnPropertyChanged("Work_Order_Num");
                OnPropertyChanged("PTSerial");
                OnPropertyChanged("WorkOrderColor");
                ExtractionInProgress = false;
                User_Info = null;
                ProgressLog = null;
                SelectedLogEntry = null;
                CUA_Serial = null;
                CUB_Serial = null;
                BSA_A_Serial = null;
                BSA_B_Serial = null;
                UIC_Serial = null;
                UIC_SID = null;
                Pivot_Serial = null;
                IsPivotSerialFocused = false;
                MotorLeft_Serial = null;
                MotorRight_Serial = null;
                MotorLeft_Visibility = Visibility.Collapsed;
                MotorRight_Visibility = Visibility.Collapsed;
                CancelVisibility = Visibility.Collapsed;
                RetrieveVisibility = Visibility.Visible;
                ConfigurationOverridePopupOpen = false;
                ConfigurationOverrideMessage = null;
                ContinueOnError = null;
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


        private void CommandAuthorize_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Clear_Errors();


                if (PT_Config.ConfigType == ConfigurationTypes.Service_Final)
                {
                    if (PT_Config.CUA_Serial == null)
                    {
                        logger.Warn("CU-A empty");
                        CUA_Serial_Error = true;
                    }
                    else
                    {
                        CUA_Serial_Error = false;
                    }

                    if (PT_Config.CUB_Serial == null)
                    {
                        logger.Warn("CU-B empty");
                        CUB_Serial_Error = true;
                    }
                    else
                    {
                        CUB_Serial_Error = false;
                    }

                    if (PT_Config.BSA_A_Serial == null)
                    {
                        logger.Warn("BSA-A empty");
                        BSA_A_Serial_Error = true;
                    }
                    else
                    {
                        BSA_A_Serial_Error = false;
                    }

                    if (PT_Config.BSA_B_Serial == null)
                    {
                        logger.Warn("BSA-B empty");
                        BSA_B_Serial_Error = true;
                    }
                    else
                    {
                        BSA_B_Serial_Error = false;
                    }

                    if (PT_Config.UIC_SID == null)
                    {
                        logger.Warn("UIC-SID empty");
                        UIC_SID_Error = true;
                    }
                    else
                    {
                        UIC_SID_Error = false;
                    }

                    if (Is_PT_G2(InfrastructureModule.Current_Work_Order.PT_Part_Number) == true)
                    {
                        if (String.IsNullOrEmpty(PT_Config.UIC_Serial))
                        {
                            logger.Warn("UIC Serial empty");
                            UIC_Serial_Error = true;
                        }
                        else
                        {
                            UIC_Serial_Error = false;
                        }
                    }

                    //if (String.IsNullOrEmpty(PT_Config.Pivot_Serial))
                    //{
                    //    logger.Warn("Pivot Serial empty");
                    //    Pivot_Serial_Error = true;
                    //}
                    //else
                    //{
                    //    Pivot_Serial_Error = false;
                    //}

                    Boolean IsError = (CUA_Serial_Error || CUB_Serial_Error || BSA_A_Serial_Error || BSA_B_Serial_Error || UIC_Serial_Error || UIC_SID_Error);

                    try
                    {
                        if (IsError == false)
                        {
                            PT_Config.Status = "Passed";
                            FinalConfig.Copy(PT_Config, true);

                            //if (SART_2012_Web_Service_Client.Update_SART_PT_Configuration_Key(InfrastructureModule.Token, PT_Config) == false)
                            //{
                            //    throw new Exception("Unable to save Final PT Configuration");
                            //}


                            if (SART_PTCnf_Web_Service_Client_REST.Update_SART_PT_Configuration_Key(InfrastructureModule.Token, FinalConfig) == false)
                            {
                                var tmp = SART_PTCnf_Web_Service_Client_REST.Select_SART_PT_Configuration_Key(InfrastructureModule.Token, FinalConfig.ID);
                                if (tmp != FinalConfig)
                                {
                                    logger.Warn("Unable to save Final PT Configuration");
                                }
                            }




                            logger.Debug("Validation of Final configuration: Passed");
                            SART_Common.Create_Event(WorkOrder_Events.Final_Configuration_Validation, Event_Statuses.Passed);
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Final configuration was successful");
                            aggregator.GetEvent<WorkOrder_Configuration_Final_UpdateDB_Event>().Publish(PT_Config);

                            Application.Current.Dispatcher.Invoke((Action)delegate ()
                            {
                                CommandReturn();
                            });
                        }
                        else
                        {
                            logger.Warn("Validation of Final configuration: Failed");
                            SART_Common.Create_Event(WorkOrder_Events.Final_Configuration_Validation, Event_Statuses.Failed);
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Final configuration failed");
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        aggregator.GetEvent<WorkOrder_Configuration_Final_Event>().Publish(!IsError);
                        aggregator.GetEvent<WorkOrder_Configuration_Event>().Publish(PT_Config);
                        aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
                    }
                }
                else if (PT_Config.ConfigType == ConfigurationTypes.Service_Start)
                {
                    List<SART_PT_Configuration> configs = null;
                    if (container.IsRegistered<List<SART_PT_Configuration>>("Configurations") == true)
                    {
                        configs = container.Resolve<List<SART_PT_Configuration>>("Configurations");
                    }
                    //else
                    //    List<SART_PT_Configuration> configs = container.Resolve<List<SART_PT_Configuration>>("PT_Configurations_List");
                    if ((configs == null) || (configs.Count == 0))
                    {
                        logger.Warn("No configuration data in container");

                        Boolean error = false;
                        String errMsg = null;
                        configs = PTConfiguration.Get_PT_Configuration(InfrastructureModule.Current_Work_Order.PT_Serial, InfrastructureModule.Current_Work_Order.Work_Order_ID, out error, out errMsg);
                        if (error == true)
                        {
                            String msg = String.Format("The following error(s) occurred while retrieving configuration data:\n{0}", errMsg);
                            logger.Error(msg);
                            PopupColor = Brushes.Pink;
                            PopupMessage = msg;
                            PopupIsOpen = true;
                        }
                    }

                    if (configs == null)
                    {
                        logger.Warn("Still no configuration data");
                        configs = new List<SART_PT_Configuration>();
                    }
                    if (configs.Count > 0) configs.Sort(new SART_PT_Configuration_Created_Comparer());
                    SART_PT_Configuration currentConfig = new SART_PT_Configuration();
                    foreach (SART_PT_Configuration config in configs)
                    {
                        if (config != PT_Config) currentConfig.Update(config);
                    }

                    /////////////////////////////////////
                    // Testing CU-A
                    if (PT_Config.CUA_Serial == String.Empty)
                    {
                        logger.Debug("CU-A Marked as Blank");
                        CUA_Serial_Error = false;
                    }
                    else if (String.IsNullOrEmpty(currentConfig.CUA_Serial) == false)
                    {
                        if (currentConfig.CUA_Serial == CUA_Serial)
                        {
                            logger.Debug("CU-A Matched");
                            CUA_Serial_Error = false;
                        }
                        else if (currentConfig.CUB_Serial == CUA_Serial)
                        {
                            logger.Debug("CU-A Matched to CU-B");
                            CUA_Serial_Error = false;
                        }
                        else
                        {
                            logger.Warn("CU-A did not match");
                            CUA_Serial_Error = true;
                        }
                    }
                    else
                    {
                        logger.Warn("CU-A empty - no configuration found");
                        CUA_Serial_Error = true;
                    }
                    // Testing CU-A
                    /////////////////////////////////////

                    /////////////////////////////////////
                    // Testing CU-B
                    if (PT_Config.CUB_Serial == String.Empty)
                    {
                        logger.Debug("CU-B Marked as Blank");
                        CUB_Serial_Error = false;
                    }
                    else if (String.IsNullOrEmpty(currentConfig.CUB_Serial) == false)
                    {
                        if (currentConfig.CUB_Serial == CUB_Serial)
                        {
                            logger.Debug("CU-B Matched");
                            CUB_Serial_Error = false;
                        }
                        else if (currentConfig.CUA_Serial == CUB_Serial)
                        {
                            logger.Debug("CU-B Matched to CU-A");
                            CUB_Serial_Error = false;
                        }
                        else
                        {
                            logger.Warn("CU-B did not match");
                            CUB_Serial_Error = true;
                        }
                    }
                    else
                    {
                        logger.Warn("CU-B empty - no configuration found");
                        CUB_Serial_Error = true;
                    }
                    // Testing CU-B
                    /////////////////////////////////////

                    /////////////////////////////////////
                    // Testing BSA-A
                    if (PT_Config.BSA_A_Serial == String.Empty)
                    {
                        logger.Debug("BSA-A Marked as Blank");
                        BSA_A_Serial_Error = false;
                    }
                    else if (String.IsNullOrEmpty(currentConfig.BSA_A_Serial) == false)
                    {
                        if (currentConfig.BSA_A_Serial == BSA_A_Serial)
                        {
                            logger.Debug("BSA-A Matched");
                            BSA_A_Serial_Error = false;
                        }
                        else
                        {
                            logger.Warn("BSA-A did not match");
                            BSA_A_Serial_Error = true;
                        }
                    }
                    else
                    {
                        logger.Warn("BSA-A empty - no configuration found");
                        BSA_A_Serial_Error = true;
                    }
                    // Testing BSA-A
                    /////////////////////////////////////


                    /////////////////////////////////////
                    // Testing BSA-B
                    if (PT_Config.BSA_B_Serial == String.Empty)
                    {
                        logger.Debug("BSA-B Marked as Blank");
                        BSA_B_Serial_Error = false;
                    }
                    else if (String.IsNullOrEmpty(currentConfig.BSA_B_Serial) == false)
                    {
                        if (currentConfig.BSA_B_Serial == BSA_B_Serial)
                        {
                            logger.Debug("BSA-B Matched");
                            BSA_B_Serial_Error = false;
                        }
                        else
                        {
                            logger.Warn("BSA-B did not match");
                            BSA_B_Serial_Error = true;
                        }
                    }
                    else
                    {
                        logger.Warn("BSA-B empty - no configuration found");
                        BSA_B_Serial_Error = true;
                    }
                    // Testing BSA-B
                    /////////////////////////////////////

                    /////////////////////////////////////
                    // Testing UIC-SID
                    if (PT_Config.UIC_SID == String.Empty)
                    {
                        logger.Debug("UI-SID Marked as Blank");
                        UIC_SID_Error = false;
                    }
                    else if ((String.IsNullOrEmpty(currentConfig.UIC_SID) == false) && (String.IsNullOrEmpty(PT_Config.UIC_SID) == false))
                    {
                        try
                        {
                            logger.Debug("Latest UIC SID Configuration: {0}", currentConfig.UIC_SID);
                            String sid = currentConfig.UIC_SID;
                            if (sid.Length > 8) sid = sid.Substring(0, 8);
                            UInt32 sidVal = Converter.HexStringToUInt32(sid);
                            logger.Debug("UIC SID Converted as Hex: {0:X8}", sidVal);

                            logger.Debug("Read UIC SID: {0}", UIC_SID);
                            UInt32[] uicsid = Converter.HexStringToUInt32Array(UIC_SID);
                            if ((uicsid != null) && (uicsid.Length == 2) && (sidVal == uicsid[0]) && (sidVal == uicsid[1]))
                            {
                                logger.Debug("UIC-SID Matched");
                                UIC_SID_Error = false;
                            }
                            else
                            {
                                logger.Debug("UIC SID DECIMAL: {0}", sid);
                                sidVal = Converter.StringToUInt32(sid);
                                logger.Debug("UIC SID Converted as Dec: {0:X8}", sidVal);
                                if ((uicsid != null) && (uicsid.Length == 2) && (sidVal == uicsid[0]) && (sidVal == uicsid[1]))
                                {
                                    logger.Debug("UIC-SID Matched (decimal comparison)");
                                    UIC_SID_Error = false;
                                }
                                else
                                {
                                    logger.Warn("UIC-SID did not match");
                                    UIC_SID_Error = true;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error(Exception_Helper.FormatExceptionString(e));
                            logger.Warn("UIC-SID did not match");
                            UIC_SID_Error = true;
                        }
                    }
                    else if ((String.IsNullOrEmpty(currentConfig.UIC_SID) == true) && (String.IsNullOrEmpty(PT_Config.UIC_SID) == false))
                    {
                        logger.Warn("Read UIC SID: {0} - Previous UIC SID is null", PT_Config.UIC_SID);
                        UIC_SID_Error = false;
                    }
                    else if ((String.IsNullOrEmpty(currentConfig.UIC_SID) == false) && (String.IsNullOrEmpty(PT_Config.UIC_SID) == true))
                    {
                        logger.Warn("Read UIC SID is null - Previous UIC SID: {0}", currentConfig.UIC_SID);
                        UIC_SID_Error = true;
                    }
                    else
                    {
                        logger.Warn("Read UIC SID is null - Previous UIC SID is null");
                        UIC_SID_Error = true;
                    }
                    // Testing UIC-SID
                    /////////////////////////////////////


                    UIC_Config uc = new UIC_Config();
                    foreach (UIC_Config cfg in Manufacturing_UICConf_Web_Service_Client_REST.Select_UIC_Config_PT_SERIAL_NUMBER(InfrastructureModule.Token, InfrastructureModule.Current_Work_Order.PT_Serial))
                    {
                        uc.Update(cfg);
                    }

                    /////////////////////////////////////
                    // Testing UIC Serial
                    if (Is_PT_G2(InfrastructureModule.Current_Work_Order.PT_Part_Number) == true)
                    {
                        while (true)
                        {
                            if (String.IsNullOrEmpty(currentConfig.UIC_Serial) == false)
                            {
                                if (currentConfig.UIC_Serial == UIC_Serial)
                                {
                                    logger.Debug("UIC Serial Matched (configuration)");
                                    UIC_Serial_Error = false;
                                    break;
                                }
                                else
                                {
                                    logger.Warn("UIC Serial did not match (configuration");
                                    UIC_Serial_Error = true;
                                }
                            }

                            if (String.IsNullOrEmpty(uc.UIC_Serial_Number) == false)
                            {
                                if (uc.UIC_Serial_Number == UIC_Serial)
                                {
                                    logger.Debug("UIC Serial Matched (database)");
                                    UIC_Serial_Error = false;
                                    break;
                                }
                                else
                                {
                                    logger.Warn("UIC Serial did not match (database)");
                                    UIC_Serial_Error = true;
                                }
                            }
                            else if (String.IsNullOrEmpty(currentConfig.UIC_Serial) == true)
                            {
                                logger.Warn("Did not find a UIC Serial Number for PT: {0}", InfrastructureModule.Current_Work_Order.PT_Serial);
                                // Nothing to compare it against
                                UIC_Serial_Error = false;
                            }

                            break;
                        }
                    }
                    // Testing UIC Serial
                    /////////////////////////////////////



                    /////////////////////////////////////
                    // Testing Pivot Serial
                    Pivot_Serial_Error = false;
                    if (String.IsNullOrEmpty(Pivot_Serial) == false)
                    {
                        if (String.IsNullOrEmpty(currentConfig.Pivot_Serial) == false)
                        {
                            if (currentConfig.Pivot_Serial == Pivot_Serial)
                            {
                                Pivot_Serial_Error = false;
                            }
                            else
                            {
                                Pivot_Serial_Error = true;
                            }
                        }
                    }
                    // Testing Pivot Serial
                    /////////////////////////////////////
                    // 3/27/2013: UIC_Serial_Error and  Pivot_Serial_Error are removed from this list of blocking errors
                    // Many manufactured units did not record the data.  We want it to be captured now, but we expect many to fail validation.
                    // In time, the constraints can be added.

                    Boolean IsError = (CUA_Serial_Error || CUB_Serial_Error ||
                                       BSA_A_Serial_Error || BSA_B_Serial_Error || UIC_Serial_Error //|| UIC_SID_Error
                                       );

                    if (UIC_SID_Error == true) logger.Warn("Did not find a match for PT UIC SID value: {0}", currentConfig.UIC_SID);
                    if (UIC_Serial_Error == true) logger.Warn("Did not find a match for PT UIC Serial Number: {0}", currentConfig.UIC_Serial);
                    if (Pivot_Serial_Error == true) logger.Warn("Did not find a match for PT Pivot Serial Number: {0}", currentConfig.Pivot_Serial);

                    IsError = (IsError || UIC_SID_Error || UIC_Serial_Error || Pivot_Serial_Error) /*Thread.Sleep(2000)*/;

                    try
                    {
                        StartConfig.Copy(PT_Config, true);
                        if (IsError == false)
                        {
                            StartConfig.Status = "Passed";
                            if (SART_PTCnf_Web_Service_Client_REST.Update_SART_PT_Configuration_Key(InfrastructureModule.Token, StartConfig) == false)
                            {
                                var tmp = SART_PTCnf_Web_Service_Client_REST.Select_SART_PT_Configuration_Key(InfrastructureModule.Token, StartConfig.ID);
                                if (tmp != StartConfig)
                                {
                                    logger.Warn("Unable to save Start PT Configuration");
                                }
                            }

                            logger.Debug("Validation of Starting configuration: Passed");
                            SART_Common.Create_Event(WorkOrder_Events.Start_Configuration_Validation, Event_Statuses.Passed);
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Start configuration was successful");
                            Application.Current.Dispatcher.Invoke((Action)delegate ()
                            {
                                CommandReturn();
                            });
                        }
                        else
                        {
                            PT_Config.Status = "Failed";
                            logger.Warn("Validation of Starting configuration: Failed");
                            SART_Common.Create_Event(WorkOrder_Events.Start_Configuration_Validation, Event_Statuses.Failed);
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Start configuration failed");
                            // TODO: UNCOMMENT FOLLOWING CODE FOR START CONFIG OVERRIDE POPUP - DONE (BY ASAQIB ON 12/4/2013)
                            //ConfigurationOverrideMessage = String.Format("Failed to extract Start-Configuration. Do you want to\nset override for Work Order: {0}?", Work_Order_Num);
                            //ConfigurationOverridePopupOpen = true;
                        }
                    }
                    finally
                    {
                        aggregator.GetEvent<WorkOrder_Configuration_Start_Event>().Publish(!IsError);
                        aggregator.GetEvent<WorkOrder_Configuration_Event>().Publish(PT_Config);
                        aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
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
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = Brushes.Pink;
                PopupMessage = msg;
                PopupIsOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private SART_PT_Configuration Find_Configuration(String Work_Order_ID, ConfigurationTypes type)
        {
            SART_PT_Configuration config = null;
            List<SART_PT_Configuration> configurations = container.Resolve<List<SART_PT_Configuration>>("Configurations");
            if (configurations != null)
            {
                if (configurations.Count > 1) configurations.Sort(new SART_PT_Configuration_Created_Comparer());
                foreach (SART_PT_Configuration cnf in configurations)
                {
                    if (cnf.Work_Order != Work_Order_ID) continue;
                    if (cnf.ConfigType == type)
                    {
                        config = cnf;
                    }
                }
            }
            return config;
        }

        private Boolean Is_PT_G2(String partnum)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Part_Number_XRefs.ContainsKey(partnum) == false) return false;
                String manPartNum = Part_Number_XRefs[partnum].Production_Part_Number;
                if (Part_Number_Info.ContainsKey(manPartNum) == false) return false;
                return Part_Number_Info[manPartNum].Model.ToUpper().StartsWith("G2");
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = Brushes.Pink;
                PopupMessage = msg;
                PopupIsOpen = true;
                return false;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private Boolean Is_PT_SE(String partnum)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Part_Number_XRefs.ContainsKey(partnum) == false) return false;
                String manPartNum = Part_Number_XRefs[partnum].Production_Part_Number;
                if (Part_Number_Info.ContainsKey(manPartNum) == false) return false;
                return Part_Number_Info[manPartNum].Model.ToUpper().StartsWith("SE");
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = Brushes.Pink;
                PopupMessage = msg;
                PopupIsOpen = true;
                return false;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        public void Set_ContextMenu(TextBox tb)
        {
            if (LoginContext.User_Level < UserLevels.Advanced) return;
            foreach (MenuItem item in tb.ContextMenu.Items)
            {
                if ((String)(item.Header) == "Manual Entry")
                {
                    return;
                }
            }
            //tb.ContextMenu = new ContextMenu();
            MenuItem mi = new MenuItem();
            mi.Header = "Manual Entry";
            if (tb.Name == "CUABox")
            {
                mi.Click += CUA_Manual_Entry_Click;
            }
            else if (tb.Name == "CUBBox")
            {
                mi.Click += CUB_Manual_Entry_Click;
            }
            else if (tb.Name == "BSA_A_Box")
            {
                mi.Click += BSAA_Manual_Entry_Click;
            }
            else if (tb.Name == "BSA_B_Box")
            {
                mi.Click += BSAB_Manual_Entry_Click;
            }
            else if (tb.Name == "UI_SID_Box")
            {
                mi.Click += UISID_Manual_Entry_Click;
            }
            tb.ContextMenu.Items.Add(mi);
        }

        private void CUA_Manual_Entry_Click(object sender, RoutedEventArgs e)
        {
            Entry_Window ew = new Entry_Window();
            if (ew.ShowDialog() == true)
            {
                CUA_Serial = ew.dp_TextBoxText;
                if (Config_Type == ConfigurationTypes.Service_Start)
                {
                    SART_Common.Create_Event(WorkOrder_Events.Start_Config_Manual_Entry_CUA, Event_Statuses.Passed, message: ew.dp_TextBoxText);
                }
                else if (Config_Type == ConfigurationTypes.Service_Final)
                {
                    SART_Common.Create_Event(WorkOrder_Events.Final_Config_Manual_Entry_CUA, Event_Statuses.Passed, message: ew.dp_TextBoxText);
                }
                aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
            }
        }

        private void CUB_Manual_Entry_Click(object sender, RoutedEventArgs e)
        {
            Entry_Window ew = new Entry_Window();
            if (ew.ShowDialog() == true)
            {
                CUB_Serial = ew.dp_TextBoxText;
                if (Config_Type == ConfigurationTypes.Service_Start)
                {
                    SART_Common.Create_Event(WorkOrder_Events.Start_Config_Manual_Entry_CUB, Event_Statuses.Passed, message: ew.dp_TextBoxText);
                }
                else if (Config_Type == ConfigurationTypes.Service_Final)
                {
                    SART_Common.Create_Event(WorkOrder_Events.Final_Config_Manual_Entry_CUB, Event_Statuses.Passed, message: ew.dp_TextBoxText);
                }
                aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
            }
        }

        private void BSAA_Manual_Entry_Click(object sender, RoutedEventArgs e)
        {
            Entry_Window ew = new Entry_Window();
            if (ew.ShowDialog() == true)
            {
                BSA_A_Serial = ew.dp_TextBoxText;
                if (Config_Type == ConfigurationTypes.Service_Start)
                {
                    SART_Common.Create_Event(WorkOrder_Events.Start_Config_Manual_Entry_BSAA, Event_Statuses.Passed, message: ew.dp_TextBoxText);
                }
                else if (Config_Type == ConfigurationTypes.Service_Final)
                {
                    SART_Common.Create_Event(WorkOrder_Events.Final_Config_Manual_Entry_BSAA, Event_Statuses.Passed, message: ew.dp_TextBoxText);
                }
                aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
            }
        }

        private void BSAB_Manual_Entry_Click(object sender, RoutedEventArgs e)
        {
            Entry_Window ew = new Entry_Window();
            if (ew.ShowDialog() == true)
            {
                BSA_B_Serial = ew.dp_TextBoxText;
                if (Config_Type == ConfigurationTypes.Service_Start)
                {
                    SART_Common.Create_Event(WorkOrder_Events.Start_Config_Manual_Entry_BSAB, Event_Statuses.Passed, message: ew.dp_TextBoxText);
                }
                else if (Config_Type == ConfigurationTypes.Service_Final)
                {
                    SART_Common.Create_Event(WorkOrder_Events.Final_Config_Manual_Entry_BSAB, Event_Statuses.Passed, message: ew.dp_TextBoxText);
                }
                aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
            }
        }

        private void UISID_Manual_Entry_Click(object sender, RoutedEventArgs e)
        {
            Entry_Window ew = new Entry_Window();
            if (ew.ShowDialog() == true)
            {
                UIC_SID = ew.dp_TextBoxText;
                if (Config_Type == ConfigurationTypes.Service_Start)
                {
                    SART_Common.Create_Event(WorkOrder_Events.Start_Config_Manual_Entry_UICSID, Event_Statuses.Passed, message: ew.dp_TextBoxText);
                }
                else if (Config_Type == ConfigurationTypes.Service_Final)
                {
                    SART_Common.Create_Event(WorkOrder_Events.Final_Config_Manual_Entry_UICSID, Event_Statuses.Passed, message: ew.dp_TextBoxText);
                }
                aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
            }
        }



        ////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
