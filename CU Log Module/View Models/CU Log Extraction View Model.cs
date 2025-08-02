using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Login.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder;
using Segway.SART.CULog.Objects;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.CAN;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Objects;
using Segway.Service.Tools.CAN2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Segway.Modules.CU_Log_Module
{
    public class CU_Log_Extraction_View_Model : ViewModelBase, CU_Log_Extraction_View_Model_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator aggregator;

        public CU_Log_Extraction_View_Model(CU_Log_Extraction_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.aggregator = eventAggregator;


            #region Event Subscriptions

            aggregator.GetEvent<SART_EventLog_Add_Event>().Subscribe(Add_Event, ThreadOption.UIThread, true);
            aggregator.GetEvent<SART_EventLog_Update_Event>().Subscribe(Update_Event, ThreadOption.UIThread, true);
            aggregator.GetEvent<Application_Logout_Event>().Subscribe(Clear_Event, ThreadOption.UIThread, true);
            aggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, ThreadOption.UIThread, true);
            aggregator.GetEvent<CU_Log_EventID_Event>().Subscribe(Receive_Event_ID, true);
            aggregator.GetEvent<OpenExtractPanelEvent>().Subscribe(InitalizeExtractPanel_Event, true);
            aggregator.GetEvent<WorkOrder_Opened_Event>().Subscribe(WorkOrder_Opened_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Setups

            ClearLogCommand = new DelegateCommand(CommandClearLog, CanCommandClearLog);
            ExtractCommand = new DelegateCommand(CommandExtract, CanCommandExtract);
            CancelCommand = new DelegateCommand(CommandCancel, CanCommandCancel);
            YesCommand = new DelegateCommand(CommandYes, CanCommandYes);
            NoCommand = new DelegateCommand(CommandNo, CanCommandNo);
            ExtractLogNoteCommand = new DelegateCommand(CommandExtractLogNote, CanCommandExtractLogNote);
            CancelNoteCommand = new DelegateCommand(CommandCancelNote, CanCommandCancelNote);

            OpenPopupCommand = new DelegateCommand(CommandOpenPopup, CanCommandOpenPopup);

            #endregion

            Is_CUA = Is_CUB = true;
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        #region LoginContext

        private Login_Context _LoginContext = null;

        /// <summary>Property LoginContext of type Login_Context</summary>
        public Login_Context LoginContext
        {
            get
            {
                if (container.IsRegistered<AuthenticationToken_Interface>(AuthenticationToken.ApplicationGlobalInstanceName) == true)
                {
                    AuthenticationToken at = container.Resolve<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName);
                    _LoginContext = at.LoginContext;
                }
                return _LoginContext;
            }
        }

        #endregion

        private int EventID { get; set; }


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        #region ProgressLog

        private ObservableCollection<SART_Event_Log_Entry> _ProgressLog;

        /// <summary>Property ProgressLog of type List<SART.Objects.SART_Event_Log_Entry</summary>
        public ObservableCollection<SART_Event_Log_Entry> ProgressLog
        {
            get
            {
                if (_ProgressLog == null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        _ProgressLog = new ObservableCollection<SART_Event_Log_Entry>();
                    });
                }
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

        #region Is_CUA

        private Boolean _Is_CUA = true;

        /// <summary>Property Is_CUA of type Boolean</summary>
        public Boolean Is_CUA
        {
            get { return _Is_CUA; }
            set
            {
                _Is_CUA = value;
                OnPropertyChanged("Is_CUA");
                ExtractCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Is_CUB

        private Boolean _Is_CUB = true;

        /// <summary>Property Is_CUB of type Boolean</summary>
        public Boolean Is_CUB
        {
            get { return _Is_CUB; }
            set
            {
                _Is_CUB = value;
                OnPropertyChanged("Is_CUB");
                ExtractCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Is_Wired

        private Boolean _Is_Wired = true;

        /// <summary>Property Is_Wired of type Boolean</summary>
        public Boolean Is_Wired
        {
            get { return _Is_Wired; }
            set
            {
                _Is_Wired = value;
                OnPropertyChanged("Is_Wired");
            }
        }

        #endregion

        #region Is_Wireless

        private Boolean _Is_Wireless = false;

        /// <summary>Property Is_Wireless of type Boolean</summary>
        public Boolean Is_Wireless
        {
            get { return _Is_Wireless; }
            set
            {
                _Is_Wireless = value;
                OnPropertyChanged("Is_Wireless");
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
            }
        }

        #endregion

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

        #region Header_Image

        private BitmapImage _Header_Image;

        /// <summary>Property Header_Image of type BitmapImage</summary>
        public BitmapImage Header_Image
        {
            get
            {
                if (_Header_Image == null)
                {
                    _Header_Image = Image_Helper.ImageFromEmbedded(".Images.new.png");
                }
                return _Header_Image;
            }
            set
            {
                OnPropertyChanged("Header_Image");
            }
        }

        #endregion

        #region IsFormatLog

        private Boolean _IsFormatLog = false;

        /// <summary>Property IsFormatLog of type Boolean</summary>
        public Boolean IsFormatLog
        {
            get
            {
                if ((InfrastructureModule.Current_Work_Order != null) && (InfrastructureModule.Current_Work_Order.Priority == "Incident"))
                {
                    _IsFormatLog = false;
                }

                return _IsFormatLog;
            }
            set
            {
                _IsFormatLog = value;
                OnPropertyChanged("IsFormatLog");
                if (_IsFormatLog)
                {
                    _IsClearLog = _IsExtractLog = false;
                    OnPropertyChanged("IsExtractLog");
                    OnPropertyChanged("IsClearLog");
                }
            }
        }

        #endregion

        #region IsClearLog

        private Boolean? _IsClearLog = null;

        /// <summary>Property IsClearLog of type Boolean</summary>
        public Boolean IsClearLog
        {
            get
            {
                if ((InfrastructureModule.Current_Work_Order != null) && (InfrastructureModule.Current_Work_Order.Priority == "Incident"))
                {
                    _IsClearLog = false;
                }
                else if (_IsClearLog.HasValue == false)
                {
                    if ((User_Info == null) || (User_Info.User_Level == Service.Objects.UserLevels.Basic)) _IsClearLog = false;
                    else _IsClearLog = true;
                }
                return _IsClearLog.Value;
            }
            set
            {
                _IsClearLog = value;
                OnPropertyChanged("IsClearLog");
                if (_IsClearLog.Value == true)
                {
                    _IsFormatLog = false;
                    OnPropertyChanged("IsFormatLog");
                }
            }
        }

        #endregion

        #region IsExtractLog

        private Boolean _IsExtractLog = true;

        /// <summary>Property IsExtractLog of type Boolean</summary>
        public Boolean IsExtractLog
        {
            get { return _IsExtractLog; }
            set
            {
                _IsExtractLog = value;
                OnPropertyChanged("IsExtractLog");
                OnPropertyChanged("ShowRawData_Visibility");
                if (_IsExtractLog)
                {
                    _IsFormatLog = false;
                    OnPropertyChanged("IsFormatLog");
                }
            }
        }

        #endregion

        #region Clear_Log_Visibility

        /// <summary>Property Clear_Log_Visibility of type Visibility</summary>
        public Visibility Clear_Log_Visibility
        {
            get
            {
                if (User_Info == null) return Visibility.Collapsed;
                else if (User_Info.User_Level >= Service.Objects.UserLevels.Intermediate)
                {
                    if ((InfrastructureModule.Current_Work_Order == null) || (InfrastructureModule.Current_Work_Order.Priority == "Incident"))
                    {
                        return Visibility.Collapsed;
                    }
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            set { OnPropertyChanged("Clear_Log_Visibility"); }
        }

        #endregion

        #region Format_Log_Visibility

        /// <summary>Property Format_Log_Visibility of type Visibility</summary>
        public Visibility Format_Log_Visibility
        {
            get
            {
                if (User_Info == null) return Visibility.Collapsed;
                if (User_Info.User_Level >= UserLevels.Expert)
                {
                    if ((InfrastructureModule.Current_Work_Order == null) || (InfrastructureModule.Current_Work_Order.Priority == "Incident"))
                    {
                        return Visibility.Collapsed;
                    }
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            set { OnPropertyChanged("Format_Log_Visibility"); }
        }

        #endregion

        #region AutoLoad_Visibility

        /// <summary>Property AutoLoad_Visibility of type Visibility</summary>
        public Visibility AutoLoad_Visibility
        {
            get
            {
                return Visibility.Collapsed;
                // Don't need this functionality for now, 12/27/2013
                //if (User_Info.User_Level == Service.Objects.UserLevels.Expert)
                //{
                //if ((InfrastructureModule.Current_Work_Order != null) && (InfrastructureModule.Current_Work_Order.Priority == "Incident"))
                //{
                //    return Visibility.Collapsed;
                //}

                //    return Visibility.Visible;
                //}
                //else
                //{
                //    return Visibility.Collapsed;
                //}
            }
            set
            {
                OnPropertyChanged("AutoLoad_Visibility");
            }
        }

        #endregion

        #region IsAutoLoad

        private Boolean _IsAutoLoad = true;

        /// <summary>Property IsAutoLoad of type Boolean</summary>
        public Boolean IsAutoLoad
        {
            get
            {
                if ((InfrastructureModule.Current_Work_Order != null) && (InfrastructureModule.Current_Work_Order.Priority == "Incident"))
                {
                    _IsAutoLoad = false;
                }
                return _IsAutoLoad;
            }
            set
            {
                _IsAutoLoad = value;
                OnPropertyChanged("IsAutoLoad");
            }
        }

        #endregion

        #region BorderColumns

        /// <summary>Property BorderColumns of type Double</summary>
        public Double BorderColumns
        {
            get
            {
                if ((User_Info != null) && (User_Info.User_Level >= UserLevels.Expert))
                {
                    return 3.0;
                }
                else
                {
                    return 2.0;
                }
            }
            set
            {
                OnPropertyChanged("BorderColumns");
            }
        }

        #endregion

        #region ClearButtonVisibility

        private Visibility _ClearButtonVisibility = Visibility.Collapsed;

        /// <summary>Property ClearButtonVisibility of type Visibility</summary>
        public Visibility ClearButtonVisibility
        {
            get { return _ClearButtonVisibility; }
            set
            {
                _ClearButtonVisibility = value;
                OnPropertyChanged("ClearButtonVisibility");
            }
        }

        #endregion

        #region ExtractVisibility

        private Visibility _ExtractVisibility = Visibility.Visible;

        /// <summary>Property ExtractVisibility of type Visibility</summary>
        public Visibility ExtractVisibility
        {
            get { return _ExtractVisibility; }
            set
            {
                _ExtractVisibility = value;
                OnPropertyChanged("ExtractVisibility");
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

        //CUSerialNumberValidationErrorMessage
        #region NoteVisibility

        private Visibility _NoteVisibility;

        /// <summary>Property NoteVisibility of type Visibility</summary>
        public Visibility NoteVisibility
        {
            get
            {
                return _NoteVisibility;
            }
            set
            {
                _NoteVisibility = value;
                OnPropertyChanged("NoteVisibility");
            }
        }

        #endregion

        #region Extraction_Note

        private String _Extraction_Note = "Be certain battery power is sufficient before pulling logs.";

        /// <summary>Property Extraction_Note of type String</summary>
        public String Extraction_Note
        {
            get { return _Extraction_Note; }
            set
            {
                _Extraction_Note = value;
                OnPropertyChanged("Extraction_Note");
            }
        }

        #endregion

        #region Extract_Note_Popup_Open

        private Boolean _Extract_Note_Popup_Open;

        /// <summary>Property Extract_Note_Popup_Open of type Boolean</summary>
        public Boolean Extract_Note_Popup_Open
        {
            get { return _Extract_Note_Popup_Open; }
            set
            {
                _Extract_Note_Popup_Open = value;
                OnPropertyChanged("Extract_Note_Popup_Open");
            }
        }

        #endregion

        #region CUSerialNumberValidationErrorPopupOpen

        private Boolean _CUSerialNumberValidationErrorPopupOpen = false;

        /// <summary>Property CUSerialNumberValidationErrorPopupOpen of type Boolean</summary>
        public Boolean CUSerialNumberValidationErrorPopupOpen
        {
            get { return _CUSerialNumberValidationErrorPopupOpen; }
            set
            {
                _CUSerialNumberValidationErrorPopupOpen = value;
                OnPropertyChanged("CUSerialNumberValidationErrorPopupOpen");
            }
        }

        #endregion

        #region CUSerialNumberValidationErrorMessage

        private String _CUSerialNumberValidationErrorMessage;

        /// <summary>Property CUSerialNumberValidationErrorMessage of type Boolean</summary>
        public String CUSerialNumberValidationErrorMessage
        {
            get { return _CUSerialNumberValidationErrorMessage; }
            set
            {
                _CUSerialNumberValidationErrorMessage = value;
                OnPropertyChanged("CUSerialNumberValidationErrorMessage");
            }
        }

        #endregion

        #region ContinueOnError

        private Boolean? _ContinueOnError = null;

        /// <summary>Property CULogValidationErrorPopupOpen of type Boolean</summary>
        public Boolean? ContinueOnError
        {
            get { return _ContinueOnError; }
            set
            {
                _ContinueOnError = value;
            }
        }

        #endregion


        #region CULogExtract Popup Controls

        #region CULogExtract_PopupMessage

        private String _CULogExtract_PopupMessage;

        /// <summary>ViewModel Property: CULogExtract_PopupMessage of type: String</summary>
        public String CULogExtract_PopupMessage
        {
            get { return _CULogExtract_PopupMessage; }
            set
            {
                _CULogExtract_PopupMessage = value;
                OnPropertyChanged("CULogExtract_PopupMessage");
            }
        }

        #endregion

        #region CULogExtract PopupOpen

        private Boolean _CULogExtract_PopupOpen;

        /// <summary>ViewModel Property: CULogExtract_PopupOpen of type: Boolean</summary>
        public Boolean CULogExtract_PopupOpen
        {
            get { return _CULogExtract_PopupOpen; }
            set
            {
                _CULogExtract_PopupOpen = value;
                OnPropertyChanged("CULogExtract_PopupOpen");
            }
        }

        #endregion

        #region CULogExtract PopupColor

        private Brush _CULogExtract_PopupColor;

        /// <summary>ViewModel Property: CULogExtract_PopupColor of type: Brush</summary>
        public Brush CULogExtract_PopupColor
        {
            get { return _CULogExtract_PopupColor; }
            set
            {
                _CULogExtract_PopupColor = value;
                OnPropertyChanged("CULogExtract_PopupColor");
            }
        }

        #endregion

        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region INavigationAware Implementation

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //aggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            aggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Event Logs", "CU_Log_Extraction"));
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ContinueOnError = null;
            aggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            aggregator.GetEvent<ToolBar_Selection_Event>().Publish("Event Logs");
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region LoadCodeNoticeCommand

        /// <summary>Delegate Command: ExtractLogNoteCommand</summary>
        public DelegateCommand ExtractLogNoteCommand { get; set; }

        private Boolean CanCommandExtractLogNote()
        {
            return (Is_CUA || Is_CUB) && (IsExtractLog || IsFormatLog || IsClearLog);
        }

        private void CommandExtractLogNote()
        {
            // form button bound to this command
            if ((User_Info != null) && (User_Info.User_Level >= UserLevels.Expert))
            {
                CommandExtract();
            }
            else
            {
                // need to launch popup for log extraction
                Extract_Note_Popup_Open = true;
            }
        }

        #endregion

        #region CancelNoteCommand

        /// <summary>Delegate Command: CancelNoteCommand</summary>
        public DelegateCommand CancelNoteCommand { get; set; }

        private Boolean CanCommandCancelNote() { return true; }
        private void CommandCancelNote()
        {
            // close the popup and do nothing
            Extract_Note_Popup_Open = false;
        }

        #endregion

        #region ExtractCommand

        /// <summary>Delegate Command: ExtractCommand</summary>
        public DelegateCommand ExtractCommand { get; set; }

        private Boolean CanCommandExtract()
        {
            return (Is_CUA || Is_CUB) && (IsExtractLog || IsFormatLog || IsClearLog);
        }

        private void CommandExtract()
        {
            ProgressLog.Clear();

            Thread back = new Thread(new ThreadStart(CommandExtract_Back));
            back.IsBackground = true;
            back.Start();
        }

        #endregion

        #region ClearLogCommand

        /// <summary>Delegate Command: ClearLogCommand</summary>
        public DelegateCommand ClearLogCommand { get; set; }

        private Boolean CanCommandClearLog() { return true; }
        private void CommandClearLog() { }

        #endregion


        #region CancelCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: CancelCommand</summary>
        public DelegateCommand CancelCommand { get; set; }
        private Boolean CanCommandCancel() { return true; }
        private void CommandCancel()
        {
            CAN2_Commands.Continue_Processing = false;
            CancelVisibility = Visibility.Collapsed;
            //Application_Helper.DoEvents();
        }

        /////////////////////////////////////////////
        #endregion

        #region YesCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: YesCommand</summary>
        public DelegateCommand YesCommand { get; set; }
        private Boolean CanCommandYes() { return true; }
        private void CommandYes()
        {
            ContinueOnError = true;
            CUSerialNumberValidationErrorPopupOpen = false;
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
            CUSerialNumberValidationErrorPopupOpen = false;
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
                CULogExtract_PopupColor = Brushes.Pink;
                CULogExtract_PopupMessage = msg;
                CULogExtract_PopupOpen = true;
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
        #region SART_WorkOrder_Close_Handler  -- Event: SART_WorkOrder_Close_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Close_Handler(Boolean closed)
        {
            if (closed == true) Reset();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Cancel_Handler  -- Event: SART_WorkOrder_Cancel_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Cancel_Handler(Boolean cancelled)
        {
            if (cancelled == true) Reset();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Opened_Handler  -- Event: WorkOrder_Opened_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Opened_Handler(Boolean opened)
        {
            _IsClearLog = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        private void Add_Event(SART_Event_Log_Entry log)
        {
            if (log.Object_ID == EventID)
            {
                ProgressLog.Add(log);
                SelectedLogEntry = log;
            }
        }


        private void Update_Event(SART_Event_Log_Entry log)
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
            Application.Current.Dispatcher.Invoke((Action)delegate ()
            {
                ProgressLog.Clear();
            });
        }

        private void Receive_Event_ID(int id)
        {
            EventID = id;
        }

        private void InitalizeExtractPanel_Event(Boolean IsExtractPanelOpen)
        {
            if (IsExtractPanelOpen)
            {
                if ((User_Info != null) && (User_Info.User_Level >= UserLevels.Expert))
                {
                    // just display the legal text notice on the bottom of page
                    NoteVisibility = Visibility.Visible;
                }
                else
                {
                    // will need to launch popup for code load
                    NoteVisibility = Visibility.Collapsed;
                }
            }
        }


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        public void OnSelected()
        {
            OnPropertyChanged("Work_Order_Num");
            OnPropertyChanged("PTSerial");
            OnPropertyChanged("WorkOrderColor");
            OnPropertyChanged("Format_Log_Visibility");
            OnPropertyChanged("AutoLoad_Visibility");
            OnPropertyChanged("IsAutoLoad");
            OnPropertyChanged("BorderColumns");
            OnPropertyChanged("Clear_Log_Visibility");
            OnPropertyChanged("Format_Log_Visibility");
            OnPropertyChanged("ClearButtonVisibility");
            OnPropertyChanged("IsExtractLog");
            OnPropertyChanged("IsClearLog");
            OnPropertyChanged("IsFormatLog");
            OnPropertyChanged("Header_Image");
            OnPropertyChanged("ExtractionInProgress");
            OnPropertyChanged("Is_Wireless");
            OnPropertyChanged("Is_Wired");
            OnPropertyChanged("Is_CUA");
            OnPropertyChanged("Is_CUB");
            OnPropertyChanged("SelectedLogEntry");
            OnPropertyChanged("ProgressLog");
            OnPropertyChanged("ShowRawData_Visibility");
        }

        private void Reset()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                ProgressLog = null;
                SelectedLogEntry = null;
                Is_CUA = true;
                Is_CUB = true;
                Is_Wired = true;
                Is_Wireless = false;
                User_Info = null;
                ExtractionInProgress = false;
                OnPropertyChanged("Work_Order_Num");
                OnPropertyChanged("PTSerial");
                OnPropertyChanged("WorkOrderColor");

                IsFormatLog = false;
                IsClearLog = false;
                IsExtractLog = true;

                OnPropertyChanged("Clear_Log_Visibility");
                OnPropertyChanged("Format_Log_Visibility");
                OnPropertyChanged("AutoLoad_Visibility");

                IsAutoLoad = true;
                OnPropertyChanged("BorderColumns");

                ClearButtonVisibility = Visibility.Collapsed;
                ExtractVisibility = Visibility.Visible;
                CancelVisibility = Visibility.Collapsed;
                NoteVisibility = Visibility.Collapsed;
                Extraction_Note = "Be certain battery power is sufficient before pulling logs.";
                Extract_Note_Popup_Open = false;
                CUSerialNumberValidationErrorPopupOpen = false;
                CUSerialNumberValidationErrorMessage = null;
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


        private void CommandExtract_Back()
        {
            try
            {
                // close the popup if it is open, or for experts hide the notice
                Extract_Note_Popup_Open = false;
                NoteVisibility = Visibility.Collapsed;
                aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                ExtractionInProgress = true;
                CAN2_Commands.Continue_Processing = true;
                //SART_Common.Initialize_Timeouts_and_Delays();

                CancelVisibility = Visibility.Visible;
                ExtractVisibility = Visibility.Collapsed;
                NoteVisibility = Visibility.Collapsed;


                Boolean ExtractA = false;
                Boolean ExtractB = false;

                if (IsExtractLog == true)
                {
                    if (Is_CUA)
                    {
                        aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Extracting log for CU-A");
                        this.ContinueOnError = null;
                        SART_CU_Logs log = Log_Extract.Extract_CU_Log(this, CAN_CU_Sides.A);
                        if (log != null)
                        {
                            ExtractA = true;
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Successfully extracted log for CU-A ({0})", log.CU_Serial));
                            if ((User_Info != null) && (User_Info.User_Level >= UserLevels.Expert))
                            {
                                Common.Display_CU_Log(log, G2_Report_Types.Advanced, true, false);
                            }
                            else
                            {
                                Common.Display_CU_Log(log, G2_Report_Types.Basic, true, false);
                            }
                        }
                    }

                    if (CAN2_Commands.Continue_Processing == false) return;


                    if (Is_CUB)
                    {
                        aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Extracting log for CU-B");
                        this.ContinueOnError = null;
                        SART_CU_Logs log = Log_Extract.Extract_CU_Log(this, CAN_CU_Sides.B);
                        if (log != null)
                        {
                            ExtractB = true;
                            aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Successfully extracted log for CU-B ({0})", log.CU_Serial));
                            if ((User_Info != null) && (User_Info.User_Level >= UserLevels.Expert))
                            {
                                Common.Display_CU_Log(log, G2_Report_Types.Advanced, true, false);
                            }
                            else
                            {
                                Common.Display_CU_Log(log, G2_Report_Types.Basic, true, false);
                            }
                        }
                    }
                }

                if (IsClearLog == true)
                {
                    if ((User_Info != null) && (User_Info.User_Level >= UserLevels.Intermediate))
                    {
                        if (Is_CUA)
                        {
                            if ((IsExtractLog == false) || ((IsExtractLog == true) && (ExtractA == true)))
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Clearing log for CU-A");
                                String cuSerial = null;
                                if (Log_Extract.Clear_CU_Log(this, out cuSerial, CAN_CU_Sides.A) == true)
                                {
                                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Successfully cleared log for CU-A ({0})", cuSerial));
                                }
                                else
                                {
                                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Clearing log for CU-A ({0}) Failed!", cuSerial));
                                }
                            }
                        }

                        if (CAN2_Commands.Continue_Processing == false) return;

                        if (Is_CUB)
                        {
                            if ((IsExtractLog == false) || ((IsExtractLog == true) && (ExtractB == true)))
                            {
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Clearing log for CU-B");
                                String cuSerial = null;
                                if (Log_Extract.Clear_CU_Log(this, out cuSerial, CAN_CU_Sides.B) == true)
                                {
                                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Successfully cleared log for CU-B ({0})", cuSerial));
                                }
                                else
                                {
                                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Clearing log for CU-B ({0}) Failed!", cuSerial));
                                }
                            }
                        }
                    }
                }

                if (IsFormatLog == true)
                {
                    if ((User_Info != null) && (User_Info.User_Level >= UserLevels.Expert))
                    {
                        if (Is_CUA)
                        {
                            if ((IsExtractLog == false) || ((IsExtractLog == true) && (ExtractA == true)))
                            {
                                String cuSerial = null;
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Formatting log for CU-A");
                                if (Log_Extract.Format_CU_Log(User_Info, out cuSerial, CAN_CU_Sides.A) == true)
                                {
                                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Successfully formatted log for CU-A ({0})", cuSerial));
                                }
                                else
                                {
                                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Formatting log for CU-A ({0}) Failed!", cuSerial));
                                }
                            }
                        }

                        if (CAN2_Commands.Continue_Processing == false) return;

                        if (Is_CUB)
                        {
                            if ((IsExtractLog == false) || ((IsExtractLog == true) && (ExtractB == true)))
                            {
                                String cuSerial = null;
                                aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Formatting log for CU-B");
                                if (Log_Extract.Format_CU_Log(User_Info, out cuSerial, CAN_CU_Sides.B) == true)
                                {
                                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Successfully formatted log for CU-B ({0})", cuSerial));
                                }
                                else
                                {
                                    aggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Formatting log for CU-B ({0}) Failed!", cuSerial));
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                aggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                CancelVisibility = Visibility.Collapsed;
                ExtractVisibility = Visibility.Visible;
            }
        }


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
