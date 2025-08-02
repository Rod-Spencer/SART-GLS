using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Database.Objects;
using Segway.Login.Objects;
using Segway.Manufacturing.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.Modules.AddWindow;
using Segway.Service.SART.Client.REST;
using Segway.Syteline.Client.REST;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Segway.Modules.SART.Repair
{
    /// <summary>Public Class</summary>
    public class Repair_ViewModel : ViewModelBase, Repair_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator aggregator;

        private Boolean UpdatingComponentLines = false;
        //private SART_WO_Components store_Selected_Component = null;

        /// <summary>Contructor</summary>
        /// <param name="view">Repair_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Repair_ViewModel(Repair_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.aggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<WorkOrder_Opened_Event>().Subscribe(Open_WorkOrder_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<WorkOrder_Save_Event>().Subscribe(WorkOrder_Save_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<WorkOrder_Configuration_Final_UpdateDB_Event>().Subscribe(WorkOrder_Configuration_Final_UpdateDB_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Setups

            ObservationSaveCommand = new DelegateCommand(CommandObservationSave, CanCommandObservationSave);
            //SaveLinesCommand = new DelegateCommand(CommandSaveLines, CanCommandSaveLines);
            //AddPartCommand = new DelegateCommand(CommandAddPart, CanCommandAddPart);
            ReadyToQuoteCommand = new DelegateCommand(CommandReadyToQuote, CanCommandReadyToQuote);
            DeletePartCommand = new DelegateCommand(CommandDeletePart, CanCommandDeletePart);
            UnSelectCommand = new DelegateCommand(CommandUnSelect, CanCommandUnSelect);
            RefreshCommand = new DelegateCommand(CommandRefresh, CanCommandRefresh);

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

        #region Part_Types

        private Dictionary<String, Segway_Part_Type_Xref> _Part_Types;

        /// <summary>Property Part_Types of type Dictionary(String, String)</summary>
        public Dictionary<String, Segway_Part_Type_Xref> Part_Types
        {
            get
            {
                if (_Part_Types == null)
                {
                    var ptxs = Manufacturing_SPTX_Web_Service_Client_REST.Select_Segway_Part_Type_Xref_All(InfrastructureModule.Token);
                    if (ptxs != null)
                    {
                        _Part_Types = new Dictionary<String, Segway_Part_Type_Xref>();
                        foreach (var ptx in ptxs)
                        {
                            _Part_Types.Add(ptx.Assembly_Part_Number, ptx);
                        }
                    }
                }
                return _Part_Types;
            }
        }

        #endregion


        #region Selected_PartType

        private Segway_Part_Type_Xref _Selected_PartType;

        /// <summary>Property Selected_PartType of type Segway_Part_Type_Xref</summary>
        public Segway_Part_Type_Xref Selected_PartType
        {
            get { return _Selected_PartType; }
            set { _Selected_PartType = value; }
        }

        #endregion


        #region Part_Number_Cross_Ref

        private List<Segway_Part_Type_Xref> _Part_Number_Cross_Ref;

        /// <summary>Property Part_Number_Cross_Ref of type List of Segway_Part_Type_Xref</summary>
        public List<Segway_Part_Type_Xref> Part_Number_Cross_Ref
        {
            get
            {
                if ((_Part_Number_Cross_Ref == null) || (_Part_Number_Cross_Ref.Count == 0))
                {
                    if (InfrastructureModule.Token != null)
                    {
                        _Part_Number_Cross_Ref = Manufacturing_SPTX_Web_Service_Client_REST.Select_Segway_Part_Type_Xref_All(InfrastructureModule.Token);
                    }
                    if (_Part_Number_Cross_Ref == null) return new List<Segway_Part_Type_Xref>();
                }
                return _Part_Number_Cross_Ref;
            }
            set
            {
                _Part_Number_Cross_Ref = value;
            }
        }

        #endregion


        private static String Repair_Last_10_FileName = "Repair Last 10";

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        #region ImagePath

        private BitmapImage _ImagePath = null;

        /// <summary>Property ImagePath of type String</summary>
        public BitmapImage ImagePath
        {
            get
            {
                if (_ImagePath == null) _ImagePath = Image_Helper.ImageFromEmbedded("Images.Repair.png");
                return _ImagePath;
            }
            set { OnPropertyChanged("ImagePath"); }
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

        #region ObservationsIndex

        private int _ObservationsIndex = 1;

        /// <summary>Property ObservationsIndex of type int</summary>
        public int ObservationsIndex
        {
            get { return _ObservationsIndex; }
            set
            {
                _ObservationsIndex = value;
                OnPropertyChanged("ObservationsIndex");
                ObservationSaveCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Summary_Technician_Observations

        /// <summary>Property Summary_Technician_Observations of type String</summary>
        public String Summary_Technician_Observations
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Observation_Technician;
            }
            set
            {
                InfrastructureModule.Current_Work_Order.Observation_Technician = value;
                OnPropertyChanged("Summary_Technician_Observations");
            }
        }

        #endregion

        #region Current_Technician_Observations

        private String _Current_Technician_Observations;

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

        #region Summary_Repair_Comments

        /// <summary>Property Summary_Repair_Comments of type String</summary>
        public String Summary_Repair_Comments
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Repair_Performed_Note;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order != null)
                {
                    InfrastructureModule.Current_Work_Order.Repair_Performed_Note = value;
                    OnPropertyChanged("Summary_Repair_Comments");
                    ReadyToQuoteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Current_Repair_Comments

        private String _Current_Repair_Comments;

        /// <summary>Property Current_Repair_Comments of type String</summary>
        public String Current_Repair_Comments
        {
            get { return _Current_Repair_Comments; }
            set
            {
                _Current_Repair_Comments = value;
                OnPropertyChanged("Current_Repair_Comments");
                ObservationSaveCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region PT_Components_List

        private ObservableCollection<SART_WO_Components> _PT_Components_List;

        /// <summary>Property PT_Components_List of type List(SART_WO_Components)</summary>
        public ObservableCollection<SART_WO_Components> PT_Components_List
        {
            get { return _PT_Components_List; }
            set
            {
                _PT_Components_List = value;

                OnPropertyChanged("PT_Components_List");
                OnPropertyChanged("RepairedPartCount");
            }
        }

        #endregion

        #region Selected_Component

        private SART_WO_Components _Selected_Component;

        /// <summary>Property Selected_Component of type SART_WO_Components</summary>
        public SART_WO_Components Selected_Component
        {
            get { return _Selected_Component; }
            set
            {
                //if (store_Selected_Component != null)
                //{
                //    if (_Selected_Component != store_Selected_Component)
                //    {
                //        SART_2012_Web_Service_Client.Update_SART_WO_Components_Key(InfrastructureModule.Token, _Selected_Component);
                //    }
                //}
                _Selected_Component = value;
                //if (value == null)
                //{
                //    store_Selected_Component = null;
                //}
                //else if (store_Selected_Component != null)
                //{
                //    store_Selected_Component.Copy(value, true);
                //}
                //else
                //{
                //    store_Selected_Component = new SART_WO_Components();
                //    store_Selected_Component.Copy(value, true);
                //}

                OnPropertyChanged("Selected_Component");
                //if (_Selected_Component != null)
                //{
                //    if (String.IsNullOrEmpty(_Selected_Component.Unit_Of_Measure) == true)
                //    {
                //        _SelectedUM = "EA";
                //    }
                //    else
                //    {
                //        foreach (var um in UMList)
                //        {
                //            if (um.StartsWith(_Selected_Component.Unit_Of_Measure) == true)
                //            {
                //                _SelectedUM = um;
                //                break;
                //            }
                //        }
                //    }
                //}
                //OnPropertyChanged("SelectedUM");
                //OnPropertyChanged("IsAddPartsExpanded");
                //OnPropertyChanged("PartQuantity");
                //OnPropertyChanged("Part_Number");
                //OnPropertyChanged("Part_Name");
                //OnPropertyChanged("OldSerialNumber");
                //OnPropertyChanged("NewSerialNumber");
                //OnPropertyChanged("SelectedBillableCode");
                //OnPropertyChanged("SelectedChangeType");
                //OnPropertyChanged("IsApproved");
                //OnPropertyChanged("IsSelected");
                //OnPropertyChanged("IsDeclined");
                //OnPropertyChanged("IsInstalledYes");
                //OnPropertyChanged("IsInstalledNo");

                DeletePartCommand.RaiseCanExecuteChanged();
                UnSelectCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region RepairedPartCount

        /// <summary>Property RepairedPartCount of type String</summary>
        public int RepairedPartCount
        {
            get
            {
                if (_PT_Components_List == null) return 0;
                return _PT_Components_List.Count;
            }
            set
            {
                OnPropertyChanged("RepairedPartCount");
                //SaveLinesCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region IsNotReadOnly

        /// <summary>Property IsNotReadOnly of type Boolean</summary>
        public Boolean IsNotReadOnly
        {
            get { return InfrastructureModule.WorkOrder_OpenMode == Open_Mode.Read_Write; }
            set { OnPropertyChanged("IsNotReadOnly"); }
        }

        #endregion

        #region IsReadOnly

        private Boolean _IsReadOnly;

        /// <summary>Property IsReadOnly of type Boolean</summary>
        public Boolean IsReadOnly
        {
            get { return InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write; }
            set
            {
                _IsReadOnly = value;
                OnPropertyChanged("IsReadOnly");
            }
        }

        #endregion

        #region ChangeTypeList

        private ObservableCollection<Change_Types> _ChangeTypeList;

        /// <summary>Property ChangeTypeList of type ObservableCollection(String)</summary>
        public ObservableCollection<Change_Types> ChangeTypeList
        {
            get
            {
                if (_ChangeTypeList == null)
                {
                    Load_Change_Codes();
                }
                return _ChangeTypeList;
            }
            set
            {
                _ChangeTypeList = value;
                OnPropertyChanged("ChangeTypeList");
            }
        }

        #endregion


        #region ApprovalTypeList

        private ObservableCollection<Approval_Types> _ApprovalTypeList;

        /// <summary>Property ApprovalTypeList of type ObservableCollection(String)</summary>
        public ObservableCollection<Approval_Types> ApprovalTypeList
        {
            get
            {
                if (_ApprovalTypeList == null)
                {
                    Load_Approval_Codes();
                }
                return _ApprovalTypeList;
            }
            set
            {
                _ApprovalTypeList = value;
                OnPropertyChanged("ApprovalTypeList");
            }
        }

        #endregion


        #region InstallTypeList

        private ObservableCollection<Install_Types> _InstallTypeList;

        /// <summary>Property InstallTypeList of type ObservableCollection(Install_Types)</summary>
        public ObservableCollection<Install_Types> InstallTypeList
        {
            get
            {
                if (_InstallTypeList == null)
                {
                    Load_Install_Codes();
                }
                return _InstallTypeList;
            }
            set
            {
                _InstallTypeList = value;
                OnPropertyChanged("InstallTypeList");
            }
        }

        #endregion

        #region BillableCodeList

        private ObservableCollection<Billing_Types> _BillableCodeList;

        /// <summary>Property BillableCodeList of type ObservableCollection(String)</summary>
        public ObservableCollection<Billing_Types> BillableCodeList
        {
            get
            {
                if (_BillableCodeList == null)
                {
                    Load_Billing_Codes();
                }
                return _BillableCodeList;
            }
            set
            {
                _BillableCodeList = value;
                OnPropertyChanged("BillableCodeList");
            }
        }

        #endregion


        #region LocationCodeList

        private ObservableCollection<Location_Types> _LocationCodeList;

        /// <summary>Property LocationCodeList of type ObservableCollection of Location_Codes</summary>
        public ObservableCollection<Location_Types> LocationCodeList
        {
            get
            {
                if (_LocationCodeList == null)
                {
                    Load_Location_Codes();
                }
                return _LocationCodeList;
            }
            set
            {
                _LocationCodeList = value;
                OnPropertyChanged("LocationCodeList");
            }
        }

        #endregion


        #region UMList

        private ObservableCollection<Units_of_Measure_Types> _UMList;

        /// <summary>Property UMList of type ObservableCollection(String)</summary>
        public ObservableCollection<Units_of_Measure_Types> UMList
        {
            get
            {
                if (_UMList == null)
                {
                    Load_UM_Codes();
                }
                return _UMList;
            }
            set
            {
                _UMList = value;
                OnPropertyChanged("UMList");
            }
        }

        #endregion

        #region IsChecked_NoPartsRequired

        private Boolean _IsChecked_NoPartsRequired;

        /// <summary>Property IsChecked_NoPartsRequired of type Boolean</summary>
        public Boolean IsChecked_NoPartsRequired
        {
            get { return _IsChecked_NoPartsRequired; }
            set
            {
                _IsChecked_NoPartsRequired = value;
                OnPropertyChanged("IsChecked_NoPartsRequired");
                ReadyToQuoteCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region RepairHours

        /// <summary>ViewModel property: RepairHours of type: String points to InfrastructureModule.Current_Work_Order.Labor_Hours</summary>
        public String RepairHours
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                if (InfrastructureModule.Current_Work_Order.Labor_Hours.HasValue == false) return null;
                return InfrastructureModule.Current_Work_Order.Labor_Hours.Value.ToString("N2");
            }
            set
            {
                try
                {
                    if (InfrastructureModule.Current_Work_Order == null) return;
                    InfrastructureModule.Current_Work_Order.Labor_Hours = Single.Parse(value);
                }
                catch
                {
                    InfrastructureModule.Current_Work_Order.Labor_Hours = null;
                }
                finally
                {
                    OnPropertyChanged("RepairHours");
                }
            }
        }

        #endregion

        #region Popup Controls

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

        #region PopupOpen

        private Boolean _PopupOpen;

        /// <summary>Property PopupOpen of type Boolean</summary>
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

        #region SelectedTab

        private TabItem _SelectedTab;

        /// <summary>Property SelectedTab of type TabItem</summary>
        public TabItem SelectedTab
        {
            get { return _SelectedTab; }
            set
            {
                _SelectedTab = value;
                OnPropertyChanged("SelectedTab");
                OnPropertyChanged("IsReadOnly");
                ObservationSaveCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion


        #region New_Part_Number

        private String _New_Part_Number;

        /// <summary>Property New_Part_Number of type String</summary>
        public String New_Part_Number
        {
            get { return _New_Part_Number; }
            set
            {
                _New_Part_Number = value;
                OnPropertyChanged("New_Part_Number");
            }
        }

        #endregion

        #region QuoteVisibility

        /// <summary>Property QuoteVisibility of type Visibility</summary>
        public Visibility QuoteVisibility
        {
            get
            {
                if (Application_Helper.Application_Name().Contains("Pilot") == true) return Visibility.Collapsed;
                return Visibility.Visible;
            }
            set
            {
                OnPropertyChanged("QuoteVisibility");
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region ObservationSaveCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ObservationSaveCommand</summary>
        public DelegateCommand ObservationSaveCommand { get; set; }
        private Boolean CanCommandObservationSave()
        {
            if (InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write) return false;
            if (ObservationsIndex == 0)
            {
                if (String.IsNullOrWhiteSpace(Current_Technician_Observations) == true) return false;
                return true;
            }
            else if (ObservationsIndex == 1)
            {
                if (String.IsNullOrWhiteSpace(Current_Repair_Comments) == true) return false;
                return true;
            }
            return false;
        }

        private void CommandObservationSave()
        {
            if (ObservationsIndex == 0)
            {
                // Technician Observations

                StringBuilder sb = new StringBuilder(Summary_Technician_Observations);
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(String.Format("{0} - {1}:", LoginContext.UserName, DateTime.Now));
                sb.AppendLine(Current_Technician_Observations);
                Summary_Technician_Observations = sb.ToString();
                //Work_Order_Events.Update_Work_Order();
                Current_Technician_Observations = "";
            }
            else if (ObservationsIndex == 1)
            {
                // Repair Comments
                StringBuilder sb = new StringBuilder(Summary_Repair_Comments);
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(String.Format("{0} - {1}:", LoginContext.UserName, DateTime.Now));
                sb.AppendLine(Current_Repair_Comments);
                Summary_Repair_Comments = sb.ToString();
                //Work_Order_Events.Update_Work_Order();
                Current_Repair_Comments = "";
            }
        }

        /////////////////////////////////////////////
        #endregion


        //#region SaveLinesCommand
        ///////////////////////////////////////////////

        ///// <summary>Delegate Command: SaveLinesCommand</summary>
        //public DelegateCommand SaveLinesCommand { get; set; }
        //private Boolean CanCommandSaveLines()
        //{
        //    if (RepairedPartCount > 0) return true;
        //    return false;
        //}

        //private void CommandSaveLines()
        //{
        //    try
        //    {
        //        logger.Trace("Entered");

        //        foreach (SART_WO_Components comp in PT_Components_List)
        //        {
        //            if (comp.RowPointer == Guid.Empty)
        //            {
        //                SART_WO_Components newcomp = SART_2012_Web_Service_Client.Insert_SART_WO_Components_Key(InfrastructureModule.Token, comp);
        //                if (newcomp == null)
        //                {
        //                    logger.Warn("Add component sequence {0} failed for Work Order: {1}", comp.Sequence, comp.Work_Order_ID);

        //                    comp.Work_Order_ID = String.Format("LS{0}", Strings.Digits(comp.Work_Order_ID).PadLeft(8, '0'));
        //                    newcomp = SART_2012_Web_Service_Client.Insert_SART_WO_Components_Key(InfrastructureModule.Token, comp);
        //                }
        //                if (newcomp == null)
        //                {
        //                    String msg = String.Format("Add component sequence {0} failed for Work Order: {1}", comp.Sequence, comp.Work_Order_ID);
        //                    logger.Warn(msg);
        //                    MessageBox.Show(msg);
        //                    return;
        //                }
        //                comp.RowPointer = newcomp.RowPointer;
        //            }
        //            else
        //            {
        //                SART_2012_Web_Service_Client.Update_SART_WO_Components_Key(InfrastructureModule.Token, comp);
        //            }
        //        }

        //        Type stType = typeof(Service_Table);

        //        Service_Table sr = SART_2012_Web_Service_Client.Get_Service_Table_SERVICE_REQUEST(InfrastructureModule.Token, InfrastructureModule.Current_Work_Order.Work_Order_ID);
        //        if (sr != null)
        //        {
        //            for (int x = 1; x <= 15; x++)
        //            {

        //                if (x <= PT_Components_List.Count)
        //                {
        //                    SART_WO_Components comp = PT_Components_List[x - 1];

        //                    String name = String.Format("Kit_PN_{0}", x);
        //                    PropertyInfo pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        if (String.IsNullOrEmpty(comp.Part_Number) == false) pi.SetValue(sr, comp.Part_Number, null);
        //                        else pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Part_Name_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        if (String.IsNullOrEmpty(comp.Part_Name) == false) pi.SetValue(sr, comp.Part_Name, null);
        //                        else pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Old_SN_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        if (String.IsNullOrEmpty(comp.Serial_Number_Old) == false) pi.SetValue(sr, comp.Serial_Number_Old, null);
        //                        else pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("New_SN_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        if (String.IsNullOrEmpty(comp.Serial_Number_New) == false) pi.SetValue(sr, comp.Serial_Number_New, null);
        //                        else pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Qty_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        if (String.IsNullOrEmpty(comp.Quantity) == false) pi.SetValue(sr, comp.Quantity, null);
        //                        else pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Warr_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        if (String.IsNullOrEmpty(comp.Billable_Code) == false)
        //                        {
        //                            if (String.Compare(comp.Billable_Code, "Warranty", true) == 0)
        //                            {
        //                                pi.SetValue(sr, "Yes", null);
        //                            }
        //                            else pi.SetValue(sr, null, null);
        //                        }
        //                        else pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Required_Suggested_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        if (String.IsNullOrEmpty(comp.Change_Type) == false) pi.SetValue(sr, comp.Change_Type, null);
        //                        else pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Approved_Declined_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        if (String.IsNullOrEmpty(comp.Change_Approval) == false) pi.SetValue(sr, comp.Change_Approval, null);
        //                        else pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Service_Act_Code_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Installed_YN_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        if (String.IsNullOrEmpty(comp.Installed) == false) pi.SetValue(sr, comp.Installed, null);
        //                        else pi.SetValue(sr, null, null);
        //                    }
        //                }
        //                else
        //                {
        //                    PropertyInfo pi = stType.GetProperty(String.Format("Kit_PN_{0}", x));
        //                    if (pi != null) pi.SetValue(sr, null, null);

        //                    pi = stType.GetProperty(String.Format("Part_Name_{0}", x));
        //                    if (pi != null) pi.SetValue(sr, null, null);

        //                    pi = stType.GetProperty(String.Format("Old_SN_{0}", x));
        //                    if (pi != null)
        //                    {
        //                        pi.SetValue(sr, null, null);
        //                    }

        //                    String name = String.Format("New_SN_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Qty_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Warr_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Required_Suggested_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Approved_Declined_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Service_Act_Code_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        pi.SetValue(sr, null, null);
        //                    }

        //                    name = String.Format("Installed_YN_{0}", x);
        //                    pi = stType.GetProperty(name);
        //                    if (pi != null)
        //                    {
        //                        pi.SetValue(sr, null, null);
        //                    }
        //                }

        //            }
        //            if (SART_2012_Web_Service_Client.Update_Service_Table(InfrastructureModule.Token, sr) == true)
        //            {
        //                PopupColor = Brushes.LightGreen;
        //                PopupMessage = "Component lines were successfully save.";
        //            }
        //            else
        //            {
        //                PopupColor = Brushes.Pink;
        //                PopupMessage = "An error occurred while saving component lines.";
        //            }
        //            PopupOpen = true;
        //        }
        //        else
        //        {
        //            PopupColor = Brushes.Pink;
        //            PopupMessage = "An error occurred while saving component lines.";
        //            PopupOpen = true;
        //        }
        //    }
        //    catch (Authentication_Exception ae)
        //    {
        //        logger.Warn(Exception_Helper.FormatExceptionString(ae));
        //        aggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(Exception_Helper.FormatExceptionString(ex));
        //        PopupColor = Brushes.Pink;
        //        PopupMessage = String.Format("The following error occurred while saving component lines:\n\n{0}", ex.Message);
        //        PopupOpen = true;
        //        throw;
        //    }
        //    finally
        //    {
        //        logger.Trace("Leaving");
        //    }
        //}

        ///////////////////////////////////////////////
        //#endregion


        //#region AddPartCommand
        ///////////////////////////////////////////////

        ///// <summary>Delegate Command: AddPartCommand</summary>
        //public DelegateCommand AddPartCommand { get; set; }
        //private Boolean CanCommandAddPart()
        //{
        //    if (InfrastructureModule.WorkOrder_OpenMode == Open_Mode.Read_Write) return true;
        //    return false;
        //}

        //private void CommandAddPart()
        //{
        //}

        ///////////////////////////////////////////////
        //#endregion


        #region DeletePartCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: DeletePartCommand</summary>
        public DelegateCommand DeletePartCommand { get; set; }
        private Boolean CanCommandDeletePart()
        {
            if (InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write) return false;
            if (Selected_Component == null) return false;
            return true;
        }

        private void CommandDeletePart()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SART_WO_Components selected = Selected_Component;
                if (selected == null) return;
                if (Update_Part_Lines() == true)
                {
                    if (selected.ID > 0)
                    {
                        SART_WOComp_Web_Service_Client_REST.Delete_SART_WO_Components_Key(InfrastructureModule.Token, selected.ID);
                    }
                    PT_Components_List.Remove(selected);
                    Update_Sequence();
                    Refresh_Components();
                }
                ReadyToQuoteCommand.RaiseCanExecuteChanged();
                ((Repair_Control)View).tbNewPN.Focus();
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


        #region ReadyToQuoteCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ReadyToQuoteCommand</summary>
        public DelegateCommand ReadyToQuoteCommand { get; set; }
        private Boolean CanCommandReadyToQuote()
        {
            if (InfrastructureModule.WorkOrder_OpenMode != Open_Mode.Read_Write) return false;
            if (String.IsNullOrEmpty(Summary_Repair_Comments) == true) return false;
            if (IsChecked_NoPartsRequired == true || (PT_Components_List != null && PT_Components_List.Count != 0)) return true;
            return false;
        }

        private void CommandReadyToQuote()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                String sts = "RdytoQte";
                String woid = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                if (Syteline_FSSRO_Web_Service_Client_REST.Change_WorkingStatus(InfrastructureModule.Token, woid, sts, LoginContext.UserName) == false)
                {
                    PopupColor = Brushes.Pink;
                    PopupMessage = String.Format("An error occurred while trying to update the status of Work Order (Service Request): {0} to \"Ready To Quote\"", woid);
                }
                else
                {
                    aggregator.GetEvent<WorkOrder_Status_Change_Event>().Publish(sts);
                    PopupColor = Brushes.LightGreen;
                    PopupMessage = String.Format("The status of Work Order (Service Request): {0} has been changed to: \"Ready To Quote\"", woid);
                }
                PopupOpen = true;
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
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
                //return null;
                //return false;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region UnSelectCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: UnSelectCommand</summary>
        public DelegateCommand UnSelectCommand { get; set; }
        private Boolean CanCommandUnSelect() { return Selected_Component != null; }
        private void CommandUnSelect()
        {
            Selected_Component = null;
            ((Repair_Control)View).tbNewPN.Focus();
        }

        /////////////////////////////////////////////
        #endregion


        #region RefreshCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: RefreshCommand</summary>
        public DelegateCommand RefreshCommand { get; set; }
        private Boolean CanCommandRefresh()
        {
            return true;
        }

        private void CommandRefresh()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Refresh_Components();
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
        #region Application_Login_Handler  -- Event: Application_Login_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Login_Handler(String name)
        {
            _LoginContext = null;
            _Token = null;

            logger.Info("Loading Approval codes");
            Load_Approval_Codes();
            logger.Info("Loading Change codes");
            Load_Change_Codes();
            logger.Info("Loading Billing codes");
            Load_Billing_Codes();
            logger.Info("Loading Install codes");
            Load_Install_Codes();
            logger.Info("Loading Location codes");
            Load_Location_Codes();
            logger.Info("Loading Unit of Measure codes");
            Load_UM_Codes();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////
        #region Open_WorkOrder_Handler  -- WorkOrder_Instance_Event Event Handler
        /////////////////////////////////////////////

        private void Open_WorkOrder_Handler(Boolean obj)
        {
            Refresh_Components();
        }

        /////////////////////////////////////////////
        #endregion
        /////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Close_Handler  -- Event: SART_WorkOrder_Close_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Close_Handler(Boolean close)
        {
            WorkOrder_Save_Handler(true);
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
        #region WorkOrder_Configuration_Final_UpdateDB_Handler  -- Event: WorkOrder_Configuration_Final_UpdateDB_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Configuration_Final_UpdateDB_Handler(SART_PT_Configuration config)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                foreach (var comp in PT_Components_List)
                {
                    if (String.IsNullOrEmpty(comp.Part_Number) == true)
                    {
                        throw new Exception("Corrupt component data - no part number");
                    }

                    String partnumber = comp.Part_Number;

                    var xref = Find(comp.Part_Number);
                    if (xref != null)
                    {
                        partnumber = xref.Assembly_Part_Number;
                    }
                    logger.Debug("Part Number: {0}", partnumber);

                    if (String.IsNullOrEmpty(partnumber) == false)
                    {
                        partnumber = partnumber.Replace("-", "");
                        if (partnumber.Length >= 5) partnumber = partnumber.Insert(5, "-");

                        logger.Debug("Adjusted Part Number: {0}", partnumber);
                        String serial = InfrastructureModule.Current_Work_Order.PT_Serial;
                        var asmList = Manufacturing_MCA_Web_Service_Client_REST.Select_Manufacturing_Component_Assemblies_PARENT_TYPE_LOCATION(Token, serial, partnumber, "Part_Number");
                        if (asmList != null)
                        {
                            //foreach (var asm in asmList)
                            //{

                            //}
                        }
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Save_Handler  -- Event: WorkOrder_Save_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Save_Handler(Boolean save)
        {
            if (PT_Components_List != null)
            {
                foreach (var item in PT_Components_List)
                {
                    SART_WOComp_Web_Service_Client_REST.Update_SART_WO_Components_Key(InfrastructureModule.Token, item);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        /// <summary></summary>
        /// <param name="navigationContext"></param>
        /// <returns></returns>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <summary></summary>
        /// <param name="navigationContext"></param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //aggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            aggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Repairs", "Repair_Control"));
        }

        /// <summary></summary>
        /// <param name="navigationContext"></param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            aggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            aggregator.GetEvent<ToolBar_Selection_Event>().Publish("Repairs");
            OnPropertyChanged("Summary_Technician_Observations");
            OnPropertyChanged("Summary_Repair_Comments");

            OnPropertyChanged("Work_Order_Num");
            OnPropertyChanged("PTSerial");
            OnPropertyChanged("WorkOrderColor");
            OnPropertyChanged("RepairHours");
            OnPropertyChanged("IsNotReadOnly");
            OnPropertyChanged("IsReadOnly");
            OnPropertyChanged("QuoteVisibility");

            ReadyToQuoteCommand.RaiseCanExecuteChanged();
            UnSelectCommand.RaiseCanExecuteChanged();
            //AddPartCommand.RaiseCanExecuteChanged();
            DeletePartCommand.RaiseCanExecuteChanged();
            ObservationSaveCommand.RaiseCanExecuteChanged();
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        /// <summary>Public Method - Add_New_Component</summary>
        /// <param name="pn">String</param>
        public void Add_New_Component(String pn)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SART_WO_Components comp = new SART_WO_Components();
                comp.Work_Order_ID = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                comp.Change_Type = "Required";
                comp.Quantity = "1";
                comp.Unit_Of_Measure = "EA";
                comp.Sequence = PT_Components_List.Count + 1;
                comp.RowPointer = GUID_Helper.NewGuid();
                comp.Part_Number = pn;
                comp.Part_Name = Syteline_Items_Web_Service_Client_REST.Get_Items_Description(InfrastructureModule.Token, pn);
                if (String.IsNullOrEmpty(comp.Part_Name) == true)
                {
                    String m = $"Part Number: {pn} is not valid";
                    Message_Window.Warning(m, height: Window_Sizes.Small).ShowDialog();
                    New_Part_Number = null;
                    return;
                }
                SART_WO_Components newcomp = SART_WOComp_Web_Service_Client_REST.Insert_SART_WO_Components_Key(InfrastructureModule.Token, comp);
                if (newcomp == null)
                {
                    String m = $"An error occurred trying to add Part Number: {pn} to SRO: {comp.Work_Order_ID}.";
                    Message_Window.Error(m, height: Window_Sizes.Small).ShowDialog();
                    return;
                }
                else
                {
                    Last_10.Update_Most_Recent(Repair_Last_10_FileName, pn);
                    PT_Components_List.Add(newcomp);
                    Selected_Component = newcomp;
                    New_Part_Number = null;
                    ((Repair_Control)View).tbNewPN.Text = String.Empty;
                    ((Repair_Control)View).tbNewPN.Focus();

                }
                ReadyToQuoteCommand.RaiseCanExecuteChanged();
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

        private void Refresh_Components()
        {
            var clist = SART_WOComp_Web_Service_Client_REST.Select_SART_WO_Components_WORK_ORDER_ID(InfrastructureModule.Token, InfrastructureModule.Current_Work_Order.Work_Order_ID);
            if ((clist != null) && (clist.Count > 0))
            {
                clist = Sort_and_Sequence(clist);
                PT_Components_List = new ObservableCollection<SART_WO_Components>(clist);
            }
            else if (InfrastructureModule.Current_Work_Order.Work_Order_ID == Strings.Digits(InfrastructureModule.Current_Work_Order.Work_Order_ID))
            {
                String woID = String.Format("LS{0}", InfrastructureModule.Current_Work_Order.Work_Order_ID.PadLeft(8, '0'));
                clist = SART_WOComp_Web_Service_Client_REST.Select_SART_WO_Components_WORK_ORDER_ID(InfrastructureModule.Token, woID);
                if ((clist != null) && (clist.Count > 0))
                {
                    clist = Sort_and_Sequence(clist);
                    PT_Components_List = new ObservableCollection<SART_WO_Components>(clist);
                }
                else
                {
                    PT_Components_List = new ObservableCollection<SART_WO_Components>();
                }
            }
            else
            {
                PT_Components_List = new ObservableCollection<SART_WO_Components>();
            }
        }

        private void Update_Component_List()
        {
            if (UpdatingComponentLines == false)
            {
                UpdatingComponentLines = true;
                try
                {
                    SART_WO_Components save = Selected_Component;
                    if (PT_Components_List != null)
                    {
                        PT_Components_List = new ObservableCollection<SART_WO_Components>(PT_Components_List);
                    }
                    else
                    {
                        PT_Components_List = new ObservableCollection<SART_WO_Components>();
                    }
                    Selected_Component = save;
                }
                finally
                {
                    UpdatingComponentLines = false;
                }
            }
        }

        private void Update_Sequence()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                int row = 1;
                foreach (SART_WO_Components comp in PT_Components_List)
                {
                    if (comp.Sequence != row)
                    {
                        comp.Sequence = row;
                        if (comp.RowPointer == Guid.Empty)
                        {
                            SART_WO_Components newObj = SART_WOComp_Web_Service_Client_REST.Insert_SART_WO_Components_Key(InfrastructureModule.Token, comp);
                            if (newObj != null) comp.RowPointer = newObj.RowPointer;
                        }
                        else
                        {
                            SART_WOComp_Web_Service_Client_REST.Update_SART_WO_Components_Key(InfrastructureModule.Token, comp);
                        }
                    }
                    row++;
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


        private Boolean Update_Part_Lines()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                foreach (SART_WO_Components comp in PT_Components_List)
                {
                    if (comp.RowPointer == Guid.Empty)
                    {
                        SART_WO_Components newObj = SART_WOComp_Web_Service_Client_REST.Insert_SART_WO_Components_Key(InfrastructureModule.Token, comp);
                        if (newObj != null) comp.RowPointer = newObj.RowPointer;
                        else
                        {
                            throw new Exception(String.Format("Insert of part number: {0} ({1}) failed", comp.Sequence, comp.Part_Number, comp.Part_Name));
                        }
                    }
                    else if (SART_WOComp_Web_Service_Client_REST.Update_SART_WO_Components_Key(InfrastructureModule.Token, comp) == false)
                    {
                        throw new Exception(String.Format("Update of part number: {0} ({1}) failed", comp.Sequence, comp.Part_Number, comp.Part_Name));
                    }
                }
                return true;
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



        private List<SART_WO_Components> Sort_and_Sequence(List<SART_WO_Components> clist)
        {
            if ((clist == null) || (clist.Count == 0)) return clist;

            List<SART_WO_Components> newlist = new List<SART_WO_Components>();
            List<SART_WO_Components> zerolist = new List<SART_WO_Components>();
            clist.Sort(new SART_WO_Components_Sequence_Comparer());
            foreach (SART_WO_Components comp in clist)
            {
                if (comp.Sequence == 0) zerolist.Add(comp);
                else
                {
                    newlist.Add(comp);
                    comp.Sequence = newlist.Count;
                }
            }
            foreach (SART_WO_Components comp in zerolist)
            {
                comp.Sequence = newlist.Count + 1;
                newlist.Add(comp);
            }
            return newlist;
        }


        private SerialValidation Validate_Serial(String serial, String partNumber, String ptSerial)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                String pn = partNumber.ToUpper().Replace("-", "").Insert(5, "-");
                logger.Debug("testing part number: {0}", pn);
                switch (pn)
                {
                    case "23371-00001":
                        if (Serial_Validation.Is_SE_PowerBoard_Defective_Serial(serial) == true) break;
                        if (Serial_Validation.Is_SE_PowerBoard_Serial(serial) == true) break;
                        return SerialValidation.Serial_Format_Invalid;
                    case "23451-00001":
                        if (Serial_Validation.Is_SE_RadioBoard_Serial(serial) == true) break;
                        return SerialValidation.Serial_Format_Invalid;
                    case "23361-00001":
                        if (Serial_Validation.Is_Pivot_Serial(serial) == true) break;
                        return SerialValidation.Serial_Format_Invalid;
                    case "23405-00001":
                        if (Serial_Validation.Is_SE_ACFilterBoard_Serial(serial) == true) break;
                        return SerialValidation.Serial_Format_Invalid;
                    case "22252-00001":// Motor
                        if (Serial_Validation.Is_G2_Motor_Serial(serial) == true) break;
                        return SerialValidation.Serial_Format_Invalid;
                    case "20967-00001":// G2 Battery
                        return SerialValidation.Validated;
                }

                if (String.IsNullOrEmpty(ptSerial) == false)
                {
                    Production_Line_Assembly pt = null;
                    Production_Line_Assembly comp = null;
                    switch (pn)
                    {
                        case "23371-00001":
                        case "23451-00001":
                        case "23361-00001":
                        case "23405-00001":
                            pt = Manufacturing_PLA_Web_Service_Client_REST.Get_SubAssembly(InfrastructureModule.Token, ptSerial);
                            comp = Manufacturing_PLA_Web_Service_Client_REST.Get_SubAssembly(InfrastructureModule.Token, serial);
                            if ((pt != null) && (comp != null))
                            {
                                if (comp.Master_ID != pt.ID) return SerialValidation.Serial_Not_Associated;
                            }
                            else
                            {
                                return SerialValidation.Serial_Not_Associated;
                            }
                            break;

                        case "22252-00001":// Motor
                            pt = Manufacturing_PLA_Web_Service_Client_REST.Get_SubAssembly(InfrastructureModule.Token, ptSerial);
                            comp = Manufacturing_PLA_Web_Service_Client_REST.Get_SubAssembly(InfrastructureModule.Token, serial);
                            if ((pt != null) && (comp != null))
                            {
                                if (comp.Master_ID != pt.ID) return SerialValidation.Serial_Not_Associated;
                            }
                            else if ((pt != null) && (comp == null))
                            {
                                comp = new Production_Line_Assembly()
                                {
                                    Serial_Number = serial,
                                    Part_Type = Serial_Validation.Is_G2_Motor_Serial(serial) ? "Motor" : "",
                                    Part_Number = pn,
                                    Start_Date = DateTime.Now,
                                    Created_By = InfrastructureModule.Token.LoginContext.UserName,
                                };
                                return SerialValidation.Serial_Not_Associated;
                            }
                            break;
                    }
                }
                return SerialValidation.Validated;
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


#if Do_Not_Do_This
        private void Test_and_Update_Assembly_Table(SART_WO_Components comp)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (String.IsNullOrEmpty(comp.Part_Number) == true)
                {
                    logger.Debug("Part Number is empty.");
                    return;
                }
                if (String.IsNullOrEmpty(comp.Installed) == true)
                {
                    logger.Debug("The Installed indicator is empty.");
                    return;
                }
                if (String.IsNullOrEmpty(comp.Serial_Number_Old) == true)
                {
                    logger.Debug("The serial number of the part to be replaced is empty.");
                    return;
                }
                if (String.IsNullOrEmpty(comp.Serial_Number_New) == true)
                {
                    logger.Debug("The serial number of the replacement part is empty.");
                    return;
                }

                String ErrorMessage = null;
                if (SART_2012_Web_Service_Client.Update_Assembly_Table(InfrastructureModule.Token, comp, PTSerial, out ErrorMessage) == false)
                {
                    PopupColor = Brushes.Pink;
                    PopupMessage = ErrorMessage;
                    PopupOpen = true;
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
#endif

        private void Reset()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Selected_Component = null;
                PT_Components_List = null;
                Current_Repair_Comments = null;
                Current_Technician_Observations = null;
                IsChecked_NoPartsRequired = false;
                OnPropertyChanged("RepairHours");
                OnPropertyChanged("Summary_Technician_Observations");
                OnPropertyChanged("Summary_Repair_Comments");
                OnPropertyChanged("RepairedPartCount");
                OnPropertyChanged("QuoteVisibility");
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


        private void Load_Approval_Codes()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                FileInfo fi = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "Approval Codes.xml"));
                if (fi.Directory.Exists == false) fi.Directory.Create();

                var apprCodes = SART_RepApprv_Web_Service_Client_REST.Select_SART_Repair_Approval_Codes_Criteria(InfrastructureModule.Token, null);
                if ((apprCodes != null) && (apprCodes.Count > 0))
                {
                    logger.Debug("Number of codes retrieved: {0}", apprCodes.Count);
                    List<Approval_Types> atypes = new List<Approval_Types>();
                    _ApprovalTypeList = new ObservableCollection<Approval_Types>();
                    foreach (var appr in apprCodes)
                    {
                        Approval_Types at = new Approval_Types(appr.Code, appr.Description);
                        _ApprovalTypeList.Add(at);
                        atypes.Add(at);
                    }
                    Serialization.SerializeToFile<List<Approval_Types>>(atypes, fi.FullName);
                }
                else if (fi.Exists == true)
                {
                    List<Approval_Types> atypes = Serialization.DeserializeFromFile<List<Approval_Types>>(fi.FullName);
                    _ApprovalTypeList = new ObservableCollection<Approval_Types>(atypes);
                }
                else
                {
                    logger.Warn("Unable to load Approval Codes");
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


        private void Load_Change_Codes()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                FileInfo fi = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "Change Codes.xml"));
                if (fi.Directory.Exists == false) fi.Directory.Create();

                var chngCodes = SART_RepChng_Web_Service_Client_REST.Select_SART_Repair_Change_Codes_Criteria(InfrastructureModule.Token, null);
                if ((chngCodes != null) && (chngCodes.Count > 0))
                {
                    logger.Debug("Number of codes retrieved: {0}", chngCodes.Count);
                    List<Change_Types> ctypes = new List<Change_Types>();
                    _ChangeTypeList = new ObservableCollection<Change_Types>();
                    foreach (var chng in chngCodes)
                    {
                        Change_Types ct = new Change_Types(chng.Code, chng.Description);
                        _ChangeTypeList.Add(ct);
                        ctypes.Add(ct);
                    }
                    Serialization.SerializeToFile<List<Change_Types>>(ctypes, fi.FullName);
                }
                else if (fi.Exists == true)
                {
                    List<Change_Types> atypes = Serialization.DeserializeFromFile<List<Change_Types>>(fi.FullName);
                    _ChangeTypeList = new ObservableCollection<Change_Types>(atypes);
                }
                else
                {
                    logger.Warn("Unable to load Change Codes");
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

        private void Load_Install_Codes()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                FileInfo fi = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "Install Codes.xml"));
                if (fi.Directory.Exists == false) fi.Directory.Create();

                SqlBooleanCriteria criteria = new SqlBooleanCriteria(new FieldData("ID", null, FieldCompareOperator.NotEqual));
                var instCodes = SART_RepInst_Web_Service_Client_REST.Select_SART_Repair_Installed_Codes_Criteria(InfrastructureModule.Token, criteria);
                if ((instCodes != null) && (instCodes.Count > 0))
                {
                    logger.Debug("Number of codes retrieved: {0}", instCodes.Count);
                    List<Install_Types> ctypes = new List<Install_Types>();
                    _InstallTypeList = new ObservableCollection<Install_Types>();
                    foreach (var inst in instCodes)
                    {
                        Install_Types it = new Install_Types(inst.Code, inst.Description);
                        _InstallTypeList.Add(it);
                        ctypes.Add(it);
                    }
                    Serialization.SerializeToFile<List<Install_Types>>(ctypes, fi.FullName);
                }
                else if (fi.Exists == true)
                {
                    List<Install_Types> atypes = Serialization.DeserializeFromFile<List<Install_Types>>(fi.FullName);
                    _InstallTypeList = new ObservableCollection<Install_Types>(atypes);
                }
                else
                {
                    logger.Warn("Unable to load Install Codes");
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


        private void Load_Billing_Codes()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                FileInfo fi = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "Billing Codes.xml"));
                if (fi.Directory.Exists == false) fi.Directory.Create();

                var billCodes = SART_RepBill_Web_Service_Client_REST.Select_SART_Repair_Billing_Codes_Criteria(InfrastructureModule.Token, null);
                if ((billCodes != null) && (billCodes.Count > 0))
                {
                    logger.Debug("Number of codes retrieved: {0}", billCodes.Count);
                    List<Billing_Types> ctypes = new List<Billing_Types>();
                    _BillableCodeList = new ObservableCollection<Billing_Types>();
                    foreach (var inst in billCodes)
                    {
                        Billing_Types it = new Billing_Types(inst.Code, inst.Description);
                        _BillableCodeList.Add(it);
                        ctypes.Add(it);
                    }
                    Serialization.SerializeToFile<List<Billing_Types>>(ctypes, fi.FullName);
                }
                else if (fi.Exists == true)
                {
                    List<Billing_Types> atypes = Serialization.DeserializeFromFile<List<Billing_Types>>(fi.FullName);
                    _BillableCodeList = new ObservableCollection<Billing_Types>(atypes);
                }
                else
                {
                    logger.Warn("Unable to load Billing Codes");
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


        private void Load_Location_Codes()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                FileInfo fi = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "Location Codes.xml"));
                if (fi.Directory.Exists == false) fi.Directory.Create();

                var locCodes = SART_RepLoc_Web_Service_Client_REST.Select_SART_Repair_Location_Codes_Criteria(InfrastructureModule.Token, null);
                if ((locCodes != null) && (locCodes.Count > 0))
                {
                    logger.Debug("Number of codes retrieved: {0}", locCodes.Count);
                    List<Location_Types> ctypes = new List<Location_Types>();
                    _LocationCodeList = new ObservableCollection<Location_Types>();
                    foreach (var inst in locCodes)
                    {
                        Location_Types it = new Location_Types(inst.Code, inst.Description);
                        _LocationCodeList.Add(it);
                        ctypes.Add(it);
                    }
                    Serialization.SerializeToFile<List<Location_Types>>(ctypes, fi.FullName);
                }
                else if (fi.Exists == true)
                {
                    List<Location_Types> atypes = Serialization.DeserializeFromFile<List<Location_Types>>(fi.FullName);
                    _LocationCodeList = new ObservableCollection<Location_Types>(atypes);
                }
                else
                {
                    logger.Warn("Unable to load Location Codes");
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


        private void Load_UM_Codes()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                FileInfo fi = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "UoM Codes.xml"));
                if (fi.Directory.Exists == false) fi.Directory.Create();

                var umCodes = Syteline_UOM_Web_Service_Client_REST.Select_UOM_All(InfrastructureModule.Token);
                if ((umCodes != null) && (umCodes.Count > 0))
                {
                    logger.Debug("Number of codes retrieved: {0}", umCodes.Count);
                    umCodes = umCodes.OrderBy(x => x.U_M).ThenBy(x => x.Record_Date).ToList();
                    List<Units_of_Measure_Types> ctypes = new List<Units_of_Measure_Types>();
                    _UMList = new ObservableCollection<Units_of_Measure_Types>();
                    foreach (var inst in umCodes)
                    {
                        Units_of_Measure_Types it = new Units_of_Measure_Types(inst.U_M, inst.Description);
                        _UMList.Add(it);
                        ctypes.Add(it);
                    }
                    Serialization.SerializeToFile<List<Units_of_Measure_Types>>(ctypes, fi.FullName);
                }
                else if (fi.Exists == true)
                {
                    List<Units_of_Measure_Types> atypes = Serialization.DeserializeFromFile<List<Units_of_Measure_Types>>(fi.FullName);
                    _UMList = new ObservableCollection<Units_of_Measure_Types>(atypes);
                }
                else
                {
                    logger.Warn("Unable to load Unit of Measure Codes");
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

        private Segway_Part_Type_Xref Find(String pn)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                foreach (var xref in Part_Number_Cross_Ref)
                {
                    if (xref.Service_Part_Number == pn) return xref;
                }
                return null;
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

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
