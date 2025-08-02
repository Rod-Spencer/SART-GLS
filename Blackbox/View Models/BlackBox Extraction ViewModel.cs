using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Login.Objects;
using Segway.Manufacturing.Objects;
using Segway.Modules.Controls.JTags;
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
using Segway.Service.Modules.AddWindow;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Segway.Service.SART
{
    /// <summary>Public Class - BlackBox_Extraction_ViewModel</summary>
    public class BlackBox_Extraction_ViewModel : ViewModelBase, BlackBox_Extraction_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;

        //private readonly String Name = "BlackBox_Extraction_ViewModel";

        /// <summary>Contructor</summary>
        /// <param name="view">BlackBox_Extraction_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public BlackBox_Extraction_ViewModel(BlackBox_Extraction_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<WorkOrder_Opened_Event>().Subscribe(WorkOrder_Opened_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<BlackBox_Open_Extract_Panel_Event>().Subscribe(BlackBox_Open_Extract_Panel_Handler, true);
            eventAggregator.GetEvent<SART_EventLog_Add_Event>().Subscribe(SART_EventLog_Add_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_EventLog_Update_Event>().Subscribe(SART_EventLog_Update_Handler, ThreadOption.UIThread, true);

            #endregion

            #region Command Delegates

            StartBBExtractCommand = new DelegateCommand(CommandStartBBExtract, CanCommandStartBBExtract);
            CancelCommand = new DelegateCommand(CommandCancel, CanCommandCancel);
            OpenPopupCommand = new DelegateCommand(CommandOpenPopup, CanCommandOpenPopup);

            #endregion
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties


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


        private int EventID { get; set; }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

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

        #region Header Controls

        #region Work_Order_Num

        private String _Work_Order_Num;

        /// <summary>Property Work_Order_Num of type String</summary>
        public String Work_Order_Num
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Work_Order_ID;
            }
            set
            {
                _Work_Order_Num = value;
                OnPropertyChanged("Work_Order_Num");
            }
        }

        #endregion

        #region PTSerial

        private String _PTSerial;

        /// <summary>Property PTSerial of type String</summary>
        public String PTSerial
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.PT_Serial;
            }
            set
            {
                _PTSerial = value;
                OnPropertyChanged("PTSerial");
            }
        }

        #endregion

        #region WorkOrderColor

        /// <summary>Property WorkOrderColor of type Brush</summary>
        public Brush WorkOrderColor
        {
            get
            {
                if ((InfrastructureModule.Current_Work_Order == null) || (InfrastructureModule.Current_Work_Order.Priority != "Incident"))
                {
                    return new BrushConverter().ConvertFromString("#FF67A1DC") as Brush;
                }
                else
                {
                    return Brushes.Red;
                }
            }
            set
            {
                OnPropertyChanged("WorkOrderColor");
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
                    _Header_Image = Image_Helper.ImageFromEmbedded(".Images.BBExtract.png");
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

        #region Extraction_Note

        private String _Extraction_Note;

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

        #region ClearButtonVisibility

        private Visibility _ClearButtonVisibility = Visibility.Visible;

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

        #region Expert_Visibility

        /// <summary>Property Expert_Visibility of type Visibility</summary>
        public Visibility Expert_Visibility
        {
            get
            {
                if (LoginContext == null) return Visibility.Collapsed;
                if (LoginContext.User_Level >= UserLevels.Expert) return Visibility.Visible;
                return Visibility.Collapsed;
            }
            set
            {
                OnPropertyChanged("Expert_Visibility");
            }
        }

        #endregion


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
                return CAN2_Commands.JTags;
            }
            set
            {
                CAN2_Commands.JTags = value;
                OnPropertyChanged("JTagsData");
            }
        }

        #endregion

        #region ProgressLog

        private ObservableCollection<SART_Event_Log_Entry> _ProgressLog;

        /// <summary>Property ProgressLog of type List&lt;SART.Objects.SART_Event_Log_Entry</summary>
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
            }
        }

        #endregion

        #region IsClearLog

        private Boolean _IsClearLog;

        /// <summary>Property IsClearLog of type Boolean</summary>
        public Boolean IsClearLog
        {
            get { return _IsClearLog; }
            set
            {
                _IsClearLog = value;
                OnPropertyChanged("IsClearLog");
            }
        }

        #endregion

        #region Is_BSAA 

        private Boolean _Is_BSAA = true;

        /// <summary>Property Is_BSAA of type Boolean</summary>
        public Boolean Is_BSAA
        {
            get { return _Is_BSAA; }
            set
            {
                _Is_BSAA = value;
                OnPropertyChanged("Is_BSAA");
            }
        }

        #endregion

        #region Is_BSAB

        private Boolean _Is_BSAB = true;

        /// <summary>Property Is_BSAB of type Boolean</summary>
        public Boolean Is_BSAB
        {
            get { return _Is_BSAB; }
            set
            {
                _Is_BSAB = value;
                OnPropertyChanged("Is_BSAB");
            }
        }

        #endregion

        #region Clear_Log_Visibility

        /// <summary>Property Clear_Log_Visibility of type Visibility</summary>
        public Visibility Clear_Log_Visibility
        {
            get
            {
                if (LoginContext == null) return Visibility.Collapsed;
                if (LoginContext.User_Level >= UserLevels.Expert) return Visibility.Visible;
                return Visibility.Collapsed;
            }
            set { OnPropertyChanged("Clear_Log_Visibility"); }
        }

        #endregion


        #region BBExtract Popup Controls

        #region BBExtract_PopupMessage

        private String _BBExtract_PopupMessage;

        /// <summary>ViewModel Property: BBExtract_PopupMessage of type: String</summary>
        public String BBExtract_PopupMessage
        {
            get { return _BBExtract_PopupMessage; }
            set
            {
                _BBExtract_PopupMessage = value;
                OnPropertyChanged("BBExtract_PopupMessage");
            }
        }

        #endregion

        #region BBExtract PopupOpen

        private Boolean _BBExtract_PopupOpen;

        /// <summary>ViewModel Property: BBExtract_PopupOpen of type: Boolean</summary>
        public Boolean BBExtract_PopupOpen
        {
            get { return _BBExtract_PopupOpen; }
            set
            {
                _BBExtract_PopupOpen = value;
                OnPropertyChanged("BBExtract_PopupOpen");
            }
        }

        #endregion

        #region BBExtract PopupColor

        private Brush _BBExtract_PopupColor;

        /// <summary>ViewModel Property: BBExtract_PopupColor of type: Brush</summary>
        public Brush BBExtract_PopupColor
        {
            get { return _BBExtract_PopupColor; }
            set
            {
                _BBExtract_PopupColor = value;
                OnPropertyChanged("BBExtract_PopupColor");
            }
        }

        #endregion

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region StartBBExtractCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: StartBBExtractCommand</summary>
        public DelegateCommand StartBBExtractCommand { get; set; }
        private Boolean CanCommandStartBBExtract() { return true; }
        private void CommandStartBBExtract()
        {
            if (InfrastructureModule.Current_Work_Order == null)
            {
                Message_Window.Error("Black Box data can only be extracted when within a Work Order", height: Window_Sizes.Small).ShowDialog();
                return;
            }


            Thread back = new Thread(new ThreadStart(CommandStartBBExtract_Back));
            back.IsBackground = true;
            back.Start();

            ExtractVisibility = Visibility.Collapsed;
            CancelVisibility = Visibility.Visible;
        }


        /////////////////////////////////////////////
        #endregion


        #region CancelCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: CancelCommand</summary>
        public DelegateCommand CancelCommand { get; set; }
        private Boolean CanCommandCancel() { return true; }
        private void CommandCancel()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                CancelVisibility = Visibility.Collapsed;
                ExtractVisibility = Visibility.Visible;
                CAN2_Commands.Continue_Processing = false;
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
            _LoginContext = null;
            _Token = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Opened_Handler  -- Event: WorkOrder_Opened_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Opened_Handler(Boolean opened)
        {
            OnPropertyChanged("WorkOrderColor");
            OnPropertyChanged("Work_Order_Num");
            OnPropertyChanged("PTSerial");
            OnPropertyChanged("Expert_Visibility");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BlackBox_Open_Extract_Panel_Handler  -- Event: BlackBox_Open_Extract_Panel_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BlackBox_Open_Extract_Panel_Handler(IViewModel vm)
        {
            if (vm == this) OnNavigatedTo(null);
            else OnNavigatedFrom(null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_EventLog_Add_Handler  -- Event: SART_EventLog_Add_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_EventLog_Add_Handler(SART_Event_Log_Entry log)
        {
            if (log.Object_ID == EventID)
            {
                ProgressLog.Add(log);
                SelectedLogEntry = log;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_EventLog_Update_Handler  -- Event: SART_EventLog_Update_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_EventLog_Update_Handler(SART_Event_Log_Entry log)
        {
            if (log.Object_ID == EventID)
            {
                ProgressLog.Remove(log);
                ProgressLog.Add(log);
                SelectedLogEntry = log;
            }
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
        public bool IsNavigationTarget(NavigationContext navigationContext) { return true; }

        /// <summary>Public Method - OnNavigatedFrom</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ((JTags_ViewModel)((BlackBox_Extraction_Control)View).jtagCtrl.ViewModel).GetJTagCommand.RaiseCanExecuteChanged();
            OnPropertyChanged("Clear_Log_Visibility");
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private void CommandStartBBExtract_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                CAN2_Commands.Continue_Processing = true;
                //SART_Common.Initialize_Timeouts_and_Delays();

                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    if (((BlackBox_Extraction_Control)View).jtagCtrl == null) ((BlackBox_Extraction_Control)View).jtagCtrl = new JTags_Control();
                    if (((BlackBox_Extraction_Control)View).jtagCtrl.dp_JTags == null) ((BlackBox_Extraction_Control)View).jtagCtrl.dp_JTags = CAN2_Commands.JTags;
                    //CAN2_Commands._JTags = CAN2_Commands.JTags;
                    //((BlackBox_Extraction_Control)View).jtagCtrl.Update_JTags(ref CAN2_Commands._JTags);
                    ProgressLog = null;
                    SelectedLogEntry = null;
                });

                Security sec = SART_Common.Get_Security_Data(InfrastructureModule.Current_Work_Order.PT_Serial);
                CAN2_Commands.JTags.Set_For_Load(sec);

                logger.Debug("Creating instance of SART_Event_Object");
                SART_Event_Object obj = new SART_Event_Object(InfrastructureModule.Current_Work_Order.Work_Order_ID,
                                                              Event_Types.BSA_BlackBox_Extract,
                                                              Event_Statuses.In_Progress,
                                                              LoginContext.UserName);

                logger.Debug("Inserting instance of SART_Event_Object to DB");
                obj = SART_EVOBJ_Web_Service_Client_REST.Insert_SART_Event_Object_Key(InfrastructureModule.Token, obj);
                if (obj == null)
                {
                    String msg = "Unable to insert Event Object";
                    logger.Error(msg);
                    InfrastructureModule.Aggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
                    throw new Exception(msg);
                }
                logger.Debug("SART Event Ojbect ID: {0}", obj.ID);

                InfrastructureModule.Container.RegisterInstance<int>("ObjectID", obj.ID, new ContainerControlledLifetimeManager());
                EventID = obj.ID;


                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Extracting BSA Black Box for Side-A");
                Event_Statuses status = Event_Statuses.Passed;


                try
                {
                    if (Is_BSAA == true)
                    {
                        logger.Info("Extracting BSA-A Black Box Log");
                        Guid? BB_A_ID = Black_Box_Extract_Services.Extract(CAN_CU_Sides.A, CAN2_Commands.JTags.BSA, obj);
                        if ((BB_A_ID.HasValue == false) || (BB_A_ID.Value == Guid.Empty))
                        {
                            logger.Warn(Black_Box_Services.Error_Message);
                            PopupColor = Brushes.LightGoldenrodYellow;
                            PopupMessage = Black_Box_Services.Error_Message;
                            PopupOpen = true;
                            status = Event_Statuses.Failed;
                        }
                    }

                    if (Is_BSAB == true)
                    {
                        logger.Info("Extracting BSA-B Black Box Log");
                        eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Extracting BSA Black Box for Side-B");
                        Guid? BB_B_ID = Black_Box_Extract_Services.Extract(CAN_CU_Sides.B, CAN2_Commands.JTags.BSA, obj);
                        if ((BB_B_ID.HasValue == false) || (BB_B_ID.Value == Guid.Empty))
                        {
                            logger.Warn(Black_Box_Services.Error_Message);
                            PopupColor = Brushes.LightGoldenrodYellow;
                            PopupMessage = Black_Box_Services.Error_Message;
                            PopupOpen = true;
                            status = Event_Statuses.Failed;
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
                    obj.Timestamp_End = DateTime.Now;
                    obj.EventStatus = Event_Statuses.Finished;
                    SART_EVOBJ_Web_Service_Client_REST.Update_SART_Event_Object_Key(InfrastructureModule.Token, obj);

                    var sts = SART_Common.Create_Event(WorkOrder_Events.BSA_BlackBox_Extract, status, obj.ID);
                    if (sts == true)
                    {
                        InfrastructureModule.Aggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Publish(true);
                    }
                    eventAggregator.GetEvent<BlackBox_Refresh_Event>().Publish(true);
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
                CancelVisibility = Visibility.Collapsed;
                ExtractVisibility = Visibility.Visible;
                CAN2_Commands.Continue_Processing = false;

                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private void CommandOpenPopup_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                //throw new NotImplementedException();
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
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
