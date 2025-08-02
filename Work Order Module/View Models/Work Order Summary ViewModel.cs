using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Segway.Login.Objects;
using Segway.Modules.Controls.JTags;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.Progression_Log_Viewer;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder.Services;
using Segway.SART.Objects;
using Segway.Service.AppSettings.Helper;
using Segway.Service.Authentication.Client.REST;
using Segway.Service.Authentication.Objects;
using Segway.Service.CAN;
using Segway.Service.Common;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Modules.AddWindow;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using Segway.Syteline.Client.REST;
using Segway.Syteline.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Segway.Modules.WorkOrder
{
    public class Work_Order_Summary_ViewModel : ViewModelBase, Work_Order_Summary_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        public Work_Order_Summary_ViewModel(Work_Order_Summary_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;

            logger.Debug("Entered - Work_Order_Summary_ViewModel Constructor");


            #region Event Subscriptions

            //eventAggregator.GetEvent<Application_Logout_Event>().Subscribe(Application_Logout_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<Application_Logout_Event>().Subscribe(Application_Logout_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<Shell_Close_Event>().Subscribe(Close_SART, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_BSATest_Done_Event>().Subscribe(Received_BSATest_Done_Notice, true);
            eventAggregator.GetEvent<SART_BSA_CodeLoad_Event>().Subscribe(Received_BSA_CodeLoad_Notice, true);
            eventAggregator.GetEvent<SART_CU_CodeLoad_A_Event>().Subscribe(Received_CU_CodeLoad_A_Notice, true);
            eventAggregator.GetEvent<SART_CU_CodeLoad_B_Event>().Subscribe(Received_CU_CodeLoad_B_Notice, true);
            eventAggregator.GetEvent<SART_LEDTest_Done_Event>().Subscribe(Received_LEDTest_Done_Notice, true);
            eventAggregator.GetEvent<SART_MotorTest_Done_Event>().Subscribe(Received_MotorTest_Done_Notice, true);
            eventAggregator.GetEvent<SART_RiderDetectTest_Done_Event>().Subscribe(Received_RiderDetectTest_Done_Notice, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(WorkOrder_Cancel_Handler, true);
            eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, true);
            eventAggregator.GetEvent<WorkOrder_AuditUpdate_Request_Event>().Subscribe(Update_Audit, true);
            eventAggregator.GetEvent<WorkOrder_Configuration_Event>().Subscribe(Add_PT_Config, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<WorkOrder_Configuration_Final_Event>().Subscribe(UpdateConfigFinal, true);
            eventAggregator.GetEvent<WorkOrder_Configuration_Refresh_Event>().Subscribe(Config_Refresh_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<WorkOrder_Configuration_Start_Event>().Subscribe(UpdateConfigStart, true);
            eventAggregator.GetEvent<WorkOrder_Opened_Event>().Subscribe(Load_Work_Order, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<WorkOrder_Read_CU_Log_Event>().Subscribe(Read_CU_Log, true);
            eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Subscribe(WorkOrder_RideTest_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<WorkOrder_Status_Change_Event>().Subscribe(Status_Change_Handler, true);
            eventAggregator.GetEvent<JTag_Request_Serial_Event>().Subscribe(JTag_Request_Serial_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<Navigate_To_Login_Event>().Subscribe(NavigateTo_Login_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates

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
            IKErr_CopyCommand = new DelegateCommand(CommandIKErr_Copy, CanCommandIKErr_Copy);

            #endregion

            //Show_Warning_Message = true;
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


        #region Current_Work_Order

        /// <summary>Property Current_Work_Order of type SART_Work_Order</summary>
        public SART_Work_Order Current_Work_Order
        {
            get { return InfrastructureModule.Current_Work_Order; }
            set { InfrastructureModule.Current_Work_Order = value; }
        }

        #endregion

        #region Status_Codes

        private Dictionary<String, String> _Status_Codes;

        /// <summary>Property Status_Codes of type Dictionary<String, String></summary>
        public Dictionary<String, String> Status_Codes
        {
            get
            {
                if (_Status_Codes == null)
                {
                    try
                    {
                        if (InfrastructureModule.Token != null)
                        {
                            _Status_Codes = Common.Get_Statuses(InfrastructureModule.Token);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(Exception_Helper.FormatExceptionString(e));
                    }
                }
                return _Status_Codes;
            }
            set
            {
                _Status_Codes = value;
                OnPropertyChanged("Status_Codes");
            }
        }

        #endregion

        //public Boolean Show_Warning_Message { get; set; }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties


        ////////////////////////////////////////////////
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
        ////////////////////////////////////////////////


        #region Component_Data

        private List<Component_Info> _Component_Data;

        /// <summary>Property Component_Data of type List<Component_Info></summary>
        public List<Component_Info> Component_Data
        {
            get { return _Component_Data; }
            set
            {
                _Component_Data = value;
                OnPropertyChanged("Component_Data");
            }
        }

        #endregion


        #region ConfigChoiceCheckboxVisibility

        private Visibility _ConfigChoiceCheckboxVisibility;

        /// <summary>Property ConfigChoiceCheckboxVisibility of type Visibility</summary>
        public Visibility ConfigChoiceCheckboxVisibility
        {
            get { return _ConfigChoiceCheckboxVisibility; }
            set
            {
                _ConfigChoiceCheckboxVisibility = value;
                OnPropertyChanged("ConfigChoiceCheckboxVisibility");
            }
        }

        #endregion

        #region ConfigSummaryVisibility

        /// <summary>Property ConfigSummaryVisibility of type Visibility</summary>
        public Visibility ConfigSummaryVisibility
        {
            get
            {
                if (LoginContext == null) return Visibility.Collapsed;
                if (LoginContext.User_Level == UserLevels.Basic) return Visibility.Visible;
                else return _IsDetailedView ? Visibility.Collapsed : Visibility.Visible;
            }
            set
            {
                OnPropertyChanged("ConfigSummaryVisibility");
            }
        }

        #endregion

        #region Current_Error_Code_Observations

        private String _Current_Error_Code_Observations = "";

        /// <summary>Property Current_Error_Code_Observations of type String</summary>
        public String Current_Error_Code_Observations
        {
            get { return _Current_Error_Code_Observations; }
            set
            {
                _Current_Error_Code_Observations = value;
                OnPropertyChanged("Current_Error_Code_Observations");
                ObservationSaveCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Current_Segway_Observations

        private String _Current_Segway_Observations = "";

        /// <summary>Property Current_Segway_Observations of type String</summary>
        public String Current_Segway_Observations
        {
            get { return _Current_Segway_Observations; }
            set
            {
                _Current_Segway_Observations = value;
                OnPropertyChanged("Current_Segway_Observations");
                ObservationSaveCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Current_Technician_Observations

        private String _Current_Technician_Observations = "";

        /// <summary>Property Current_Technician_Observations of type String</summary>
        public String Current_Technician_Observations
        {
            get { return _Current_Technician_Observations; }
            set
            {
                _Current_Technician_Observations = value;
                OnPropertyChanged("Current_Technician_Observations");
                ObservationSaveCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Current_Unit_Condition

        private String _Current_Unit_Condition = "";

        /// <summary>Property Current_Unit_Condition of type String</summary>
        public String Current_Unit_Condition
        {
            get { return _Current_Unit_Condition; }
            set
            {
                _Current_Unit_Condition = value;
                OnPropertyChanged("Current_Unit_Condition");
                ObservationSaveCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Error_Code_Observations

        /// <summary>Property Error_Code_Observations of type String</summary>
        public String Error_Code_Observations
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return "";
                return InfrastructureModule.Current_Work_Order.Error_Code_Notes;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;
                InfrastructureModule.Current_Work_Order.Error_Code_Notes = value;
                OnPropertyChanged("Error_Code_Observations");
            }
        }

        #endregion

        #region IsConfigStart

        /// <summary>Property IsConfigStart of type Boolean?</summary>
        public Boolean? IsConfigStart
        {
            get
            {
                if (Current_Work_Order == null) return false;
                return Current_Work_Order.Is_Start_Config;
            }
            set
            {
                Current_Work_Order.Is_Start_Config = value;
                OnPropertyChanged("IsConfigStart");
                OnPropertyChanged("Summary_Start_Config_Visibility");
                OnPropertyChanged("IsConfigStartOverride");
                OnPropertyChanged("Summary_Start_Config_Override_Visibility");
                ConfigStartCommand.RaiseCanExecuteChanged();
                ConfigFinalCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region IsConfigStartOverride

        /// <summary>ViewModel property: IsConfigStartOverride of type: Boolean points to Current_Work_Order.Is_Start_Config_Override</summary>
        public Boolean IsConfigStartOverride
        {
            get
            {
                if (Current_Work_Order == null) return false;
                if (Current_Work_Order.Config_Start_Override.HasValue == false) return false;
                return Current_Work_Order.Config_Start_Override.Value;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Config_Start_Override = value;
                OnPropertyChanged("IsConfigStart");
                OnPropertyChanged("Summary_Start_Config_Visibility");
                OnPropertyChanged("IsConfigStartOverride");
                OnPropertyChanged("Summary_Start_Config_Override_Visibility");
                ConfigStartCommand.RaiseCanExecuteChanged();
                ConfigFinalCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion


        ////////////////////////////////////////////////
        #region Picture Controls

        #region IsPicturesExpanded

        private Boolean _IsPicturesExpanded = false;

        /// <summary>Property IsPicturesExpanded of type Boolean</summary>
        public Boolean IsPicturesExpanded
        {
            get { return _IsPicturesExpanded; }
            set
            {
                _IsPicturesExpanded = value;
                OnPropertyChanged("IsPicturesExpanded");
            }
        }

        #endregion

        #region Picture_List

        private ObservableCollection<Seg_SART_Pictures_Nodata> _Picture_List;

        /// <summary>Property Picture_List of type List<SART_Pictures></summary>
        public ObservableCollection<Seg_SART_Pictures_Nodata> Picture_List
        {
            get
            {
                if (_Picture_List == null) _Picture_List = new ObservableCollection<Seg_SART_Pictures_Nodata>();
                return _Picture_List;
            }
            set
            {
                _Picture_List = value;
                OnPropertyChanged("Picture_List");
            }
        }

        #endregion

        #region Selected_Picture

        private Seg_SART_Pictures_Nodata _Selected_Picture;

        /// <summary>Property Selected_Picture of type SART_Pictures</summary>
        public Seg_SART_Pictures_Nodata Selected_Picture
        {
            get { return _Selected_Picture; }
            set
            {
                _Selected_Picture = value;
                OnPropertyChanged("Selected_Picture");
                PictureOpenCommand.RaiseCanExecuteChanged();
                PictureDelCommand.RaiseCanExecuteChanged();
                PictureEditCommand.RaiseCanExecuteChanged();
                PictureAddCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////

        #region SegwayObservationsVisibility

        /// <summary>Property ConfigSummaryVisibility of type Visibility</summary>
        public Visibility SegwayObservationsVisibility
        {
            get
            {
                if (LoginContext == null) return Visibility.Collapsed;
                if (LoginContext.User_Level >= UserLevels.Expert) return Visibility.Visible;
                else return Visibility.Collapsed;
            }
            set
            {
                OnPropertyChanged("SegwayObservationsVisibility");
            }
        }

        #endregion


        #region SelectedTabIndex

        private int _SelectedTabIndex;

        /// <summary>Property SelectedTabIndex of type int</summary>
        public int SelectedTabIndex
        {
            get { return _SelectedTabIndex; }
            set
            {
                _SelectedTabIndex = value;
                OnPropertyChanged("SelectedTabIndex");
            }
        }

        #endregion

        #region Summary_Segway_Observations


        /// <summary>Property Summary_Segway_Observations of type String</summary>
        public String Summary_Segway_Observations
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return "";
                return InfrastructureModule.Current_Work_Order.Segway_Observation;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;
                InfrastructureModule.Current_Work_Order.Segway_Observation = value;
                OnPropertyChanged("Summary_Segway_Observations");
            }
        }

        #endregion

        #region Summary_Technician_Observations

        /// <summary>Property Summary_Technician_Observations of type String</summary>
        public String Summary_Technician_Observations
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return "";
                return InfrastructureModule.Current_Work_Order.Observation_Technician;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;
                InfrastructureModule.Current_Work_Order.Observation_Technician = value;
                OnPropertyChanged("Summary_Technician_Observations");
            }
        }

        #endregion

        #region Summary_UnitCondition

        /// <summary>Property Summary_UnitCondition of type String</summary>
        public String Summary_UnitCondition
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return "";
                return InfrastructureModule.Current_Work_Order.Unit_Condition;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;
                InfrastructureModule.Current_Work_Order.Unit_Condition = value;
                OnPropertyChanged("Summary_UnitCondition");
            }
        }

        #endregion

        #region WorkOrderHistoryVisibility

        private Visibility _WorkOrderHistoryVisibility;

        /// <summary>Property WorkOrderHistoryVisibility of type Visibility</summary>
        public Visibility WorkOrderHistoryVisibility
        {
            get { return _WorkOrderHistoryVisibility; }
            set
            {
                _WorkOrderHistoryVisibility = value;
                OnPropertyChanged("WorkOrderHistoryVisibility");
            }
        }

        #endregion


        ////////////////////////////////////////////////////////////
        // Un-Sorted Controls

        #region Summary_Work_Order_Number

        /// <summary>Property Work_Order_Number of type String</summary>
        public String Summary_Work_Order_Number
        {
            get
            {
                if (Current_Work_Order == null) return null;
                return Current_Work_Order.Work_Order_ID;
            }
            set { OnPropertyChanged("Summary_Work_Order_Number"); }
        }

        #endregion

        #region Summary_PT_Serial_Number

        /// <summary>Property PT_Serial_Number of type String</summary>
        public String Summary_PT_Serial_Number
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.PT_Serial;
            }
            set { OnPropertyChanged("Summary_PT_Serial_Number"); }
        }

        #endregion

        #region Summary_PT_Model

        /// <summary>Property PT_Model of type String</summary>
        public String Summary_PT_Model
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.PT_Model;
            }
            set { OnPropertyChanged("Summary_PT_Model"); }
        }

        #endregion

        #region Summary_User_Name

        /// <summary>Property User_Name of type String</summary>
        public String Summary_User_Name
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Entered_By;
            }
            set
            {
                //if (InfrastructureModule.Current_Work_Order == null) return;
                //InfrastructureModule.Current_Work_Order.Entered_By = value;
                OnPropertyChanged("Summary_User_Name");
            }
        }

        #endregion

        #region Summary_Technician_Name

        /// <summary>Property User_Name of type String</summary>
        public String Summary_Technician_Name
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Technician_Name;
            }
            set
            {
                OnPropertyChanged("Summary_Technician_Name");
            }
        }

        #endregion

        #region Summary_Customer

        /// <summary>ViewModel property: Summary_Customer of type: String points to InfrastructureModule.Current_Work_Order.Customer_Name</summary>
        public String Summary_Customer
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Customer_Name;
            }
            set { OnPropertyChanged("Summary_Customer"); }
        }

        #endregion

        #region Summary_Date_Entered

        /// <summary>Property Date_Entered of type DateTime?</summary>
        public DateTime? Summary_Date_Entered
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Date_Created;
            }
            set
            {
                //if (InfrastructureModule.Current_Work_Order == null) return;
                //InfrastructureModule.Current_Work_Order.Date_Created = value;
                OnPropertyChanged("Summary_Date_Entered");
            }
        }

        #endregion

        #region Summary_Start_Date

        /// <summary>ViewModel property: Summary_Start_Date of type: DateTime? points to InfrastructureModule.Current_Work_Order.Start_Date_1</summary>
        public DateTime? Summary_Start_Date
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Start_Date;
            }
            set
            {
                //InfrastructureModule.Current_Work_Order.Start_Date_1 = value;
                OnPropertyChanged("Summary_Start_Date");
            }
        }

        #endregion

        #region Summary_IsWarranty

        /// <summary>Property IsWarranty of type Boolean</summary>
        public String Summary_IsWarranty
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.IsWarranty ? "YES" : "NO";
            }
            set
            {
                //if (String.IsNullOrEmpty(value)) _IsWarranty = false;
                //else if (value.ToUpper() == "YES") _IsWarranty = true;
                //else _IsWarranty = false;
                OnPropertyChanged("Summary_IsWarranty");
            }
        }

        #region Status

        private String _Status;

        /// <summary>Property Status of type String</summary>
        public String Status
        {
            get { return _Status; }
            set
            {
                _Status = value;
                OnPropertyChanged("Status");
            }
        }

        #endregion

        #endregion

        #region Summary_Status

        private String _Summary_Status;

        /// <summary>Property Summary_Status of type String</summary>
        public String Summary_Status
        {
            get { return _Summary_Status; }
            set
            {
                _Summary_Status = value;
                OnPropertyChanged("Summary_Status");
            }
        }

        #endregion

        #region Summary_Complaint

        private String _Summary_Complaint;

        /// <summary>Property Summary_Complaint of type String</summary>
        public String Summary_Complaint
        {
            get { return _Summary_Complaint; }
            set
            {
                _Summary_Complaint = value;
                OnPropertyChanged("Summary_Complaint");
            }
        }

        #endregion

        #region Summary_Image

        private BitmapImage _Summary_Image;

        /// <summary>Property Summary_Image of type BitmapImage</summary>
        public BitmapImage Summary_Image
        {
            get
            {
                if (_Summary_Image == null) _Summary_Image = Image_Helper.ImageFromEmbedded("Images.summary.png");
                return _Summary_Image;
            }
            set
            {
                _Summary_Image = value;
                OnPropertyChanged("Summary_Image");
            }
        }

        #endregion

        #region TaskSummary_Image

        private BitmapImage _TaskSummary_Image;

        /// <summary>Property TaskSummary_Image of type BitmapImage</summary>
        public BitmapImage TaskSummary_Image
        {
            get
            {
                if (_TaskSummary_Image == null) _TaskSummary_Image = Image_Helper.ImageFromEmbedded("Images.configuration.png");
                return _TaskSummary_Image;
            }
            set
            {
                _TaskSummary_Image = value;
                OnPropertyChanged("TaskSummary_Image");
            }
        }

        #endregion

        #region Summary_Events_List

        private ObservableCollection<SART_Events> _Summary_Events_List;


        /// <summary>Property Summary_Events_List of type List<SART_Events></summary>
        public ObservableCollection<SART_Events> Summary_Events_List
        {
            get { return _Summary_Events_List; }
            set
            {
                _Summary_Events_List = value;
                OnPropertyChanged("Summary_Events_List");
            }
        }

        #endregion

        #region Selected_Summary_Event

        private SART_Events _Selected_Summary_Event;

        /// <summary>Property Selected_Summary_Event of type SART_Events</summary>
        public SART_Events Selected_Summary_Event
        {
            get { return _Selected_Summary_Event; }
            set
            {
                _Selected_Summary_Event = value;
                ((Work_Order_Summary_Control)View).lvAudit.ContextMenu = null;
                if (LoginContext.User_Level >= UserLevels.Expert)
                {
                    if ((_Selected_Summary_Event != null) && (_Selected_Summary_Event.Object_ID > 0))
                    {
                        ContextMenu cm = new ContextMenu();
                        MenuItem mi = new MenuItem();
                        mi.Header = "View Details";
                        mi.Click += new RoutedEventHandler(Display_SART_Event);
                        cm.Items.Add(mi);
                        ((Work_Order_Summary_Control)View).lvAudit.ContextMenu = cm;
                    }
                }
                OnPropertyChanged("Selected_Summary_Event");
            }
        }

        #endregion

        #region IsCUA_Extracted

        //private Boolean _IsCUA_Extracted;

        /// <summary>Property IsCUA_Extracted of type Boolean</summary>
        public Boolean? IsCUA_Extracted
        {
            get
            {
                if (Current_Work_Order == null) return null;
                return Current_Work_Order.Is_CUA_Extracted;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_CUA_Extracted = value;
                OnPropertyChanged("IsCUA_Extracted");
            }
        }

        #endregion

        #region IsCUB_Extracted

        /// <summary>Property IsCUA_Extracted of type Boolean</summary>
        public Boolean? IsCUB_Extracted
        {
            get
            {
                if (Current_Work_Order == null) return null;
                return Current_Work_Order.Is_CUB_Extracted;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_CUB_Extracted = value;
                OnPropertyChanged("IsCUB_Extracted");
            }
        }

        #endregion

        #region IsCUA_Coded

        /// <summary>Property IsCUB_Coded of type Boolean</summary>
        public Boolean? IsCUA_Coded
        {
            get
            {
                if (Current_Work_Order == null) return null;
                return Current_Work_Order.Is_CUA_Loaded;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_CUA_Loaded = value;
                OnPropertyChanged("IsCUA_Coded");
            }
        }

        #endregion

        #region IsCUB_Coded

        /// <summary>Property IsCUB_Coded of type Boolean</summary>
        public Boolean? IsCUB_Coded
        {
            get
            {
                if (Current_Work_Order == null) return null;
                return Current_Work_Order.Is_CUB_Loaded;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_CUB_Loaded = value;
                OnPropertyChanged("IsCUB_Coded");
            }
        }

        #endregion

        #region IsNormaltMotorTest

        /// <summary>Property IsNormalMotorTest of type Boolean</summary>
        public Boolean? IsNormalMotorTest
        {
            get
            {
                if (Current_Work_Order == null) return null;
                return Current_Work_Order.Is_NormalMotor_Test;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_NormalMotor_Test = value;
                OnPropertyChanged("IsNormalMotorTest");
            }
        }

        #endregion

        #region IsBSA_Coded

        /// <summary>Property IsBSA_Coded of type Boolean?</summary>
        public Boolean? IsBSA_Coded
        {
            get
            {
                if (Current_Work_Order == null) return null;
                return Current_Work_Order.Is_BSA_Code_Loaded;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_BSA_Code_Loaded = value;
                OnPropertyChanged("IsBSA_Coded");
            }
        }

        #endregion

        #region IsBSATest

        /// <summary>Property IsBSATest of type Boolean?</summary>
        public Boolean? IsBSATest
        {
            get
            {
                if (Current_Work_Order == null) return null;
                return Current_Work_Order.Is_BSA_Test;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_BSA_Test = value;
                OnPropertyChanged("IsBSATest");
            }
        }

        #endregion

        #region IsRiderDetect

        /// <summary>Property IsRiderDetect of type Boolean</summary>
        public Boolean? IsRiderDetect
        {
            get
            {
                if (Current_Work_Order == null) return null;
                return Current_Work_Order.Is_RiderDetect_Test;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_RiderDetect_Test = value;
                OnPropertyChanged("IsRiderDetect");
            }
        }

        #endregion

        #region IsLEDTest

        /// <summary>Property IsLEDTest of type Boolean?</summary>
        public Boolean? IsLEDTest
        {
            get
            {
                if (Current_Work_Order == null) return null;
                if (Current_Work_Order != null) return Current_Work_Order.Is_LED_Test;
                return false;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_LED_Test = value;
                OnPropertyChanged("IsLEDTest");
            }
        }

        #endregion

        #region IsRideTest

        /// <summary>Property IsRideTest of type Boolean?</summary>
        public Boolean? IsRideTest
        {
            get
            {
                if (Current_Work_Order == null) return null;
                return Current_Work_Order.Is_Ride_Test;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_Ride_Test = value;
                OnPropertyChanged("IsRideTest");
            }
        }

        #endregion

        #region IsConfigFinal

        /// <summary>Property IsConfigEnd of type Boolean</summary>
        public Boolean? IsConfigFinal
        {
            get
            {
                if (Current_Work_Order == null) return false;
                return Current_Work_Order.Is_Final_Config;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Is_Final_Config = value;
                OnPropertyChanged("IsConfigFinal");
                OnPropertyChanged("Summary_Final_Config_Visibility");
                OnPropertyChanged("Summary_Final_Config_Override_Visibility");
                ConfigStartCommand.RaiseCanExecuteChanged();
                ConfigFinalCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region IsConfigFinalOverride

        /// <summary>ViewModel property: IsConfigFinalOverride of type: Boolean points to Current_Work_Order.Is_Final_Config_Override</summary>
        public Boolean IsConfigFinalOverride
        {
            get
            {
                if (Current_Work_Order == null) return false;
                if (Current_Work_Order.Config_Final_Override.HasValue == false) return false;
                return Current_Work_Order.Config_Final_Override.Value;
            }
            set
            {
                if (Current_Work_Order == null) return;
                Current_Work_Order.Config_Final_Override = value;
                OnPropertyChanged("IsConfigFinalOverride");
                OnPropertyChanged("Summary_Start_Config_Visibility");
                ConfigStartCommand.RaiseCanExecuteChanged();
                ConfigFinalCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region InfoKey_Error_Code

        private String _InfoKey_Error_Code;

        /// <summary>Property InfoKey_Error_Code of type String</summary>
        public String InfoKey_Error_Code
        {
            get { return _InfoKey_Error_Code; }
            set
            {
                _InfoKey_Error_Code = value;
                OnPropertyChanged("InfoKey_Error_Code");
            }
        }

        #endregion


        #region PT_Configurations_List

        private ObservableCollection<SART_PT_Configuration> _PT_Configurations_List;

        /// <summary>Property PT_Configurations_List of type ObservableCollection<SART_PT_Configuration></summary>
        public ObservableCollection<SART_PT_Configuration> PT_Configurations_List
        {
            get
            {
                if (_PT_Configurations_List == null) _PT_Configurations_List = new ObservableCollection<SART_PT_Configuration>();
                return _PT_Configurations_List;
            }
            set
            {
                _PT_Configurations_List = value;
                OnPropertyChanged("PT_Configurations_List");
                List<SART_PT_Configuration> ptconfList = new List<SART_PT_Configuration>();
                if (value != null) ptconfList.AddRange(value);

                //var manager = new ContainerControlledLifetimeManager();
                //container.RegisterInstance<List<SART_PT_Configuration>>("Configurations", ptconfList, manager);
                container.RegisterInstance<List<SART_PT_Configuration>>("Configurations", ptconfList);
            }
        }

        #endregion


        #region ConfigDetailVisibility

        /// <summary>Property ConfigDetailVisibility of type Visibility</summary>
        public Visibility ConfigDetailVisibility
        {
            get
            {
                if (LoginContext == null) return Visibility.Collapsed;
                if (LoginContext.User_Level == UserLevels.Basic) return Visibility.Collapsed;
                else return _IsDetailedView ? Visibility.Visible : Visibility.Collapsed;
            }
            set
            {
                OnPropertyChanged("ConfigDetailVisibility");
            }
        }

        #endregion




        #region IsObservationsExpanded

        private Boolean _IsObservationsExpanded = false;

        /// <summary>Property IsObservationsExpanded of type Boolean</summary>
        public Boolean IsObservationsExpanded
        {
            get { return _IsObservationsExpanded; }
            set
            {
                _IsObservationsExpanded = value;
                OnPropertyChanged("IsObservationsExpanded");
            }
        }

        #endregion

        #region IsAuditExpanded

        private Boolean _IsAuditExpanded = false;

        /// <summary>Property IsAuditExpanded of type Boolean</summary>
        public Boolean IsAuditExpanded
        {
            get { return _IsAuditExpanded; }
            set
            {
                _IsAuditExpanded = value;
                OnPropertyChanged("IsAuditExpanded");
            }
        }

        #endregion

        #region IsConfigurationsExpanded

        private Boolean _IsConfigurationsExpanded = true;

        /// <summary>Property IsConfigurationsExpanded of type Boolean</summary>
        public Boolean IsConfigurationsExpanded
        {
            get { return _IsConfigurationsExpanded; }
            set
            {
                _IsConfigurationsExpanded = value;
                OnPropertyChanged("IsConfigurationsExpanded");
            }
        }

        #endregion

        #region IsDetailedView

        private Boolean _IsDetailedView;

        /// <summary>Property IsDetailedView of type Boolean</summary>
        public Boolean IsDetailedView
        {
            get { return _IsDetailedView; }
            set
            {
                _IsDetailedView = value;
                OnPropertyChanged("IsDetailedView");
                OnPropertyChanged("ConfigSummaryVisibility");
                OnPropertyChanged("ConfigDetailVisibility");
            }
        }

        #endregion


        #region Summary_Start_Config_Visibility

        /// <summary>Property Summary_Start_Config_Visibility of type Visibility</summary>
        public Visibility Summary_Start_Config_Visibility
        {
            get
            {
                if (Current_Work_Order == null) return Visibility.Collapsed;
                if (Current_Work_Order.Config_Start_Override == true) return Visibility.Collapsed;
                return Visibility.Visible;
            }
            set { }
        }

        #endregion

        #region Summary_Start_Config_Override_Visibility

        /// <summary>Property Summary_Start_Config_Override_Visibility of type Visibility</summary>
        public Visibility Summary_Start_Config_Override_Visibility
        {
            get
            {
                if (Current_Work_Order == null) return Visibility.Collapsed;
                if (Current_Work_Order.Config_Start_Override == true) return Visibility.Visible;
                return Visibility.Collapsed;
            }
            set { }
        }

        #endregion

        #region Summary_Final_Config_Visibility

        /// <summary>ViewModel property: Summary_Final_Config_Visibility of type: Visibility points to Current_Work_Order.Is_Final_Config_Override</summary>
        public Visibility Summary_Final_Config_Visibility
        {
            get
            {
                //if (Current_Work_Order.Is_Final_Config_Override == true) return Visibility.Collapsed;
                return Visibility.Visible;
            }
            set { }
        }

        #endregion

        #region Summary_Final_Config_Override_Visibility

        /// <summary>ViewModel property: Summary_Final_Config_Override_Visibility of type: Visibility points to Current_Work_Order.ObjProperty</summary>
        public Visibility Summary_Final_Config_Override_Visibility
        {
            get
            {
                //if (Current_Work_Order.Is_Final_Config_Override == true) return Visibility.Visible;
                return Visibility.Collapsed;
            }
            set { }
        }

        #endregion


        #region Work_Order_Number_Color

        /// <summary>Property Work_Order_Number_Color of type System.Windows.Media.Brush</summary>
        public System.Windows.Media.Brush Work_Order_Number_Color
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
            set { OnPropertyChanged("Work_Order_Number_Color"); }
        }

        #endregion
        // Un-Sorted Controls
        ////////////////////////////////////////////////////////////




        #region RideTestVisibility

        /// <summary>Property RideTestVisibility of type Visibility</summary>
        public Visibility RideTestVisibility
        {
            get
            {
                if (Application_Helper.Application_Name().Contains("Pilot")) return Visibility.Visible;
                return Visibility.Collapsed;
            }
            //set
            //{
            //    OnPropertyChanged("RideTestVisibility");
            //}
        }

        #endregion



        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
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
                if (String.IsNullOrEmpty(Summary_UnitCondition) == false) Summary_UnitCondition += "\n\n";
                Summary_UnitCondition += String.Format("{0} - {1}:\n{2}", LoginContext.UserName, DateTime.Now, Current_Unit_Condition);
                Work_Order_Events.Update_Work_Order();
                Current_Unit_Condition = "";
            }
            else if (SelectedTabIndex == 2)
            {
                // Technician Observations
                if (String.IsNullOrEmpty(Summary_Technician_Observations) == false) Summary_Technician_Observations += "\n\n";
                Summary_Technician_Observations += String.Format("{0} - {1}:\n{2}", LoginContext.UserName, DateTime.Now, Current_Technician_Observations);
                String sto = "";
                foreach (Char x in Summary_Technician_Observations)
                {
                    sto += (String)(x > 255 ? '?' : x).ToString();
                }
                Work_Order_Events.Update_Work_Order();
                Current_Technician_Observations = "";
            }
            else if (SelectedTabIndex == 4)
            {
                // Segway Observations
                if (String.IsNullOrEmpty(Summary_Segway_Observations) == false) Summary_Segway_Observations += "\n\n";
                Summary_Segway_Observations += String.Format("{0} - {1}:\n{2}", LoginContext.UserName, DateTime.Now, Current_Segway_Observations);
                Work_Order_Events.Update_Work_Order();
                Current_Segway_Observations = "";
            }
            else if (SelectedTabIndex == 3)
            {
                // Segway Observations
                if (String.IsNullOrEmpty(Error_Code_Observations) == false) Error_Code_Observations += "\n\n";
                Error_Code_Observations += String.Format("{0} - {1}:\n{2}", LoginContext.UserName, DateTime.Now, Current_Error_Code_Observations);
                Work_Order_Events.Update_Work_Order();
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
            //Application_Helper.DoEvents();

            try
            {
                logger.Trace("Entered");

                Work_Order_Events.Remove_and_Clear_AutoSave(eventAggregator);
                InfrastructureModule.WorkOrder_OpenMode = Open_Mode.Close;

                if ((Current_Work_Order != null) && (String.IsNullOrEmpty(Current_Work_Order.Work_Order_ID) == false))
                {
                    Work_Order_Events.Close_Work_Order(eventAggregator);
                    Reset_Expanded_Controls();
                    InfrastructureModule.Current_Work_Order = null;
                    if (eventAggregator != null) eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Publish(true);
                    if (regionManager != null) regionManager.RequestNavigate(RegionNames.MainRegion, Work_Order_Open_Control.Control_Name);
                }
                else
                {
                    logger.Debug("No Work Order selected");
                }

                Revert_User_Level();
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
            }
            finally
            {
                logger.Trace("Leaving");
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                //Application_Helper.DoEvents();
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
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Revert_User_Level();
                Work_Order_Events.Cancel_Current_Work_Order(eventAggregator, regionManager, true);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
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
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(Brushes.Snow);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error: Work Order Not Saved");
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(Brushes.Pink);
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
            if ((IsConfigStart.HasValue == true) && (IsConfigStart.Value == true)) return false;
            return true;
        }

        private void CommandConfigStart()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                logger.Info("Selected Configuration - Start");
                propertyChanges();
                //Work_Order_Configuration_ViewModel vm = (Work_Order_Configuration_ViewModel)container.Resolve<Work_Order_Configuration_ViewModel_Interface>();
                //if ((vm.PT_Config.Work_Order != Current_Work_Order.Work_Order_ID) || (vm.PT_Config.Serial_Number != Current_Work_Order.PT_Serial) || (vm.PT_Config.ConfigType != ConfigurationTypes.Service_Start))
                //{
                //    if ((vm.StartConfig = Find_Configuration(Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Start)) == null)
                //    {
                //        vm.StartConfig = SART_2012_Web_Service_Client.Select_SART_PT_Configuration_WORK_ORDER(InfrastructureModule.Token, Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Start);
                //        if (vm.StartConfig == null)
                //        {
                //            vm.StartConfig = new SART_PT_Configuration();
                //            vm.StartConfig.Work_Order = Current_Work_Order.Work_Order_ID;
                //            vm.StartConfig.ConfigType = ConfigurationTypes.Service_Start;
                //            vm.StartConfig.Serial_Number = Current_Work_Order.PT_Serial;

                //            vm.StartConfig = SART_2012_Web_Service_Client.Insert_SART_PT_Configuration_Key(InfrastructureModule.Token, vm.StartConfig);
                //        }
                //    }
                //    vm.PT_Config.Copy(vm.StartConfig, true);
                //}
                eventAggregator.GetEvent<WorkOrder_ConfigurationType_Event>().Publish(ConfigurationTypes.Service_Start);
                eventAggregator.GetEvent<WorkOrder_Configuration_ClearLog_Event>().Publish(true);
                regionManager.RequestNavigate(RegionNames.MainRegion, Work_Order_Configuration_Control.Control_Name);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        #endregion

        #region ConfigFinalCommand

        /// <summary>Delegate Command: ConfigFinalCommand</summary>
        public DelegateCommand ConfigFinalCommand { get; set; }

        private Boolean CanCommandConfigFinal()
        {
            if (Current_Work_Order == null) return false;
            if (InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write) return false;
            if (IsConfigStart == true) return true;
            return IsConfigStartOverride;
        }

        private void CommandConfigFinal()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                logger.Info("Selected Configuration - Final");
                propertyChanges();
                //Work_Order_Configuration_ViewModel vm = (Work_Order_Configuration_ViewModel)container.Resolve<Work_Order_Configuration_ViewModel_Interface>();
                //if ((vm.PT_Config.Work_Order != Current_Work_Order.Work_Order_ID) || (vm.PT_Config.Serial_Number != Current_Work_Order.PT_Serial) || (vm.PT_Config.ConfigType != ConfigurationTypes.Service_Final))
                //{
                //    if ((vm.FinalConfig = Find_Configuration(Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Final)) == null)
                //    {
                //        vm.FinalConfig = SART_2012_Web_Service_Client.Select_SART_PT_Configuration_WORK_ORDER(InfrastructureModule.Token, Current_Work_Order.Work_Order_ID, ConfigurationTypes.Service_Final);
                //        if (vm.FinalConfig == null)
                //        {
                //            vm.FinalConfig = new SART_PT_Configuration();
                //            vm.FinalConfig.Work_Order = Current_Work_Order.Work_Order_ID;
                //            vm.FinalConfig.ConfigType = ConfigurationTypes.Service_Final;
                //            vm.FinalConfig.Serial_Number = Current_Work_Order.PT_Serial;

                //            vm.FinalConfig = SART_2012_Web_Service_Client.Insert_SART_PT_Configuration_Key(InfrastructureModule.Token, vm.FinalConfig);
                //        }
                //    }
                //    vm.PT_Config.Copy(vm.FinalConfig, true);
                //}

                eventAggregator.GetEvent<WorkOrder_ConfigurationType_Event>().Publish(ConfigurationTypes.Service_Final);
                eventAggregator.GetEvent<WorkOrder_Configuration_ClearLog_Event>().Publish(true);
                regionManager.RequestNavigate(RegionNames.MainRegion, Work_Order_Configuration_Control.Control_Name);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
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
            //Application_Helper.DoEvents();

            try
            {
                logger.Trace("Entered");
                List<Seg_SART_Pictures_Nodata> piclist = new List<Seg_SART_Pictures_Nodata>();

                foreach (Seg_SART_Pictures_Nodata pic in ((Work_Order_Summary_Control)View).listPic.SelectedItems) piclist.Add(pic);
                if (piclist.Count > 1) piclist.Sort(new Seg_SART_Pictures_Nodata_CREATE_DATE_Comparer());
                foreach (Seg_SART_Pictures_Nodata pic in piclist)
                {
                    FileInfo fi = SART_Common.Format_Cache_Filename(pic);
                    if (fi.Exists == false)
                    {
                        logger.Debug("Picture: {0} does not exist in cache", fi.FullName);
                        Byte[] picdata = Syteline_Picture_Web_Service_Client_REST.Get_SART_Pictures_Data(InfrastructureModule.Token, pic.RowPointer);
                        if (picdata != null)
                        {
                            logger.Debug("Writing picture to cache");
                            SART_Common.Write_Picture_To_Cache(fi, new Seg_SART_Pictures(pic, picdata));
                            fi.Refresh();
                        }
                    }

                    if (fi.Exists == true) ProcessHelper.Run(fi.FullName, null, 0, true, false, false, false);
                }

                Picture_List = new ObservableCollection<Seg_SART_Pictures_Nodata>(Picture_List);
                foreach (Seg_SART_Pictures_Nodata pic in piclist)
                {
                    if (((Work_Order_Summary_Control)View).listPic.SelectedItems.Contains(pic) == false)
                    {
                        ((Work_Order_Summary_Control)View).listPic.SelectedItems.Add(pic);
                    }
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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
                //Application_Helper.DoEvents();
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
            if (LoginContext.User_Level >= UserLevels.Expert) ofd.Multiselect = true;
            else ofd.Multiselect = false;
            ofd.CheckFileExists = true;
            if (ofd.ShowDialog() == true)
            {
                String picDesc = null;
                Entry_Window ew = null;
                while (String.IsNullOrEmpty(picDesc) == true)
                {
                    ew = new Entry_Window();
                    ew.Width = 600;
                    ew.dp_LabelText = " Edit Description ";

                    Boolean? result = ew.ShowDialog();
                    if (result == true)
                    {
                        if (String.IsNullOrEmpty(ew.dp_TextBoxText) == false) break;
                    }
                    if (result == false)
                    {
                        return;
                    }
                }

                picDesc = ew.dp_TextBoxText;
                if (picDesc.Length > 300) picDesc = picDesc.Substring(0, 300);

                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
                //Application_Helper.DoEvents();

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
                            Seg_SART_Pictures_Nodata pic = new Seg_SART_Pictures_Nodata();
                            pic.Description = picDesc;
                            pic.Name = fi.Name;
                            //pic.Picture_Data = picdata;
                            pic.Unique_Name = GUID_Helper.Encode(Guid.NewGuid()) + Path.GetExtension(fi.Name);
                            pic.User_Name = LoginContext.UserName;
                            pic.Created_By = LoginContext.UserName;
                            pic.Updated_By = LoginContext.UserName;
                            pic.Create_Date = DateTime.Now;
                            pic.Record_Date = DateTime.Now;
                            pic.SRO_Num = Current_Work_Order.Work_Order_ID;
                            //pic.SRO_Line = 1;
                            pic = Syteline_PicND_Web_Service_Client_REST.Insert_Seg_SART_Pictures_Nodata_Key(InfrastructureModule.Token, pic);
                            if (pic != null)
                            {
                                SART_Picture_Data pd = new SART_Picture_Data();
                                pd.RowPointer = pic.RowPointer;
                                pd.PictureData = picdata;
                                Thread back = new Thread(new ParameterizedThreadStart(SART_Common.Upload_Picture_Back));
                                back.IsBackground = true;
                                back.Start(pd);
                                //SART_2012_Web_Service_Client.Select_Seg_SART_Pictures_Nodata_Key(InfrastructureModule.Token, pic.RowPointer)
                                var list = SART_Common.Add_Picture_To_List(new List<Seg_SART_Pictures_Nodata>(Picture_List), pic);
                                Picture_List = new ObservableCollection<Seg_SART_Pictures_Nodata>(list);
                                Selected_Picture = pic;
                                FileInfo cacheFI = SART_Common.Format_Cache_Filename(Selected_Picture);
                                SART_Common.Write_Picture_To_Cache(cacheFI, new Seg_SART_Pictures(pic, picdata));

                                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Uploaded picture: {0} of {1}", ++picCount, ofd.FileNames.Length));
                            }
                        }
                    }
                }
                catch (Authentication_Exception ae)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(ae));
                    eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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
                    //Application_Helper.DoEvents();
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
            if (InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write) return false;
            return Selected_Picture != null;
        }

        private void CommandPictureDel()
        {
            if (Selected_Picture != null)
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
                //Application_Helper.DoEvents();

                try
                {
                    logger.Trace("Entered");
                    List<Seg_SART_Pictures_Nodata> pics = new List<Seg_SART_Pictures_Nodata>();
                    foreach (Seg_SART_Pictures_Nodata pic in ((Work_Order_Summary_Control)View).listPic.SelectedItems)
                    {
                        pics.Add(pic);
                    }
                    int picCount = 0;
                    foreach (Seg_SART_Pictures_Nodata pic in pics)
                    {
                        if (Syteline_Picture_Web_Service_Client_REST.Delete_Seg_SART_Pictures_Key(InfrastructureModule.Token, pic.RowPointer) == true)
                        {
                            FileInfo fi = SART_Common.Format_Cache_Filename(pic);
                            if (fi.Exists) fi.Delete();
                            Picture_List.Remove(pic);
                            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Deleted picture: {0} of {1}", ++picCount, pics.Count));
                            //Application_Helper.DoEvents();
                        }
                    }
                }
                catch (Authentication_Exception ae)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(ae));
                    eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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
            if (InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write) return false;
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
                    ew.Width = 600;
                    ew.dp_LabelText = " Edit Description ";
                    ew.dp_TextBoxText = Selected_Picture.Description;
                    Boolean? result = ew.ShowDialog();
                    if (result == true)
                    {
                        if (String.IsNullOrEmpty(ew.dp_TextBoxText) == false) break;
                    }
                    if (result == false)
                    {
                        return;
                    }
                }

                Selected_Picture.Description = ew.dp_TextBoxText;
                if (Selected_Picture.Description.Length > 300) Selected_Picture.Description = Selected_Picture.Description.Substring(0, 300);

                if (Syteline_PicND_Web_Service_Client_REST.Update_Seg_SART_Pictures_Nodata_Key(InfrastructureModule.Token, Selected_Picture) == false)
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
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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

        #region IKErr_CopyCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: IKErr_CopyCommand</summary>
        public DelegateCommand IKErr_CopyCommand { get; set; }
        private Boolean CanCommandIKErr_Copy() { return true; }
        private void CommandIKErr_Copy()
        {
            if (InfrastructureModule.Current_Work_Order != null)
            {
                if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Error_Code) == false)
                {
                    System.Windows.Clipboard.SetText(InfrastructureModule.Current_Work_Order.Error_Code);
                }
            }
        }

        /////////////////////////////////////////////
        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Add_PT_Config  -- WorkOrder_Configuration_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Add_PT_Config(SART_PT_Configuration config)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (config.Date_Time_Created.HasValue == false) config.Date_Time_Created = DateTime.Now;
                if (config.Date_Time_Entered.HasValue == false) config.Date_Time_Entered = DateTime.Now;

                if (config.ID == 0)
                {
                    foreach (var cnf in PT_Configurations_List)
                    {
                        if ((cnf.Type == config.Type) && (cnf.Date_Time_Entered.Value.Date == config.Date_Time_Entered.Value.Date))
                        {
                            cnf.Update(config);
                            cnf.ID = config.ID;
                            if (SART_PTCnf_Web_Service_Client_REST.Update_SART_PT_Configuration_Key(InfrastructureModule.Token, cnf) == false)
                            {
                                logger.Error("An error occurred while trying to update configuration to Segway database.");
                                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error! Could not update PT configuration");
                            }
                            return;
                        }
                    }
                    config = SART_PTCnf_Web_Service_Client_REST.Insert_SART_PT_Configuration_Key(InfrastructureModule.Token, config);
                    if (config != null)
                    {
                        eventAggregator.GetEvent<WorkOrder_Configuration_Refresh_Event>().Publish(true);
                        //PT_Configurations_List.Add(config);
                    }
                    else
                    {
                        logger.Error("An error occurred while trying to upload configuration to Segway database.");
                        eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error! Could not upload PT configuration");
                    }
                }
                else if (SART_PTCnf_Web_Service_Client_REST.Update_SART_PT_Configuration_Key(InfrastructureModule.Token, config) == false)
                {
                    logger.Error("An error occurred while trying to update configuration to Segway database.");
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error! Could not update PT configuration");
                }
                else if (PT_Configurations_List.Contains(config) == false)
                {
                    eventAggregator.GetEvent<WorkOrder_Configuration_Refresh_Event>().Publish(true);
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
            finally
            {
                OnPropertyChanged("PT_Configurations_List");
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region UpdateConfigFinal  -- WorkOrder_Configuration_Final_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void UpdateConfigFinal(Boolean success)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                IsConfigFinal = success;
                Load_Configurations();
                if (Syteline_WO_Web_Service_Client_REST.Update_SART_Work_Order_Object(InfrastructureModule.Token, Current_Work_Order) == false)
                {
                    throw new Exception($"Unable to save Work Order: {Current_Work_Order.SRO_Num}");
                }
                ConfigStartCommand.RaiseCanExecuteChanged();
                ConfigFinalCommand.RaiseCanExecuteChanged();
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region UpdateConfigStart  -- WorkOrder_Configuration_Start_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void UpdateConfigStart(Boolean StartSuccessful)
        {
            IsConfigStart = StartSuccessful;
            //Boolean config = ((IsConfigStart.HasValue == true) && (IsConfigStart.Value == true)) ||
            //    (IsConfigStartOverride == true) ||
            //    (IsConfigFinalOverride == true) ||
            //    (Current_Work_Order.Status == "C");
            //Work_Order_Events.Set_ToolBar(true, config, eventAggregator);


            Boolean config = ((IsConfigStart == true) || (IsConfigStartOverride == true) || (IsConfigFinalOverride == true));
            Boolean open = ((SART_ToolBar_Group_Manager.IsOpen == true) && (Current_Work_Order.Status == "O"));
            Work_Order_Events.Set_ToolBar(open, config, eventAggregator);


            Load_Configurations();
            if (Syteline_WO_Web_Service_Client_REST.Update_SART_Work_Order_Object(InfrastructureModule.Token, Current_Work_Order) == false)
            {
                throw new Exception($"Unable to save Work Order: {Current_Work_Order.SRO_Num}");
            }

            ConfigStartCommand.RaiseCanExecuteChanged();
            ConfigFinalCommand.RaiseCanExecuteChanged();
            propertyChanges();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Load_Work_Order  -- WorkOrder_Instance_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Load_Work_Order(Boolean wo)
        {
            try
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                OnPropertyChanged("Summary_Work_Order_Number");
                OnPropertyChanged("Summary_PT_Serial_Number");
                OnPropertyChanged("Summary_PT_Model");
                OnPropertyChanged("Summary_User_Name");
                OnPropertyChanged("Summary_Technician_Name");
                OnPropertyChanged("Summary_Date_Entered");
                OnPropertyChanged("Summary_Start_Date");

                Summary_Status = Status_Codes[Current_Work_Order.Stat_Code];
                InfoKey_Error_Code = Current_Work_Order.Error_Code;
                OnPropertyChanged("Summary_IsWarranty");
                Summary_Complaint = Current_Work_Order.Customer_Complaint;
                Summary_UnitCondition = Current_Work_Order.Unit_Condition;
                Summary_Technician_Observations = Current_Work_Order.Observation_Technician;
                Summary_Segway_Observations = Current_Work_Order.Segway_Observation;

                // here we need to add hidden toolbar items to active toolbar
                var itemManager = new ToolBarItemManager(eventAggregator);
                if (LoginContext.User_Level != UserLevels.Basic)
                {
                    itemManager.HideToolBarItems(!((Current_Work_Order.Config_Start_Override == true) || (IsConfigStart == true)));
                }
                else
                {
                    itemManager.HideToolBarItems(true);
                }
                Summary_Events_List = new ObservableCollection<SART_Events>(Work_Order_Events.Retrieve_Events(Current_Work_Order.Work_Order_ID));

                ///////////////////////////////////////////
                // This was setting an incorrect value and is not needed.  
                // Replacing with the RaiseCanExecuteChanged method calls which is needed.
                //UpdateConfigStart(Current_Work_Order.Is_Config_Start.HasValue && Current_Work_Order.Is_Config_Start.Value);
                ConfigStartCommand.RaiseCanExecuteChanged();
                ConfigFinalCommand.RaiseCanExecuteChanged();
                ///////////////////////////////////////////


                if ((Current_Work_Order.Is_CUA_Extracted == false) || (Current_Work_Order.Is_CUB_Extracted == false))
                {
                    foreach (SART_CU_Logs log in SART_Log_Web_Service_Client_REST.Get_SART_CU_Logs_WORK_ORDER(InfrastructureModule.Token, Current_Work_Order.Work_Order_ID))
                    {
                        if ((log.CU_Side == CAN_CU_Sides.A) && (Current_Work_Order.Is_CUA_Extracted == false))
                        {
                            Current_Work_Order.Is_CUA_Extracted = true;
                        }
                        if ((log.CU_Side == CAN_CU_Sides.B) && (Current_Work_Order.Is_CUB_Extracted == false))
                        {
                            Current_Work_Order.Is_CUB_Extracted = true;
                        }
                    }
                }
                if (Current_Work_Order != null) //&& (LoadCounter == 0))
                {
                    /////////////////////////////////////////////////////////////////////////////////
                    // Load Configuration
                    try
                    {
                        Load_Configurations();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(Exception_Helper.FormatExceptionString(ex));
                    }
                    // Load Configuration
                    /////////////////////////////////////////////////////////////////////////////////

                    /////////////////////////////////////////////////////////////////////////////////
                    // Load Pictures
                    try
                    {
                        List<Seg_SART_Pictures_Nodata> pictures = Syteline_PicND_Web_Service_Client_REST.Select_Seg_SART_Pictures_Nodata_SRONUM(InfrastructureModule.Token, Current_Work_Order.Work_Order_ID);
                        if (pictures != null)
                        {
                            if (pictures.Count > 1) pictures.Sort(new Seg_SART_Pictures_Nodata_CREATE_DATE_Comparer());
                            Picture_List = new ObservableCollection<Seg_SART_Pictures_Nodata>(pictures);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(Exception_Helper.FormatExceptionString(ex));
                    }
                    // Load Pictures
                    /////////////////////////////////////////////////////////////////////////////////
                }

                ConfigStartCommand.RaiseCanExecuteChanged();
                ConfigFinalCommand.RaiseCanExecuteChanged();

                eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
                eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);
                //eventAggregator.GetEvent<ToolBar_Selection_Event>().Publish("Work Order");

                InfrastructureModule.Original_Work_Order = InfrastructureModule.Current_Work_Order.Copy(true);

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
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                propertyChanges();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Close_SART  -- Close_Shell_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Close_SART(String mess)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (InfrastructureModule.WorkOrder_OpenMode == Open_Mode.Read_Write) CommandClose();
                else CommandCancel();
            }
            catch (Exception e)
            {
                logger.Error(Exception_Helper.FormatExceptionString(e));
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
        #region Config_Refresh_Handler  -- WorkOrder_Configuration_Refresh_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Config_Refresh_Handler(Boolean obj)
        {
            Load_Configurations();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Read_CU_Log  -- WorkOrder_Read_CU_Log_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Read_CU_Log(WorkOrder_Events woEvent)
        {
            if (woEvent == WorkOrder_Events.Extracted_CUA_Log)
            {
                //  Current_Work_Order.Is_CUA_Extracted = true;
                IsCUA_Extracted = true;
            }
            else if (woEvent == WorkOrder_Events.Extracted_CUB_Log)
            {
                //  Current_Work_Order.CU_B_Log = 1;
                IsCUB_Extracted = true;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region ApplicationLogout  -- Application_Logout_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void ApplicationLogout(Boolean close)
        {
            if (close)
            {
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Update_Audit  -- WorkOrder_AuditUpdate_Request_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Update_Audit(Boolean update)
        {
            if (update)
            {
                Summary_Events_List = Work_Order_Events.Retrieve_Events(Current_Work_Order.Work_Order_ID);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Received_CodeLoad_A_Notice  -- SART_CodeLoad_A_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Received_CU_CodeLoad_A_Notice(Boolean status)
        {
            Current_Work_Order.Is_CUA_Loaded = status;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Received_CodeLoad_B_Notice  -- SART_CodeLoad_B_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Received_CU_CodeLoad_B_Notice(Boolean status)
        {
            Current_Work_Order.Is_CUB_Loaded = status;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Received_BSA_CodeLoad_Notice

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Received_BSA_CodeLoad_Notice
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Received_BSA_CodeLoad_Notice(Boolean status)
        {
            Current_Work_Order.Is_BSA_Code_Loaded = status;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Received_MotorTest_Done_Notice(Boolean status)
        {
            Current_Work_Order.Is_NormalMotor_Test = status;
        }

        private void Received_BSATest_Done_Notice(Boolean status)
        {
            Current_Work_Order.Is_BSA_Test = status;
        }

        private void Received_RiderDetectTest_Done_Notice(Boolean status)
        {
            Current_Work_Order.Is_RiderDetect_Test = status;
        }

        private void Received_LEDTest_Done_Notice(Boolean status)
        {
            Current_Work_Order.Is_LED_Test = status;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Status_Changed_Handler  -- WorkOrder_Status_Changed_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Status_Change_Handler(String code)
        {
            InfrastructureModule.Current_Work_Order.Stat_Code = code; //.Working_Status = code;
            Dictionary<String, String> stsCodes = null;
            if (InfrastructureModule.Token != null)
            {
                stsCodes = Common.Get_Statuses(InfrastructureModule.Token);
            }

            if (stsCodes != null)
            {
                if (stsCodes.ContainsKey(code))
                {
                    Summary_Status = stsCodes[code];
                }
            }
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
        #region WorkOrder_Cancel_Handler  -- SART_WorkOrder_Cancel_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Cancel_Handler(Boolean cancel)
        {
            Reset();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Login_Handler  -- Event: Application_Login_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Login_Handler(String name)
        {
            _LoginContext = null;
            Setup_ContextMenu();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Logout_Handler  -- Event: Application_Logout_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Logout_Handler(Boolean close)
        {
            if (close)
            {
                try
                {
                    logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                    eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                    logger.Debug("Closing Work Order");
                    Work_Order_Events.Close_Work_Order(eventAggregator);
                    logger.Debug("Resetting Expanded Controls");
                    Reset_Expanded_Controls();

                    logger.Debug("set the default drop panel");
                    eventAggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Work Order", Work_Order_Open_Control.Control_Name));
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
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_RideTest_Handler  -- Event: WorkOrder_RideTest_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_RideTest_Handler(Boolean ridetest)
        {
            IsRideTest = ridetest;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region JTag_Request_Serial_Handler  -- Event: JTag_Request_Serial_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void JTag_Request_Serial_Handler(String componentName)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                logger.Debug("Requesting: {0}", componentName);

                List<SART_PT_Configuration> configList = null;
                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    configList = new List<SART_PT_Configuration>(PT_Configurations_List);
                });

                String serial = null;
                if ((configList != null) && (configList.Count > 0))
                {
                    foreach (var item in configList)
                    {
                        if (componentName == "CUA Serial")
                        {
                            if ((serial == null) || (String.IsNullOrEmpty(item.CUA_Serial) == false)) serial = item.CUA_Serial;
                        }
                        else if (componentName == "CUB Serial")
                        {
                            if ((serial == null) || (String.IsNullOrEmpty(item.CUB_Serial) == false)) serial = item.CUB_Serial;
                        }
                        else if (componentName == "BSA Serial")
                        {
                            if ((serial == null) || (String.IsNullOrEmpty(item.BSA_A_Serial) == false)) serial = item.BSA_A_Serial;
                        }
                    }
                }
                else
                {
                    logger.Warn("No configuration data");
                }

                logger.Debug("Component: {0}, Serial: {0}", componentName, serial);
                eventAggregator.GetEvent<JTag_Response_Serial_Event>().Publish(new KeyValuePair<String, String>(componentName, serial));
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
        #region NavigateTo_Login_Handler  -- Event: NavigateTo_Login_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void NavigateTo_Login_Handler(String obj)
        {
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
            //eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            eventAggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Work Order", Work_Order_Summary_Control.Control_Name));
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Work Order", Work_Order_Summary_Control.Control_Name));
            eventAggregator.GetEvent<ToolBar_Selection_Event>().Publish("Work Order");
            if (LoginContext.User_Level != UserLevels.Basic)
            {
                //itemManager.HideToolBarItems(!((Current_Work_Order.Is_Config_Start.HasValue && Current_Work_Order.Is_Config_Start.Value) || Current_Work_Order.Is_Config_Override));
                //  set the visibility properties for proper configuration list displays
                ConfigChoiceCheckboxVisibility = Visibility.Visible;
                WorkOrderHistoryVisibility = Visibility.Collapsed;
                SegwayObservationsVisibility = Visibility.Collapsed;
            }
            else
            {
                /////////////////////////////////////////////////////////////////////////////////////////////////////////
                // Removed this secion because the Set_ToolBar method below will handle this.
                /////////////////////////////////////////////////////////////////////////////////////////////////////////
                //var itemManager = new ToolBarItemManager(eventAggregator);
                //itemManager.HideToolBarItems(true);
                /////////////////////////////////////////////////////////////////////////////////////////////////////////


                //  set the visibility properties for proper configuration list displays
                ConfigChoiceCheckboxVisibility = Visibility.Collapsed;
                WorkOrderHistoryVisibility = Visibility.Visible;
                ConfigSummaryVisibility = Visibility.Collapsed;
                SegwayObservationsVisibility = Visibility.Visible;
            }
            ConfigFinalCommand.RaiseCanExecuteChanged();
            ConfigStartCommand.RaiseCanExecuteChanged();
            CloseCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            PictureAddCommand.RaiseCanExecuteChanged();
            PictureDelCommand.RaiseCanExecuteChanged();
            PictureEditCommand.RaiseCanExecuteChanged();
            PictureOpenCommand.RaiseCanExecuteChanged();
            propertyChanges();
            if (Current_Work_Order != null)
            {
                Boolean config = ((IsConfigStart == true) || (IsConfigStartOverride == true) || (IsConfigFinalOverride == true));
                Boolean open = ((SART_ToolBar_Group_Manager.IsOpen == true) && (Current_Work_Order.Status == "O"));
                Work_Order_Events.Set_ToolBar(open, config, eventAggregator);
            }
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private void propertyChanges()
        {
            OnPropertyChanged("Component_Data");
            OnPropertyChanged("ConfigChoiceCheckboxVisibility");
            OnPropertyChanged("ConfigDetailVisibility");
            OnPropertyChanged("ConfigSummaryVisibility");
            OnPropertyChanged("Current_Error_Code_Observations");
            OnPropertyChanged("Current_Segway_Observations");
            OnPropertyChanged("Current_Technician_Observations");
            OnPropertyChanged("Current_Unit_Condition");
            OnPropertyChanged("InfoKey_Error_Code");
            OnPropertyChanged("IsAuditExpanded");
            OnPropertyChanged("IsBSATest");
            OnPropertyChanged("IsBSA_Coded");
            OnPropertyChanged("IsCUA_Coded");
            OnPropertyChanged("IsCUA_Extracted");
            OnPropertyChanged("IsCUB_Coded");
            OnPropertyChanged("IsCUB_Extracted");
            OnPropertyChanged("IsConfigEnd");
            OnPropertyChanged("IsConfigFinal");
            OnPropertyChanged("IsConfigFinalOverride");
            OnPropertyChanged("IsConfigStart");
            OnPropertyChanged("IsConfigStartOverride");
            OnPropertyChanged("IsConfigurationsExpanded");
            OnPropertyChanged("IsDetailedView");
            OnPropertyChanged("IsLEDTest");
            OnPropertyChanged("IsLeftMotorTest");
            OnPropertyChanged("IsNormalMotorTest");
            OnPropertyChanged("IsObservationsExpanded");
            OnPropertyChanged("IsRideTest");
            OnPropertyChanged("IsPicturesExpanded");
            OnPropertyChanged("IsRiderDetect");
            OnPropertyChanged("ObservationsIndex");
            OnPropertyChanged("PT_Configurations_List");
            OnPropertyChanged("Picture_List");
            OnPropertyChanged("SegwayObservationsVisibility");
            OnPropertyChanged("Selected_Picture");
            OnPropertyChanged("Status");
            OnPropertyChanged("Summary_Complaint");
            OnPropertyChanged("Summary_Customer");
            OnPropertyChanged("Summary_Date_Entered");
            OnPropertyChanged("Summary_Events_List");
            OnPropertyChanged("Summary_Final_Config_Override_Visibility");
            OnPropertyChanged("Summary_Final_Config_Visibility");
            OnPropertyChanged("Summary_Image");
            OnPropertyChanged("Summary_IsWarranty");
            OnPropertyChanged("Summary_PT_Model");
            OnPropertyChanged("Summary_PT_Serial_Number");
            OnPropertyChanged("Summary_Segway_Observations");
            OnPropertyChanged("Summary_Start_Config_Override_Visibility");
            OnPropertyChanged("Summary_Start_Config_Visibility");
            OnPropertyChanged("Summary_Start_Date");
            OnPropertyChanged("Summary_Status");
            OnPropertyChanged("Summary_Technician_Name");
            OnPropertyChanged("Summary_Technician_Observations");
            OnPropertyChanged("Summary_UnitCondition");
            OnPropertyChanged("Summary_User_Name");
            OnPropertyChanged("Summary_Work_Order_Number");
            OnPropertyChanged("TaskSummary_Image");
            OnPropertyChanged("WorkOrderHistoryVisibility");
            OnPropertyChanged("Work_Order_Number_Color");
        }


        //private SART_PT_Configuration Find_Configuration(String Work_Order_ID, ConfigurationTypes type)
        //{
        //    foreach (SART_PT_Configuration cnf in PT_Configurations_List)
        //    {
        //        if (cnf.Work_Order != Work_Order_ID) continue;
        //        if (cnf.ConfigType != type) continue;
        //        return cnf;
        //    }
        //    return null;
        //}


        private void Load_Configurations()
        {
            Boolean error = false;
            String errMsg = null;

            List<SART_PT_Configuration> ptconfigs = PTConfiguration.Get_PT_Configuration(Current_Work_Order.PT_Serial, Current_Work_Order.Work_Order_ID, out error, out errMsg);
            if (error == true)
            {
                PopupColor = Brushes.Pink;
                PopupMessage = String.Format("The following error(s) occurred while retrieving configuration data:\n{0}", errMsg);
                PopupOpen = true;
                logger.Error(PopupMessage);
            }
            if (ptconfigs == null) ptconfigs = new List<SART_PT_Configuration>();
            else if (ptconfigs.Count > 1) ptconfigs.Sort(new SART_PT_Configuration_Created_Comparer());

            //container.RegisterInstance<List<SART_PT_Configuration>>("Configurations", ptconfigs);

            PT_Configurations_List = new ObservableCollection<SART_PT_Configuration>(ptconfigs);
        }


        private void Revert_User_Level()
        {
            try
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //  TO DO:  Note this will need to be fixed when we have different user levels - all non-experts are getting set to basic every time!
                if (LoginContext != null)
                {
                    if ((LoginContext.User_Level < UserLevels.Expert) && (String.IsNullOrEmpty(LoginContext.UserName) == false))
                    {
                        logger.Debug("Resetting Non-Expert user to Basic");

                        Service_Users su = Authentication_User_Web_Service_Client_REST.Select_Service_Users_USERNAME(InfrastructureModule.Token, LoginContext.UserName);
                        if (su != null)
                        {
                            String tool = App_Settings_Helper.GetConfigurationValue("ToolName");
                            if (String.IsNullOrEmpty(tool) == false)
                            {
                                if (String.Compare("Remote Service Tool", tool, true) == 0)
                                {
                                    tool = App_Settings_Helper.GetConfigurationValue("Remote Service Tool", "RST");
                                }
                            }
                            Service_User_Access sua = su.AccessInfo(tool);
                            if (sua != null)
                            {
                                logger.Debug("Default User Level: {0}", sua.User_Default_Level);
                                sua.Access_Level = sua.User_Default_Level;
                                if (String.IsNullOrEmpty(sua.Access_Level) == true)
                                {
                                    sua.Access_Level = UserLevels.Basic.ToString();
                                    logger.Debug("Default User Level set to: {0}", sua.Access_Level);
                                }
                                if (Authentication_Access_Web_Service_Client_REST.Update_Service_User_Access_Object(InfrastructureModule.Token, sua))
                                {
                                    logger.Debug("Successfully reset Non-Expert user to {0}", sua.Access_Level);
                                }
                                else
                                {
                                    logger.Warn("Update user's access record failed");
                                }
                            }
                            else
                            {
                                logger.Warn("Did not find tool: {0} for user: {1}", tool, LoginContext.UserName);
                            }
                        }
                        else
                        {
                            logger.Warn("Did not find user's access record");
                        }
                    }
                }
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

            //  TO DO:  
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }


        private void Reset_Expanded_Controls()
        {
            IsObservationsExpanded = false;
            IsPicturesExpanded = false;
            IsAuditExpanded = false;
            IsConfigurationsExpanded = true;
            propertyChanges();
        }

        private void Reset()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                PT_Configurations_List = null;
                Summary_Events_List = null;
                Picture_List = null;
                Selected_Picture = null;
                _Current_Unit_Condition = "";
                _Current_Technician_Observations = "";
                _Current_Error_Code_Observations = "";
                _Current_Segway_Observations = "";
                propertyChanges();
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

        private void Display_SART_Event(Object obj, RoutedEventArgs args)
        {
            if (Selected_Summary_Event != null)
            {
                var EntryList = SART_EVLOG_Web_Service_Client_REST.Select_SART_Event_Log_Entry_OBJECT_ID(InfrastructureModule.Token, Selected_Summary_Event.Object_ID);
                if (EntryList != null)
                {
                    List<Progress_View_Item> items = new List<Progress_View_Item>();
                    foreach (var entry in EntryList)
                    {
                        items.Add(new Progress_View_Item(entry.ID, entry.EventStatus, entry.Message, entry.Timestamp_Start, entry.Timestamp_End, entry.Error_Description));
                    }

                    Brush woColor;
                    if (InfrastructureModule.Current_Work_Order == null)
                    {
                        woColor = new BrushConverter().ConvertFromString("#FF67A1DC") as Brush;
                    }
                    else if (InfrastructureModule.Current_Work_Order.Priority == "Incident")
                    {
                        woColor = Brushes.Red;
                    }
                    else
                    {
                        woColor = new BrushConverter().ConvertFromString("#FF67A1DC") as Brush;
                    }

                    Progression_Window pw = new Progression_Window(items, Selected_Summary_Event, InfrastructureModule.Current_Work_Order.PT_Serial, woColor);
                    pw.Show();
                }
            }
        }


        private void Setup_ContextMenu()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Boolean enable = false;
                if (LoginContext.User_Level != UserLevels.Master)
                {
                    logger.Debug("User is not authorized to add ContextMenus");
                }
                else enable = true;

                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    ((Work_Order_Summary_Control)View).cbBSATest.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbConfStart.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbConfStartOR.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbBSA_Code.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbCUA_Code.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbCUB_Code.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbConfigFinal.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbConfigFinalOR.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbCUA_Log.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbCUB_Log.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbLEDTest.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbMotorTest.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbRiderDetectTest.IsEnabled = enable;
                    ((Work_Order_Summary_Control)View).cbRideTest.IsEnabled = enable;
                });

                return;
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
