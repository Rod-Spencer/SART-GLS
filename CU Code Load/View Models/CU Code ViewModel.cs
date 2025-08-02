using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.COFF.Objects;
using Segway.Database.Objects;
using Segway.Login.Objects;
using Segway.Manufacturing.Objects;
using Segway.Modules.Controls.JTags;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.Login;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.CAN;
using Segway.Service.CAN.Objects;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using Segway.Service.Tools.COFF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Segway.Modules.SART.CodeLoad
{
    public partial class CU_Code_ViewModel : ViewModelBase, CU_Code_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator aggregator;

        static Dictionary<String, SART_Event_Log_Entry> EntryLog = new Dictionary<string, SART_Event_Log_Entry>();
        //private System.Timers.Timer JTag_Timer = null;

        private List<SART_JTag_Visibility> JTag_Visibilities = null;

        /// <summary>Contructor</summary>
        /// <param name="view">CU_Code_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public CU_Code_ViewModel(CU_Code_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.aggregator = eventAggregator;


            #region Event Subscriptions

            //this.aggregator.GetEvent<WorkOrder_Instance_Event>().Subscribe(Update_Work_Order_Data, true);
            aggregator.GetEvent<CU_Load_Code_EventID_Event>().Subscribe(Retrieve_EventID, true);
            aggregator.GetEvent<CU_Reset_Code_EventID_Event>().Subscribe(Retrieve_EventID, true);
            aggregator.GetEvent<SART_EventLog_Add_Event>().Subscribe(EventLog_Add, ThreadOption.UIThread, true);
            aggregator.GetEvent<SART_EventLog_Update_Event>().Subscribe(EventLog_Update, ThreadOption.UIThread, true);
            aggregator.GetEvent<WO_Config_Clear_Event>().Subscribe(Clear_Event, ThreadOption.UIThread, true);

            eventAggregator.GetEvent<SART_CU_CodeLoad_A_Event>().Subscribe(CUA_Load_Code_Handler, true);
            eventAggregator.GetEvent<SART_CU_CodeLoad_B_Event>().Subscribe(CUB_Load_Code_Handler, true);
            eventAggregator.GetEvent<SART_BSA_CodeLoad_A_Event>().Subscribe(BSAA_Load_Code_Handler, true);
            eventAggregator.GetEvent<SART_BSA_CodeLoad_B_Event>().Subscribe(BSAB_Load_Code_Handler, true);

            eventAggregator.GetEvent<SART_CU_CodeReset_A_Event>().Subscribe(CUA_Reset_Code_Handler, true);
            eventAggregator.GetEvent<SART_CU_CodeReset_B_Event>().Subscribe(CUB_Reset_Code_Handler, true);
            eventAggregator.GetEvent<SART_BSA_CodeReset_A_Event>().Subscribe(BSAA_Reset_Code_Handler, true);
            eventAggregator.GetEvent<SART_BSA_CodeReset_B_Event>().Subscribe(BSAB_Reset_Code_Handler, true);

            eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<WorkOrder_Opened_Event>().Subscribe(WorkOrder_Opened_Handler, true);

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Setups

            LoadCodeCommand = new DelegateCommand(CommandLoadCode, CanCommandLoadCode);
            LoadCodeNoticeCommand = new DelegateCommand(CommandLoadCodeNotice, CanCommandLoadCodeNotice);
            CancelNoticeCommand = new DelegateCommand(CommandCancelNotice, CanCommandCancelNotice);
            OpenPopupCommand = new DelegateCommand(CommandOpenPopup, CanCommandOpenPopup);

            #endregion
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

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

        /// <summary>Public Property - EventID</summary>
        public int EventID { get; set; }

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

        #region IsLoadChecked

        private Boolean _IsLoadChecked = true;

        /// <summary>Property IsLoadChecked of type Boolean</summary>
        public Boolean IsLoadChecked
        {
            get { return _IsLoadChecked; }
            set
            {
                _IsLoadChecked = value;
                ButtonText = value ? "Load" : "Reset";
                if (value == true) ((CU_Code_Control)View).jtagCtrl.dp_Load_Mode = Load_Actions.Load;
                OnPropertyChanged("IsLoadChecked");
            }
        }

        #endregion

        #region IsResetChecked

        private Boolean _IsResetChecked;

        /// <summary>Property IsResetChecked of type Boolean</summary>
        public Boolean IsResetChecked
        {
            get { return _IsResetChecked; }
            set
            {
                _IsResetChecked = value;
                ButtonText = value ? "Reset" : "Load";
                if (value == true) ((CU_Code_Control)View).jtagCtrl.dp_Load_Mode = Load_Actions.Reset;
                OnPropertyChanged("IsResetChecked");
            }
        }

        #endregion

        #region Load_CU

        private Boolean _Load_CU = true;

        /// <summary>Property Load_CU of type Boolean</summary>
        public Boolean Load_CU
        {
            get { return _Load_CU; }
            set
            {
                Is_AEnabled = Is_BEnabled = _Load_CU = value;
                OnPropertyChanged("Load_CU");
            }
        }

        #endregion

        #region Load_BSA

        private Boolean _Load_BSA = true;

        /// <summary>Property Load_BSA of type Boolean</summary>
        public Boolean Load_BSA
        {
            get { return _Load_BSA; }
            set
            {
                _Load_BSA = value;
                OnPropertyChanged("Load_BSA");
            }
        }

        #endregion

        #region Load_A

        private Boolean _Load_A = true;

        /// <summary>Property Load_A of type Boolean</summary>
        public Boolean Load_A
        {
            get { return _Load_A; }
            set
            {
                _Load_A = value;
                OnPropertyChanged("Load_A");
            }
        }

        #endregion

        #region Load_B

        private Boolean _Load_B = true;

        /// <summary>Property Load_B of type Boolean</summary>
        public Boolean Load_B
        {
            get { return _Load_B; }
            set
            {
                _Load_B = value;
                OnPropertyChanged("Load_B");
            }
        }

        #endregion

        #region Is_AEnabled

        /// <summary>Property Is_AEnabled of type Boolean</summary>
        public Boolean Is_AEnabled
        {
            get { return _Load_CU; }
            set { OnPropertyChanged("Is_AEnabled"); }
        }

        #endregion

        #region Is_BEnabled

        /// <summary>Property Is_BEnabled of type Boolean</summary>
        public Boolean Is_BEnabled
        {
            get { return _Load_CU; }
            set { OnPropertyChanged("Is_BEnabled"); }
        }

        #endregion

        #region ProgressLog

        private ObservableCollection<SART_Event_Log_Entry> _ProgressLog;

        /// <summary>Property ProgressLog of type List(SART_Event_Log_Entry)</summary>
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

        #region SelectedLogEntry

        private SART_Event_Log_Entry _SelectedLogEntry;

        /// <summary>Property SelectedLogEntry of type SART_Event_Log_Entry</summary>
        public SART_Event_Log_Entry SelectedLogEntry
        {
            get { return _SelectedLogEntry; }
            set
            {
                _SelectedLogEntry = value;
                OnPropertyChanged("SelectedLogEntry");
            }
        }

        #endregion

        #region LoadInProgress

        private Boolean _LoadInProgress;

        /// <summary>Property Is_CUB of type Boolean</summary>
        public Boolean LoadInProgress
        {
            get { return _LoadInProgress; }
            set
            {
                _LoadInProgress = value;
                OnPropertyChanged("LoadInProgress");
            }
        }

        #endregion

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

        #region CodeLoad_Notice_Popup_Open

        private Boolean _CodeLoad_Notice_Popup_Open;

        /// <summary>Property CodeLoad_Notice_Popup_Open of type Boolean</summary>
        public Boolean CodeLoad_Notice_Popup_Open
        {
            get { return _CodeLoad_Notice_Popup_Open; }
            set
            {
                _CodeLoad_Notice_Popup_Open = value;
                OnPropertyChanged("CodeLoad_Notice_Popup_Open");
            }
        }

        #endregion


        #region ButtonText

        private String _ButtonText = "Load";

        /// <summary>Property ButtonText of type String</summary>
        public String ButtonText
        {
            get { return _ButtonText; }
            set
            {
                _ButtonText = value;
                OnPropertyChanged("ButtonText");
            }
        }

        #endregion

        #region IsButtonEnabled

        private Boolean _IsButtonEnabled = true;

        /// <summary>Property IsButtonEnabled of type String</summary>
        public Boolean IsButtonEnabled
        {
            get { return _IsButtonEnabled; }
            set
            {
                _IsButtonEnabled = value;
                OnPropertyChanged("IsButtonEnabled");
            }
        }

        #endregion


        #region JTags

#if Exclude

        #region Use_CUA_JTag

        private Boolean _Use_CUA_JTag;

        /// <summary>Property Use_CUA_JTag of type Boolean</summary>
        public Boolean Use_CUA_JTag
        {
            get { return _Use_CUA_JTag; }
            set
            {
                _Use_CUA_JTag = value;
                OnPropertyChanged("Use_CUA_JTag");
            }
        }

        #endregion

        #region Use_CUB_JTag

        private Boolean _Use_CUB_JTag;

        /// <summary>Property Use_CUB_JTag of type Boolean</summary>
        public Boolean Use_CUB_JTag
        {
            get { return _Use_CUB_JTag; }
            set
            {
                _Use_CUB_JTag = value;
                OnPropertyChanged("Use_CUB_JTag");
            }
        }

        #endregion

        #region Use_BSA_JTag

        private Boolean _Use_BSA_JTag;

        /// <summary>Property Use_BSA_JTag of type Boolean</summary>
        public Boolean Use_BSA_JTag
        {
            get { return _Use_BSA_JTag; }
            set
            {
                _Use_BSA_JTag = value;
                OnPropertyChanged("Use_BSA_JTag");
            }
        }

        #endregion

        #region CUA_JTag

        private String _CUA_JTag;

        /// <summary>Property CUA_JTag of type String</summary>
        public String CUA_JTag
        {
            get { return _CUA_JTag; }
            set
            {
                _CUA_JTag = value;
                OnPropertyChanged("CUA_JTag");
            }
        }

        #endregion

        #region CUB_JTag

        private String _CUB_JTag;

        /// <summary>Property CUB_JTag of type String</summary>
        public String CUB_JTag
        {
            get { return _CUB_JTag; }
            set
            {
                _CUB_JTag = value;
                OnPropertyChanged("CUB_JTag");
            }
        }

        #endregion

        #region BSA_JTag

        private String _BSA_JTag;

        /// <summary>Property BSA_JTag of type String</summary>
        public String BSA_JTag
        {
            get { return _BSA_JTag; }
            set
            {
                _BSA_JTag = value;
                OnPropertyChanged("BSA_JTag");
            }
        }

        #endregion


        #region CUA_Serial

        private String _CUA_Serial;

        /// <summary>Property CUA_Serial of type String</summary>
        public String CUA_Serial
        {
            get { return _CUA_Serial; }
            set
            {
                _CUA_Serial = value;
                OnPropertyChanged("CUA_Serial");
                CUAJTagCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region CUB_Serial

        private String _CUB_Serial;

        /// <summary>Property CUB_Serial of type String</summary>
        public String CUB_Serial
        {
            get { return _CUB_Serial; }
            set
            {
                _CUB_Serial = value;
                OnPropertyChanged("CUB_Serial");
                CUBJTagCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region BSA_Serial

        private String _BSA_Serial;

        /// <summary>Property BSA_Serial of type String</summary>
        public String BSA_Serial
        {
            get { return _BSA_Serial; }
            set
            {
                _BSA_Serial = value;
                OnPropertyChanged("BSA_Serial");
                BSAJTagCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

#endif

        #region JTag_Visibility

        /// <summary>Property JTag_Visibility of type Visibility</summary>
        public Visibility JTag_Visibility
        {
            get
            {
                if (LoginContext == null) return Visibility.Collapsed;
                if (LoginContext.User_Level >= UserLevels.Expert) return Visibility.Visible;
                if (InfrastructureModule.Current_Work_Order == null) return Visibility.Collapsed;
                if (JTag_Visibilities == null) return Visibility.Collapsed;
                if (JTag_Visibilities.Count == 0) return Visibility.Collapsed;
                foreach (var vis in JTag_Visibilities)
                {
                    if (String.Compare(vis.User_Name, LoginContext.UserName, true) != 0) continue;
                    if (String.Compare(vis.Work_Order_Number, InfrastructureModule.Current_Work_Order.Work_Order_ID, true) != 0) continue;
                    if ((vis.Date_Time_Start.HasValue == true) && (DateTime.Compare(vis.Date_Time_Start.Value, DateTime.Now) > 0)) continue;
                    if ((vis.Date_Time_End.HasValue == true) && (DateTime.Compare(vis.Date_Time_End.Value, DateTime.Now) < 0)) continue;
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
            set
            {
                OnPropertyChanged("JTag_Visibility");
            }
        }

        #endregion


        //#region LoadMode

        ///// <summary>Property LoadMode of type Load_Actions</summary>
        //public Load_Actions LoadMode
        //{
        //    get
        //    {
        //        return ((CU_Code_Control)View).jtagCtrl.dp_Load_Mode;
        //    }
        //    set
        //    {
        //        ((CU_Code_Control)View).jtagCtrl.dp_Load_Mode = value;
        //        OnPropertyChanged("LoadMode");
        //    }
        //}

        //#endregion


        #region LoadMode

        private Load_Actions _LoadMode;

        /// <summary>Property LoadMode of type Load_Actions</summary>
        public Load_Actions LoadMode
        {
            get { return _LoadMode; }
            set
            {
                _LoadMode = value;
                OnPropertyChanged("LoadMode");
            }
        }

        #endregion

        #region JTagsData

        /// <summary>Property JTagsData of type JTag_Data</summary>
        public JTag_Data JTagsData
        {
            get
            {
                //if (View == null) return null;

                //Application.Current.Dispatcher.Invoke((Action)delegate ()
                //{
                //    // Create an instance of the control if it doesn't exist
                //    if (((CU_Code_Control)View).jtagCtrl == null) ((CU_Code_Control)View).jtagCtrl = new JTags_Control();
                //    JTags_Control jtags = ((CU_Code_Control)View).jtagCtrl;
                //    if (jtags.dp_JTags == null)
                //    {
                //        // Assign the public static instance of the JTag_Data to the control
                //        jtags.dp_JTags = CAN2_Commands.JTags;
                //    }
                //});

                return CAN2_Commands.JTags;
            }
            set
            {
                try
                {
                    CAN2_Commands.JTags = value;

                    //if (View == null) return;

                    //Application.Current.Dispatcher.Invoke((Action)delegate ()
                    //{
                    //    if (((CU_Code_Control)View).jtagCtrl == null) ((CU_Code_Control)View).jtagCtrl = new JTags_Control();
                    //    JTags_Control jtags = ((CU_Code_Control)View).jtagCtrl;
                    //    jtags.dp_JTags = CAN2_Commands.JTags;
                    //});
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString(ex));
                    //throw;
                }
                finally
                {
                    OnPropertyChanged("JTagsData");
                }
            }
        }

        #endregion

        #endregion


        #region NoteVisibility

        private Visibility _NoteVisibility;

        /// <summary>Property NoteVisibility of type Visibility</summary>
        public Visibility NoteVisibility
        {
            get { return _NoteVisibility; }
            set
            {
                _NoteVisibility = value;
                OnPropertyChanged("NoteVisibility");
            }
        }

        #endregion

        #region CodeLoad_Note

        private String _CodeLoad_Note = "Be certain battery power is sufficient before loading code.";

        /// <summary>Property CodeLoad_Note of type String</summary>
        public String CodeLoad_Note
        {
            get { return _CodeLoad_Note; }
            set
            {
                _CodeLoad_Note = value;
                OnPropertyChanged("CodeLoad_Note");
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
                    _Header_Image = Image_Helper.ImageFromEmbedded(".Images.loadcode.png");
                }
                return _Header_Image;
            }
            set
            {
                OnPropertyChanged("Header_Image");
            }
        }

        #endregion

        /////////////////////////////////////////////////////////////////
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

        #region PopupMessageCUA

        private String _PopupMessageCUA;

        /// <summary>Property PopupMessageCUA of type String</summary>
        public String PopupMessageCUA
        {
            get { return _PopupMessageCUA; }
            set
            {
                _PopupMessageCUA = value;
                OnPropertyChanged("PopupMessageCUA");
            }
        }

        #endregion

        #region PopupMessageCUB

        private String _PopupMessageCUB;

        /// <summary>Property PopupMessageCUB of type String</summary>
        public String PopupMessageCUB
        {
            get { return _PopupMessageCUB; }
            set
            {
                _PopupMessageCUB = value;
                OnPropertyChanged("PopupMessageCUB");
            }
        }

        #endregion

        #region PopupMessageBSAA

        private String _PopupMessageBSAA;

        /// <summary>Property PopupMessageBSAA of type String</summary>
        public String PopupMessageBSAA
        {
            get { return _PopupMessageBSAA; }
            set
            {
                _PopupMessageBSAA = value;
                OnPropertyChanged("PopupMessageBSAA");
            }
        }

        #endregion

        #region PopupMessageBSAB

        private String _PopupMessageBSAB;

        /// <summary>Property PopupMessageBSAB of type String</summary>
        public String PopupMessageBSAB
        {
            get { return _PopupMessageBSAB; }
            set
            {
                _PopupMessageBSAB = value;
                OnPropertyChanged("PopupMessageBSAB");
            }
        }

        #endregion

        #region PopupMessageCUA_Color

        private Brush _PopupMessageCUA_Color;

        /// <summary>Property PopupMessageCUA_Color of type Brush</summary>
        public Brush PopupMessageCUA_Color
        {
            get { return _PopupMessageCUA_Color; }
            set
            {
                _PopupMessageCUA_Color = value;
                OnPropertyChanged("PopupMessageCUA_Color");
            }
        }

        #endregion

        #region PopupMessageCUB_Color

        private Brush _PopupMessageCUB_Color;

        /// <summary>Property PopupMessageCUB_Color of type Brush</summary>
        public Brush PopupMessageCUB_Color
        {
            get { return _PopupMessageCUB_Color; }
            set
            {
                _PopupMessageCUB_Color = value;
                OnPropertyChanged("PopupMessageCUB_Color");
            }
        }

        #endregion

        #region PopupMessageBSAA_Color

        private Brush _PopupMessageBSAA_Color;

        /// <summary>Property PopupMessageBSAA_Color of type Brush</summary>
        public Brush PopupMessageBSAA_Color
        {
            get { return _PopupMessageBSAA_Color; }
            set
            {
                _PopupMessageBSAA_Color = value;
                OnPropertyChanged("PopupMessageBSAA_Color");
            }
        }

        #endregion

        #region PopupMessageBSAB_Color

        private Brush _PopupMessageBSAB_Color;

        /// <summary>Property PopupMessageBSAB_Color of type Brush</summary>
        public Brush PopupMessageBSAB_Color
        {
            get { return _PopupMessageBSAB_Color; }
            set
            {
                _PopupMessageBSAB_Color = value;
                OnPropertyChanged("PopupMessageBSAB_Color");
            }
        }

        #endregion

        #region PopupMessageCUA_Visibilty

        private Visibility _PopupMessageCUA_Visibilty;

        /// <summary>Property PopupMessageCUA_Visibilty of type Visibility</summary>
        public Visibility PopupMessageCUA_Visibilty
        {
            get { return _PopupMessageCUA_Visibilty; }
            set
            {
                _PopupMessageCUA_Visibilty = value;
                OnPropertyChanged("PopupMessageCUA_Visibilty");
            }
        }

        #endregion

        #region PopupMessageCUB_Visibilty

        private Visibility _PopupMessageCUB_Visibilty;

        /// <summary>Property PopupMessageCUB_Visibilty of type Visibility</summary>
        public Visibility PopupMessageCUB_Visibilty
        {
            get { return _PopupMessageCUB_Visibilty; }
            set
            {
                _PopupMessageCUB_Visibilty = value;
                OnPropertyChanged("PopupMessageCUB_Visibilty");
            }
        }

        #endregion

        #region PopupMessageBSAA_Visibilty

        private Visibility _PopupMessageBSAA_Visibilty;

        /// <summary>Property PopupMessageBSAA_Visibilty of type Visibility</summary>
        public Visibility PopupMessageBSAA_Visibilty
        {
            get { return _PopupMessageBSAA_Visibilty; }
            set
            {
                _PopupMessageBSAA_Visibilty = value;
                OnPropertyChanged("PopupMessageBSAA_Visibilty");
            }
        }

        #endregion

        #region PopupMessageBSAB_Visibilty

        private Visibility _PopupMessageBSAB_Visibilty;

        /// <summary>Property PopupMessageBSAB_Visibilty of type Visibility</summary>
        public Visibility PopupMessageBSAB_Visibilty
        {
            get { return _PopupMessageBSAB_Visibilty; }
            set
            {
                _PopupMessageBSAB_Visibilty = value;
                OnPropertyChanged("PopupMessageBSAB_Visibilty");
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

        private Brush _PopupColor = Brushes.LightGoldenrodYellow;

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
        /////////////////////////////////////////////////////////////////

        #region Code Load Popup Controls

        #region CodeLoad_PopupMessage

        private String _CodeLoad_PopupMessage;

        /// <summary>ViewModel Property: CodeLoad_PopupMessage of type: String</summary>
        public String CodeLoad_PopupMessage
        {
            get { return _CodeLoad_PopupMessage; }
            set
            {
                _CodeLoad_PopupMessage = value;
                OnPropertyChanged("CodeLoad_PopupMessage");
            }
        }

        #endregion

        #region CodeLoad PopupOpen

        private Boolean _CodeLoad_PopupOpen;

        /// <summary>ViewModel Property: CodeLoad_PopupOpen of type: Boolean</summary>
        public Boolean CodeLoad_PopupOpen
        {
            get { return _CodeLoad_PopupOpen; }
            set
            {
                _CodeLoad_PopupOpen = value;
                OnPropertyChanged("CodeLoad_PopupOpen");
            }
        }

        #endregion

        #region CodeLoad PopupColor

        private Brush _CodeLoad_PopupColor;

        /// <summary>ViewModel Property: CodeLoad_PopupColor of type: Brush</summary>
        public Brush CodeLoad_PopupColor
        {
            get { return _CodeLoad_PopupColor; }
            set
            {
                _CodeLoad_PopupColor = value;
                OnPropertyChanged("CodeLoad_PopupColor");
            }
        }

        #endregion

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region LoadCodeNoticeCommand

        /// <summary>Delegate Command: LoadCodeNoticeCommand</summary>
        public DelegateCommand LoadCodeNoticeCommand { get; set; }

        private Boolean CanCommandLoadCodeNotice()
        {
            if (InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write) return false;
            return true;
        }
        private void CommandLoadCodeNotice()
        {
            // form button bound to this command
            if (User_Info.User_Level >= UserLevels.Expert)
            {
                // go ahead and run load code
                CommandLoadCode();
            }
            else
            {
                // need to launch popup for code load
                CodeLoad_Notice_Popup_Open = true;
            }
        }

        #endregion

        #region CancelNoticeCommand

        /// <summary>Delegate Command: CancelNoticeCommand</summary>
        public DelegateCommand CancelNoticeCommand { get; set; }

        private Boolean CanCommandCancelNotice() { return true; }
        private void CommandCancelNotice()
        {
            // close the popup and do nothing
            CodeLoad_Notice_Popup_Open = false;
        }

        #endregion

        #region LoadCodeCommand

        /// <summary>Delegate Command: LoadCodeCommand</summary>
        public DelegateCommand LoadCodeCommand { get; set; }

        private Boolean CanCommandLoadCode() { return true; }
        private void CommandLoadCode()
        {
            ProgressLog.Clear();
            Thread thread = new Thread(new ThreadStart(CommandLoadCode_Backup));
            thread.IsBackground = true;
            thread.Start();
        }

        #endregion

#if Exclude

        #region CUAJTagCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: CUAJTagCommand</summary>
        public DelegateCommand CUAJTagCommand { get; set; }
        private Boolean CanCommandCUAJTag()
        {
            return String.IsNullOrEmpty(CUA_Serial) == false;
        }

        private void CommandCUAJTag()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                var cuaJTags = SART_2012_Web_Service_Client.Select_Stage1_CU_Security_CU_SERIAL_NUMBER(Token, CUA_Serial);
                if ((cuaJTags != null) && (cuaJTags.Count > 0))
                {
                    String jtag = cuaJTags[cuaJTags.Count - 1].CU_JTag_Lock;
                    logger.Debug("Found JTag for CU-A: {0}", jtag);
                    CUA_JTag = jtag;
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                CodeLoad_PopupColor = Brushes.Pink;
                CodeLoad_PopupMessage = msg;
                CodeLoad_PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion

        #region CUBJTagCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: CUBJTagCommand</summary>
        public DelegateCommand CUBJTagCommand { get; set; }
        private Boolean CanCommandCUBJTag()
        {
            return String.IsNullOrEmpty(CUB_Serial) == false;
        }

        private void CommandCUBJTag()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                var cubJTags = SART_2012_Web_Service_Client.Select_Stage1_CU_Security_CU_SERIAL_NUMBER(Token, CUB_Serial);
                if ((cubJTags != null) && (cubJTags.Count > 0))
                {
                    String jtag = cubJTags[cubJTags.Count - 1].CU_JTag_Lock;
                    logger.Debug("Found JTag for CU-B: {0}", jtag);
                    CUB_JTag = jtag;
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                CodeLoad_PopupColor = Brushes.Pink;
                CodeLoad_PopupMessage = msg;
                CodeLoad_PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion

        #region BSAJTagCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BSAJTagCommand</summary>
        public DelegateCommand BSAJTagCommand { get; set; }
        private Boolean CanCommandBSAJTag()
        {
            return String.IsNullOrEmpty(BSA_Serial) == false;
        }

        private void CommandBSAJTag()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                var bsaJTags = SART_2012_Web_Service_Client.Select_Stage1_BSA_Security_BSA_SERIAL_NUMBER(Token, BSA_Serial);
                if ((bsaJTags != null) && (bsaJTags.Count > 0))
                {
                    String jtag = bsaJTags[bsaJTags.Count - 1].BSA_JTag_Lock;
                    logger.Debug("Found JTag for BSA: {0}", jtag);
                    BSA_JTag = jtag;
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                CodeLoad_PopupColor = Brushes.Pink;
                CodeLoad_PopupMessage = msg;
                CodeLoad_PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion

        #region GetJTagCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: GetJTagCommand</summary>
        public DelegateCommand GetJTagCommand { get; set; }
        private Boolean CanCommandGetJTag() { return true; }
        private void CommandGetJTag()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Security sec = Get_Security_Data(InfrastructureModule.Current_Work_Order.PT_Serial);
                CUA_JTag = sec.CU_A_JTag_Lock;
                CUB_JTag = sec.CU_B_JTag_Lock;
                BSA_JTag = sec.BSA_JTag_Lock;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                CodeLoad_PopupColor = Brushes.Pink;
                CodeLoad_PopupMessage = msg;
                CodeLoad_PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion

        #region DefJTagCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: DefJTagCommand</summary>
        public DelegateCommand DefJTagCommand { get; set; }
        private Boolean CanCommandDefJTag() { return true; }
        private void CommandDefJTag()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                String def = (-1L).ToString("X16");
                CUA_JTag = def;
                CUB_JTag = def;
                BSA_JTag = def;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                CodeLoad_PopupColor = Brushes.Pink;
                CodeLoad_PopupMessage = msg;
                CodeLoad_PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion

        #region ClrJTagCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClrJTagCommand</summary>
        public DelegateCommand ClrJTagCommand { get; set; }
        private Boolean CanCommandClrJTag() { return true; }
        private void CommandClrJTag()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                CUA_JTag = null;
                CUB_JTag = null;
                BSA_JTag = null;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                CodeLoad_PopupColor = Brushes.Pink;
                CodeLoad_PopupMessage = msg;
                CodeLoad_PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion

        #region SwpJTagCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: SwpJTagCommand</summary>
        public DelegateCommand SwpJTagCommand { get; set; }
        private Boolean CanCommandSwpJTag() { return true; }
        private void CommandSwpJTag()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                String temp = CUA_JTag;
                CUA_JTag = CUB_JTag;
                CUB_JTag = temp;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = Brushes.Pink;
                PopupMessage = msg;
                PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion

#endif


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

                Thread back = new Thread(new ThreadStart(CommandOpenPopup_Back));
                back.IsBackground = true;
                back.Start();
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = Brushes.Pink;
                PopupMessage = msg;
                PopupOpen = true;
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

        //private void Update_Work_Order_Data(SART_Work_Order wo)
        //{
        //    Current_Work_Order = wo;
        //    this.Work_Order_Num = Current_Work_Order.Work_Order_ID;
        //    this.PTSerial = Current_Work_Order.PT_Serial;
        //}

        private void Retrieve_EventID(int id)
        {
            EventID = id;
        }

        private void EventLog_Add(SART_Event_Log_Entry log)
        {
            if (log.Object_ID == EventID)
            {
                ProgressLog.Add(log);
                SelectedLogEntry = log;
                //System.Windows.Forms.Application.DoEvents();
            }
        }

        private void EventLog_Update(SART_Event_Log_Entry log)
        {
            if (log.Object_ID == EventID)
            {
                ProgressLog.Remove(log);
                ProgressLog.Add(log);
                SelectedLogEntry = log;
            }
        }

        private void Clear_Event(Boolean clearRequested)
        {
            if (clearRequested)
            {
                if (ProgressLog != null)
                {
                    ProgressLog.Clear();
                }
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region CUA_Load_Code_Handler  -- CUA_Load_Code_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void CUA_Load_Code_Handler(Boolean status)
        {
            if (status)
            {
                PopupMessageCUA = "CU-A Load Code PASSED";
                PopupMessageCUA_Color = Brushes.Green;
            }
            else
            {
                PopupMessageCUA = "CU-A Load Code FAILED";
                PopupMessageCUA_Color = Brushes.Red;
            }
            PopupMessageCUA_Visibilty = Visibility.Visible;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region CUB_Load_Code_Handler  -- CUA_Load_Code_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void CUB_Load_Code_Handler(Boolean status)
        {
            if (status)
            {
                PopupMessageCUB = "CU-B Load Code PASSED";
                PopupMessageCUB_Color = Brushes.Green;
            }
            else
            {
                PopupMessageCUB = "CU-B Load Code FAILED";
                PopupMessageCUB_Color = Brushes.Red;
            }
            PopupMessageCUB_Visibilty = Visibility.Visible;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BSAA_Load_Code_Handler  -- BSAA_Load_Code_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BSAA_Load_Code_Handler(Boolean status)
        {
            if (status)
            {
                PopupMessageBSAA = "BSA-A Load Code PASSED";
                PopupMessageBSAA_Color = Brushes.Green;
            }
            else
            {
                PopupMessageBSAA = "BSA-A Load Code FAILED";
                PopupMessageBSAA_Color = Brushes.Red;
            }
            PopupMessageBSAA_Visibilty = Visibility.Visible;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BSAB_Load_Code_Handler  -- BSAB_Load_Code_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BSAB_Load_Code_Handler(Boolean status)
        {
            if (status)
            {
                PopupMessageBSAB = "BSA-B Load Code PASSED";
                PopupMessageBSAB_Color = Brushes.Green;
            }
            else
            {
                PopupMessageBSAB = "BSA-B Load Code FAILED";
                PopupMessageBSAB_Color = Brushes.Red;
            }
            PopupMessageBSAB_Visibilty = Visibility.Visible;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region CUA_Reset_Code_Handler  -- SART_CU_CodeReset_A_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void CUA_Reset_Code_Handler(Boolean status)
        {
            if (status)
            {
                PopupMessageCUA = "CU-A Reset Code PASSED";
                PopupMessageCUA_Color = Brushes.Green;
            }
            else
            {
                PopupMessageCUA = "CU-A Reset Code FAILED";
                PopupMessageCUA_Color = Brushes.Red;
            }
            PopupMessageCUA_Visibilty = Visibility.Visible;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region CUB_Reset_Code_Handler  -- SART_CU_CodeReset_B_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void CUB_Reset_Code_Handler(Boolean status)
        {
            if (status)
            {
                PopupMessageCUB = "CU-B Reset Code PASSED";
                PopupMessageCUB_Color = Brushes.Green;
            }
            else
            {
                PopupMessageCUB = "CU-B Reset Code FAILED";
                PopupMessageCUB_Color = Brushes.Red;
            }
            PopupMessageCUB_Visibilty = Visibility.Visible;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BSAA_Reset_Code_Handler  -- SART_BSA_CodeReset_A_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BSAA_Reset_Code_Handler(Boolean status)
        {
            if (status)
            {
                PopupMessageBSAA = "BSA-A Reset Code PASSED";
                PopupMessageBSAA_Color = Brushes.Green;
            }
            else
            {
                PopupMessageBSAA = "BSA-A Reset Code FAILED";
                PopupMessageBSAA_Color = Brushes.Red;
            }
            PopupMessageBSAA_Visibilty = Visibility.Visible;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BSAB_Reset_Code_Handler  -- SART_BSA_CodeReset_B_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BSAB_Reset_Code_Handler(Boolean status)
        {
            if (status)
            {
                PopupMessageBSAB = "BSA-B Reset Code PASSED";
                PopupMessageBSAB_Color = Brushes.Green;
            }
            else
            {
                PopupMessageBSAB = "BSA-B Reset Code FAILED";
                PopupMessageBSAB_Color = Brushes.Red;
            }
            PopupMessageBSAB_Visibilty = Visibility.Visible;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Close_Handler  -- Event: SART_WorkOrder_Close_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Close_Handler(Boolean closed)
        {
            if (closed == true)
            {
                Reset();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Cancel_Handler  -- Event: SART_WorkOrder_Cancel_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Cancel_Handler(Boolean canceled)
        {
            if (canceled == true)
            {
                Reset();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Opened_Handler  -- Event: WorkOrder_Opened_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Opened_Handler(Boolean open)
        {
            JTagsData = null;
            logger.Debug("JTagsData have been cleared");

            //JTag_Timer_Elapsed(null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        /// <summary>Public Method - IsNavigationTarget</summary>
        /// <param name="navigationContext">NavigationContext</param>
        /// <returns>bool</returns>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <summary>Public Method - OnNavigatedFrom</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //aggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            aggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>(CU_Code_Load_Module.ToolBar_Name, CU_Code_Control.Control_Name));
            //JTag_Timer.Stop();
            //JTag_Timer = null;
        }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            aggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            aggregator.GetEvent<ToolBar_Selection_Event>().Publish(CU_Code_Load_Module.ToolBar_Name);
            OnPropertyChanged("Work_Order_Num");
            OnPropertyChanged("PTSerial");
            OnPropertyChanged("WorkOrderColor");
            OnPropertyChanged("JTag_Visibility");
            if (User_Info.User_Level >= UserLevels.Expert)
            {
                // just display the legal text notice on the bottom of page
                NoteVisibility = Visibility.Visible;
            }
            else
            {
                // will need to launch popup for code load
                NoteVisibility = Visibility.Collapsed;
            }
            LoadCodeNoticeCommand.RaiseCanExecuteChanged();
            ((JTags_ViewModel)((CU_Code_Control)View).jtagCtrl.ViewModel).GetJTagCommand.RaiseCanExecuteChanged();
            if (Load_A == true) LoadMode = Load_Actions.Load;
            else if (Load_B == true) LoadMode = Load_Actions.Reset;
            else LoadMode = Load_Actions.Unknown;

            aggregator.GetEvent<JTag_Display_Controls_Event>().Publish(true);

            //if (JTag_Timer == null)
            //{
            //    JTag_Timer = new System.Timers.Timer();
            //    JTag_Timer.Interval = 30000;
            //    JTag_Timer.Elapsed += new System.Timers.ElapsedEventHandler(JTag_Timer_Elapsed);
            //    JTag_Timer.Enabled = true;
            //    JTag_Timer.Start();
            //}
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private void Reset()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                IsLoadChecked = true;
                IsResetChecked = false;
                Load_CU = true;
                Load_BSA = true;
                Load_A = true;
                Load_B = true;
                ProgressLog = null;
                SelectedLogEntry = null;
                _LoadInProgress = false;
                ButtonText = "Load";
                IsButtonEnabled = true;
                NoteVisibility = Visibility.Collapsed;
                CodeLoad_Note = "Be certain battery power is sufficient before loading code.";
                OnPropertyChanged("Work_Order_Num");
                OnPropertyChanged("PTSerial");
                OnPropertyChanged("WorkOrderColor");

                aggregator.GetEvent<JTag_Clear_Controls_Event>().Publish(true);
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

        private void CommandLoadCode_Backup()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                PopupMessageCUA_Visibilty = PopupMessageCUB_Visibilty = PopupMessageBSAA_Visibilty = PopupMessageBSAB_Visibilty = Visibility.Collapsed;
                PopupMessageCUA = PopupMessageCUB = PopupMessageBSAA = PopupMessageBSAB = null;
                // close the popup if it is open, or for experts hide the notice
                CodeLoad_Notice_Popup_Open = false;
                NoteVisibility = Visibility.Collapsed;
                aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                LoadInProgress = true;

                logger.Debug("Storing User Settings to CAN2_Commands");
                CAN2_Commands.Continue_Processing = true;

                //SART_Common.Initialize_Timeouts_and_Delays();

                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    if (((CU_Code_Control)View).jtagCtrl == null) ((CU_Code_Control)View).jtagCtrl = new JTags_Control();
                    if (((CU_Code_Control)View).jtagCtrl.dp_JTags == null) ((CU_Code_Control)View).jtagCtrl.dp_JTags = CAN2_Commands.JTags;

                    //CAN2_Commands._JTags = CAN2_Commands.JTags;
                    //((CU_Code_Control)View).jtagCtrl.Update_JTags(ref CAN2_Commands._JTags);
                });

                IsButtonEnabled = false;
                var partList = Manufacturing_S1PN_Web_Service_Client_REST.Select_Stage1_Partnum_UNIT_ID_SERIAL_NUMBER(InfrastructureModule.Token, InfrastructureModule.Current_Work_Order.PT_Serial);
                if ((partList == null) || (partList.Count == 0))
                {
                    throw new Exception("Request for code type returned a null or empty list");
                }
                Stage1_Partnum pn = new Stage1_Partnum();
                foreach (var part in partList)
                {
                    pn.Update(part);
                }

                PT_Models modelType = Get_Model_Type(pn.Unit_ID_Partnumber);
                if (modelType == PT_Models.NotDefined)
                {
                    throw new Exception(String.Format("Invalid PT Model: {0}", pn.Unit_ID_Partnumber));
                }

                Security sec = SART_Common.Get_Security_Data(InfrastructureModule.Current_Work_Order.PT_Serial);


                if (Load_CU == true)
                {
                    if (IsLoadChecked == true)
                    {
                        CAN2_Commands.JTags.Set_For_Load(sec);

                        COFF_Descriptor key = new COFF_Descriptor(PT_Generations.Gen2, PT_Component.CU, modelType, PT_Code_Type.Application);
                        if (CAN2_Commands.Loaded_COFF_Files.ContainsKey(key.Description) == false)
                        {
                            ///////////////////////////////////
                            // Load CU COFF file
                            logger.Debug("Loading CU COFF Image");
                            //SqlBooleanList criteria = new SqlBooleanList();
                            //criteria.Add(new FieldData("Generation", PT_Generations.Gen2.ToString()));
                            //criteria.Add(new FieldData("Model", modelType.ToString(), SegwayFieldTypes.StringInsensitive));
                            //criteria.Add(new FieldData("Component", "CU"));
                            //criteria.Add(new FieldData("Type", PT_Code_Type.Application.ToString()));
                            SqlBooleanCriteria criteria = SqlBooleanCriteria.Create_Criteria(key);

                            List<SART_COFF_Files> coffList = SART_COFF_Web_Service_Client_REST.Select_SART_COFF_Files_Criteria(InfrastructureModule.Token, criteria);
                            if ((coffList == null) || (coffList.Count == 0))
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to retrieve COFF file");
                                return;
                            }
                            CAN2_Commands.CU_COFF = coffList[coffList.Count - 1];
                            CAN2_Commands.Loaded_COFF_Files[key.Description] = CAN2_Commands.CU_COFF.Data;
                            if (COFF_File.Load(CAN2_Commands.CU_COFF.Data, key) == false)
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                                return;
                            }
                            // Load CU COFF file
                            ///////////////////////////////////
                        }
                        else if (COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key) == false)
                        {
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                            return;
                        }
                        logger.Debug("Loading COFF Image: {0}, Size: {1}", key, CAN2_Commands.Loaded_COFF_Files[key.Description].Length);

                        //if (COFF_File.Load(CAN2_Commands.CU_COFF.Data, PT_Generations.Gen2.ToString(), "CU", pn.Unit_ID_Partnumber.ToUpper(), "Application") == false)
                        //{
                        //    aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                        //    return;
                        //}

                        String variable = "bsa_software_version";
                        int address = (int)COFF_File.ResolveValue(variable);
                        logger.Debug("Resolved: {0} - {1}", variable, address);



                        Boolean status = true;
                        ///////////////////////////////////
                        // Loading to Side - A
                        if (Load_A == true)
                        {
                            CAN2_Commands.cuManufacturingData = null;

                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Loading Code for CU-A");
                            status = Load_CU_Code(CAN_CU_Sides.A, CAN2_Commands.JTags.CUA);
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Loading Code for CU-A - {0}", status ? "Passed" : "Failed"));
                            aggregator.GetEvent<SART_CU_CodeLoad_A_Event>().Publish(status);
                            //if (status == false) return;


                            if ((status == true) && (CAN2_Commands.cuManufacturingData != null) && (String.IsNullOrEmpty(CAN2_Commands.cuManufacturingData.CU_Serial_Number) == false))
                            {
                                ////////////////////////////////////////////////////////////////////////////
                                // Updating Stage1_CU_Security
                                logger.Debug("Updating Stage1_CU_Security - Searching CU Serial: {0}", CAN2_Commands.cuManufacturingData.CU_Serial_Number);
                                var cuSecList = Manufacturing_S1CUSec_Web_Service_Client_REST.Select_Stage1_CU_Security_CU_SERIAL_NUMBER_All(InfrastructureModule.Token, CAN2_Commands.cuManufacturingData.CU_Serial_Number);
                                Boolean found = false;
                                if (cuSecList != null)
                                {
                                    if (cuSecList.Count > 0)
                                    {
                                        if (cuSecList[cuSecList.Count - 1].CU_JTag_Lock == CAN2_Commands.JTags.CUA.JTag_Lock)
                                        {
                                            found = true;
                                        }
                                    }
                                    if (found == false)
                                    {
                                        Stage1_CU_Security cusec = new Stage1_CU_Security();
                                        cusec.CU_JTag_Lock = CAN2_Commands.JTags.CUA.JTag_Lock;
                                        cusec.Date_Time_Entered = DateTime.Now;
                                        cusec.Date_Time_Updated = DateTime.Now;
                                        cusec.CU_Serial_Number = CAN2_Commands.cuManufacturingData.CU_Serial_Number;
                                        if (Manufacturing_S1CUSec_Web_Service_Client_REST.Insert_Stage1_CU_Security_Key(InfrastructureModule.Token, cusec) == null)
                                        {
                                            throw new Exception(String.Format("Updating Stage1_CU_Security - Failed\n\nHowever, the code loading process was successful."));
                                        }
                                    }
                                }
                                logger.Debug("Updating Stage1_CU_Security - Successful");
                            }
                        }
                        // Loading to Side - A
                        ///////////////////////////////////


                        ///////////////////////////////////
                        // Loading to Side - B
                        if (Load_B == true)
                        {
                            CAN2_Commands.cuManufacturingData = null;

                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Loading Code for CU-B");
                            status = Load_CU_Code(CAN_CU_Sides.B, CAN2_Commands.JTags.CUB);
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Loading Code for CU-B - {0}", status ? "Passed" : "Failed"));
                            aggregator.GetEvent<SART_CU_CodeLoad_B_Event>().Publish(status);

                            if ((status == true) && (CAN2_Commands.cuManufacturingData != null) && (String.IsNullOrEmpty(CAN2_Commands.cuManufacturingData.CU_Serial_Number) == false))
                            {
                                ////////////////////////////////////////////////////////////////////////////
                                // Updating Stage1_CU_Security
                                logger.Debug("Updating Stage1_CU_Security - Searching CU Serial: {0}", CAN2_Commands.cuManufacturingData.CU_Serial_Number);
                                var cuSecList = Manufacturing_S1CUSec_Web_Service_Client_REST.Select_Stage1_CU_Security_CU_SERIAL_NUMBER_All(InfrastructureModule.Token, CAN2_Commands.cuManufacturingData.CU_Serial_Number);
                                Boolean found = false;
                                if (cuSecList != null)
                                {
                                    if (cuSecList.Count > 0)
                                    {
                                        if (cuSecList[cuSecList.Count - 1].CU_JTag_Lock == CAN2_Commands.JTags.CUB.JTag_Lock)
                                        {
                                            found = true;
                                        }
                                    }
                                    if (found == false)
                                    {
                                        Stage1_CU_Security cusec = new Stage1_CU_Security();
                                        cusec.CU_JTag_Lock = CAN2_Commands.JTags.CUB.JTag_Lock;
                                        cusec.Date_Time_Entered = DateTime.Now;
                                        cusec.Date_Time_Updated = DateTime.Now;
                                        cusec.CU_Serial_Number = CAN2_Commands.cuManufacturingData.CU_Serial_Number;
                                        if (Manufacturing_S1CUSec_Web_Service_Client_REST.Insert_Stage1_CU_Security_Key(InfrastructureModule.Token, cusec) == null)
                                        {
                                            throw new Exception(String.Format("Updating Stage1_CU_Security - Failed\n\nHowever, the code loading process was successful."));
                                        }
                                    }
                                }
                                logger.Debug("Updating Stage1_CU_Security - Successful");
                            }
                        }
                        // Loading to Side - B
                        ///////////////////////////////////

                    }
                    else if (IsResetChecked == true)
                    {
                        CAN2_Commands.JTags.Set_For_Reset(sec);

                        COFF_Descriptor key = new COFF_Descriptor(PT_Generations.Gen2, PT_Component.CU, PT_Models.NotDefined, PT_Code_Type.Loader);
                        if (CAN2_Commands.Loaded_COFF_Files.ContainsKey(key.Description) == false)
                        {
                            ///////////////////////////////////
                            // Load COFF file
                            //SqlBooleanList criteria = new SqlBooleanList();
                            //criteria.Add(new FieldData("Generation", PT_Generations.Gen2.ToString()));
                            //criteria.Add(new FieldData("Component", "CU"));
                            //criteria.Add(new FieldData("Type", PT_Code_Type.Loader.ToString()));
                            SqlBooleanCriteria criteria = SqlBooleanCriteria.Create_Criteria(key);
                            List<SART_COFF_Files> coffList = SART_COFF_Web_Service_Client_REST.Select_SART_COFF_Files_Criteria(InfrastructureModule.Token, criteria);
                            if ((coffList == null) || (coffList.Count == 0))
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to retrieve COFF file");
                                return;
                            }
                            CAN2_Commands.Loaded_COFF_Files[key.Description] = coffList[coffList.Count - 1].Data;
                            if (COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key) == false)
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                                return;
                            }
                            // Load COFF file
                            ///////////////////////////////////
                        }
                        else if (COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key) == false)
                        {
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                            return;
                        }
                        logger.Debug("Loading COFF Image: {0}, Size: {1}", key, CAN2_Commands.Loaded_COFF_Files[key.Description].Length);


                        ///////////////////////////////////
                        // Reset to Side - A
                        if (Load_A == true)
                        {
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Resetting Code for CU-A");
                            Boolean status = Reset_CU_Code(CAN_CU_Sides.A, CAN2_Commands.JTags.CUA);
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Resetting Code for CU-A - {0}", status ? "Passed" : "Failed"));
                            aggregator.GetEvent<SART_CU_CodeReset_A_Event>().Publish(status);
                        }
                        // Reset to Side - A
                        ///////////////////////////////////


                        ///////////////////////////////////
                        // Reset to Side - B
                        if (Load_B == true)
                        {
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Resetting Code for CU-B");
                            Boolean status = Reset_CU_Code(CAN_CU_Sides.B, CAN2_Commands.JTags.CUB);
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Resetting Code for CU-B - {0}", status ? "Passed" : "Failed"));
                            aggregator.GetEvent<SART_CU_CodeReset_B_Event>().Publish(status);
                        }
                        // Reset to Side - B
                        ///////////////////////////////////
                    }
                }

                if (Load_BSA == true)
                {
                    if (Load_CU == true)
                    {
                        Thread.Sleep(CAN2_Commands.Delay_Full_Stop);
                    }

                    if (IsLoadChecked == true)
                    {
                        CAN2_Commands.JTags.Set_For_Load(sec);


                        COFF_Descriptor key = new COFF_Descriptor(PT_Generations.Gen2, PT_Component.CU, modelType, PT_Code_Type.Application);
                        if (CAN2_Commands.Loaded_COFF_Files.ContainsKey(key.Description) == false)
                        {
                            ///////////////////////////////////
                            // Load CU COFF file
                            logger.Debug("Loading CU COFF Image");
                            SqlBooleanCriteria criteria = SqlBooleanCriteria.Create_Criteria(key);
                            //SqlBooleanList criteria = new SqlBooleanList();
                            //criteria.Add(new FieldData("Generation", PT_Generations.Gen2.ToString()));
                            //criteria.Add(new FieldData("Model", modelType.ToString(), SegwayFieldTypes.StringInsensitive));
                            //criteria.Add(new FieldData("Component", "CU"));
                            //criteria.Add(new FieldData("Type", PT_Code_Type.Application.ToString()));
                            List<SART_COFF_Files> coffList = SART_COFF_Web_Service_Client_REST.Select_SART_COFF_Files_Criteria(InfrastructureModule.Token, criteria);
                            if ((coffList == null) || (coffList.Count == 0))
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to retrieve COFF file");
                                return;
                            }
                            CAN2_Commands.CU_COFF = coffList[coffList.Count - 1];
                            CAN2_Commands.Loaded_COFF_Files[key.Description] = CAN2_Commands.CU_COFF.Data;
                            // Load CU COFF file
                            ///////////////////////////////////
                        }


                        key = new COFF_Descriptor(PT_Generations.Gen2, PT_Component.BSA, modelType, PT_Code_Type.Application);
                        if (CAN2_Commands.Loaded_COFF_Files.ContainsKey(key.Description) == false)
                        {
                            ///////////////////////////////////
                            // Load BSA COFF file
                            logger.Debug("Loading BSA COFF Image");
                            SqlBooleanCriteria criteria = SqlBooleanCriteria.Create_Criteria(key);

                            //SqlBooleanList criteria = new SqlBooleanList();
                            //criteria.Add(new FieldData("Generation", PT_Generations.Gen2.ToString()));
                            //criteria.Add(new FieldData("Model", modelType.ToString(), SegwayFieldTypes.StringInsensitive));
                            //criteria.Add(new FieldData("Component", "BSA"));
                            //criteria.Add(new FieldData("Type", PT_Code_Type.Application.ToString()));
                            List<SART_COFF_Files> coffList = SART_COFF_Web_Service_Client_REST.Select_SART_COFF_Files_Criteria(InfrastructureModule.Token, criteria);
                            if ((coffList == null) || (coffList.Count == 0))
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to retrieve COFF file");
                                return;
                            }
                            CAN2_Commands.BSA_COFF = coffList[coffList.Count - 1];
                            CAN2_Commands.Loaded_COFF_Files[key.Description] = CAN2_Commands.BSA_COFF.Data;
                            if (COFF_File.Load(CAN2_Commands.BSA_COFF.Data, key) == false)
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                                return;
                            }

                            //if (COFF_File.Load(CAN2_Commands.BSA_COFF.Data) == false)
                            //{
                            //    aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                            //    return;
                            //}
                            // Load BSA COFF file
                            ///////////////////////////////////
                        }
                        else if (COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key) == false)
                        {
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                            return;
                        }
                        logger.Debug("Loading COFF Image: {0}, Size: {1}", key, CAN2_Commands.Loaded_COFF_Files[key.Description].Length);


                        CAN2_Commands.bsaManufacturingData = null;
                        Boolean status = true;
                        SART_Event_Object obj = null;
                        if (Load_A == true)
                        {
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Loading Code for BSA-A");
                            if (PreBSALoad(CAN_CU_Sides.A, out obj))
                            {
                                CAN2_Commands.bsaManufacturingData = null;

                                status = Load_BSA_Code(CAN_CU_Sides.A, CAN2_Commands.JTags.BSA, CAN2_Commands.JTags.User_Key_Code, obj);
                                aggregator.GetEvent<SART_BSA_CodeLoad_A_Event>().Publish(status);
                                PostBSALoad(CAN_CU_Sides.A, obj, status);
                            }
                            else
                            {
                                status = false;
                            }
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Loading Code for BSA-A - {0}", status ? "Passed" : "Failed"));
                            if (status == false)
                            {
                                aggregator.GetEvent<SART_BSA_CodeLoad_Event>().Publish(status);
                                return;
                            }
                        }

                        if (Load_B == true)
                        {
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Loading Code for BSA-B");
                            obj = null;
                            if (PreBSALoad(CAN_CU_Sides.B, out obj))
                            {
                                CAN2_Commands.bsaManufacturingData = null;

                                status = Load_BSA_Code(CAN_CU_Sides.B, CAN2_Commands.JTags.BSA, CAN2_Commands.JTags.User_Key_Code, obj);
                                aggregator.GetEvent<SART_BSA_CodeLoad_B_Event>().Publish(status);
                                PostBSALoad(CAN_CU_Sides.B, obj, status);
                            }
                            else
                            {
                                status = false;
                            }
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Loading Code for BSA-B - {0}", status ? "Passed" : "Failed"));
                            aggregator.GetEvent<SART_BSA_CodeLoad_Event>().Publish(status);
                            if (status == false)
                            {
                                aggregator.GetEvent<SART_BSA_CodeLoad_Event>().Publish(status);
                                return;
                            }
                        }


                        for (int x = 0; x < 2; x++)
                        {
                            ////////////////////////////////////////////////////////////////////////////
                            // Extract BSA Code Information
                            var eventObj = Create_BSA_Extract_Object();
                            if (eventObj == null) return;

                            key = new COFF_Descriptor(PT_Generations.Gen2, PT_Component.CU, modelType, PT_Code_Type.Application);
                            if (COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key) == false)
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load CU COFF file");
                                return;
                            }
                            logger.Debug("Loaded COFF Image: {0}, Size: {1}", key, CAN2_Commands.Loaded_COFF_Files[key.Description].Length);

                            Thread.Sleep(CAN2_Commands.Delay_Full_Stop);

                            status = BSA_Extract.Code_Information(eventObj);
                            aggregator.GetEvent<SART_BSA_CodeLoad_Event>().Publish(status);
                            // Extract BSA Code Information
                            ////////////////////////////////////////////////////////////////////////////

                            if (status == true) break;
                        }


                        if ((status == true) && (CAN2_Commands.bsaManufacturingData != null) && (String.IsNullOrEmpty(CAN2_Commands.bsaManufacturingData.Serial_Number) == false))
                        {
                            ////////////////////////////////////////////////////////////////////////////
                            // Updating Stage1_BSA_Security
                            logger.Debug("Updating Stage1_BSA_Security");
                            var jtagList = Manufacturing_S1BSASec_Web_Service_Client_REST.Select_Stage1_BSA_Security_BSA_SERIAL_NUMBER_All(InfrastructureModule.Token, CAN2_Commands.bsaManufacturingData.Serial_Number);
                            Boolean found = false;
                            if (jtagList != null)
                            {
                                if (jtagList.Count > 0)
                                {
                                    if (jtagList[jtagList.Count - 1].BSA_JTag_Lock == CAN2_Commands.JTags.BSA.JTag_Lock)
                                    {
                                        found = true;
                                    }
                                }
                            }
                            if (found == false)
                            {
                                Stage1_BSA_Security bsec = new Stage1_BSA_Security();
                                bsec.Service_Key_Code = "320FD25CA958F948";
                                bsec.User_Key_Code = CAN2_Commands.JTags.User_Key_Code;
                                bsec.BSA_JTag_Lock = CAN2_Commands.JTags.BSA.JTag_Lock;
                                bsec.BSA_Serial_Number = CAN2_Commands.bsaManufacturingData.Serial_Number;
                                if (Manufacturing_S1BSASec_Web_Service_Client_REST.Insert_Stage1_BSA_Security_Key(InfrastructureModule.Token, bsec) == null)
                                {
                                    throw new Exception(String.Format("Updating Stage1_BSA_Security - Failed\n\nHowever, the code loading process was successful."));
                                }
                            }
                            logger.Debug("Updating Stage1_BSA_Security - Successful");

                            // Updating Stage1_BSA_Security
                            ////////////////////////////////////////////////////////////////////////////
                        }
                    }
                    else if (IsResetChecked == true)
                    {
                        CAN2_Commands.JTags.Set_For_Reset(sec);
                        Boolean status = true;


                        COFF_Descriptor key = new COFF_Descriptor(PT_Generations.Gen2, PT_Component.BSA, PT_Models.NotDefined, PT_Code_Type.Boot_Loader, "2012-05-23");
                        if (CAN2_Commands.Loaded_COFF_Files.ContainsKey(key.Description) == false)
                        {
                            ////////////////////////////////////////////////////////////////////////////
                            // Load COFF file
                            //SqlBooleanList criteria = new SqlBooleanList();
                            //criteria.Add(new FieldData("Generation", PT_Generations.Gen2.ToString()));
                            //criteria.Add(new FieldData("Component", "BSA"));
                            //criteria.Add(new FieldData("Type", PT_Code_Type.Boot_Loader.ToString()));
                            //criteria.Add(new FieldData("Build_Date", "2012-05-23"));
                            SqlBooleanCriteria criteria = SqlBooleanCriteria.Create_Criteria(key);
                            List<SART_COFF_Files> coffList = SART_COFF_Web_Service_Client_REST.Select_SART_COFF_Files_Criteria(InfrastructureModule.Token, criteria);
                            if ((coffList == null) || (coffList.Count == 0))
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to retrieve BSA Loader file");
                                return;
                            }
                            CAN2_Commands.Loaded_COFF_Files[key.Description] = coffList[coffList.Count - 1].Data;
                            if (COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key) == false)
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load BSA Loader file");
                                return;
                            }
                            // Load COFF file
                            ////////////////////////////////////////////////////////////////////////////
                        }
                        else if (COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key) == false)
                        {
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                            return;
                        }
                        logger.Debug("Loading COFF Image: {0}, Size: {1}", key, CAN2_Commands.Loaded_COFF_Files[key.Description].Length);
                        CAN2_Commands.COFFfiles[PT_Code_Type.Boot_Loader] = CAN2_Commands.Loaded_COFF_Files[key.Description];




                        key = new COFF_Descriptor(PT_Generations.Gen2, PT_Component.BSA, PT_Models.NotDefined, PT_Code_Type.Loader, "2012-05-23");
                        if (CAN2_Commands.Loaded_COFF_Files.ContainsKey(key.Description) == false)
                        {
                            ////////////////////////////////////////////////////////////////////////////
                            // Load COFF file
                            //SqlBooleanList criteria = new SqlBooleanList();
                            //criteria.Add(new FieldData("Generation", PT_Generations.Gen2.ToString()));
                            //criteria.Add(new FieldData("Component", "BSA"));
                            //criteria.Add(new FieldData("Type", PT_Code_Type.Loader.ToString()));
                            //criteria.Add(new FieldData("Build_Date", "2012-05-23"));
                            SqlBooleanCriteria criteria = SqlBooleanCriteria.Create_Criteria(key);

                            List<SART_COFF_Files> coffList = SART_COFF_Web_Service_Client_REST.Select_SART_COFF_Files_Criteria(InfrastructureModule.Token, criteria);
                            if ((coffList == null) || (coffList.Count == 0))
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to retrieve BSA Loader file");
                                return;
                            }
                            CAN2_Commands.Loaded_COFF_Files[key.Description] = coffList[coffList.Count - 1].Data;
                            if (COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key) == false)
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load BSA Loader file");
                                return;
                            }
                            // Load COFF file
                            ////////////////////////////////////////////////////////////////////////////
                        }
                        else if (COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key) == false)
                        {
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                            return;
                        }
                        logger.Debug("Loading COFF Image: {0}, Size: {1}", key, CAN2_Commands.Loaded_COFF_Files[key.Description].Length);
                        CAN2_Commands.COFFfiles[PT_Code_Type.Loader] = CAN2_Commands.Loaded_COFF_Files[key.Description];


                        // Reset to Side - A
                        if (Load_A == true)
                        {
                            ///////////////////////////////////
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Resetting Code for BSA-A");
                            status = Reset_BSA_Code(CAN_CU_Sides.A, CAN2_Commands.JTags);
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Resetting Code for BSA-A - {0}", status ? "Passed" : "Failed"));
                            aggregator.GetEvent<SART_BSA_CodeReset_A_Event>().Publish(status);
                            if (status == false) return;
                        }
                        // Reset to Side - A
                        ///////////////////////////////////


                        ///////////////////////////////////
                        // Reset to Side - B
                        if (Load_B == true && status == true)
                        {

                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Resetting Code for BSA-B");
                            status = Reset_BSA_Code(CAN_CU_Sides.B, CAN2_Commands.JTags);
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Resetting Code for BSA-B - {0}", status ? "Passed" : "Failed"));
                            aggregator.GetEvent<SART_BSA_CodeReset_B_Event>().Publish(status);
                        }
                        // Reset to Side - B
                        ///////////////////////////////////

                    }
                }
                PopupMessage = IsResetChecked ? "Reset Code has been completed" : "Code Load has been completed";
                PopupOpen = true;
                PopupColor = Brushes.LightGreen;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                aggregator.GetEvent<Shell_Close_Event>().Publish(null);
                aggregator.GetEvent<ToolBar_Selection_Event>().Publish(LoginModule.ToolBar_Name);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                CodeLoad_PopupColor = Brushes.Pink;
                CodeLoad_PopupMessage = msg;
                CodeLoad_PopupOpen = true;
            }
            finally
            {
                CAN2_Commands.COFFfiles.Clear();
                CAN2_Commands.JTags = null;

                aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                IsButtonEnabled = true;
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private PT_Models Get_Model_Type(String Partnumber)
        {
            PT_Models ptmod = PT_Models.NotDefined;
            try
            {
                ptmod = (PT_Models)Enum.Parse(typeof(PT_Models), Partnumber, true);
                return ptmod;
            }
            catch { return ptmod; }
        }


        //private void JTag_Timer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    try
        //    {
        //        logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

        //        if (LoginContext.User_Level < UserLevels.Expert)
        //        {
        //            logger.Debug("Not an Expert user");
        //            SqlBooleanCriteria criteria = new SqlBooleanCriteria();
        //            DateTime dt = DateTime.Now;
        //            criteria.Add(new FieldData("User_Name", LoginContext.UserName, SegwayFieldTypes.StringInsensitive));
        //            criteria.Add(new FieldData("Work_Order_Number", InfrastructureModule.Current_Work_Order.Work_Order_ID));
        //            criteria.Add(new FieldData("Date_Time_Start", dt, SegwayFieldTypes.DateTime, FieldCompareOperator.LessThanOrEqual));
        //            criteria.Add(new FieldData("Date_Time_End", dt, SegwayFieldTypes.DateTime, FieldCompareOperator.GreaterThanOrEqual));

        //            JTag_Visibilities = SART_JTAGvis_Web_Service_Client_REST.Select_SART_JTag_Visibility_Criteria(Token, criteria);
        //            if ((JTag_Visibilities != null) && (JTag_Visibilities.Count > 0))
        //            {
        //                JTag_Visibility = Visibility.Visible;
        //                return;
        //            }
        //        }
        //        JTag_Visibility = Visibility.Collapsed;
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


        private void CommandOpenPopup_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = Brushes.Pink;
                PopupMessage = msg;
                PopupOpen = true;
            }
            finally
            {
                aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
