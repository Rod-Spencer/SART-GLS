using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Control.Lookup;
using Segway.Login.Objects;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.Modules.AddWindow;
using Segway.Service.Objects;
using Segway.Syteline.Client.REST;
using Segway.Syteline.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;



namespace Segway.Modules.Administration
{
    /// <summary>Public Class - Create_SRO_ViewModel</summary>
    public class Create_SRO_ViewModel : ViewModelBase, Create_SRO_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;

        /// <summary>Public Member - void</summary>
        public delegate void BackGroundProcess();


        /// <summary>Contructor</summary>
        /// <param name="view">Create_SRO_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Create_SRO_ViewModel(Create_SRO_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<SART_Dealers_Loaded_Event>().Subscribe(SART_Dealers_Loaded_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates

            SubmitSROCommand = new DelegateCommand(CommandSubmitSRO, CanCommandSubmitSRO);
            ClearSROCommand = new DelegateCommand(CommandClearSRO, CanCommandClearSRO);
            ReasonAddCommand = new DelegateCommand(CommandReasonAdd, CanCommandReasonAdd);
            ReasonDelCommand = new DelegateCommand(CommandReasonDel, CanCommandReasonDel);
            NoteSubmitCommand = new DelegateCommand(CommandNoteSubmit, CanCommandNoteSubmit);
            SetDefaultsCommand = new DelegateCommand(CommandSetDefaults, CanCommandSetDefaults);
            UseDefaultsCommand = new DelegateCommand(CommandUseDefaults, CanCommandUseDefaults);
            ClipBoardCommand = new DelegateCommand(CommandClipBoard, CanCommandClipBoard);
            DealerLookupCommand = new DelegateCommand(CommandDealerLookup, CanCommandDealerLookup);
            LookupDealerCommand = new DelegateCommand(CommandLookupDealer, CanCommandLookupDealer);

            #endregion

            Last10SROsFilename = String.Format("Last 10 {0} SROs.xml", view.ViewName);
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        #region Items

        private Dictionary<String, String> _Items;

        /// <summary>Property Items of type Dictionary&lt;String, String&gt;</summary>
        public Dictionary<String, String> Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new Dictionary<String, String>();
                }
                return _Items;
            }
        }

        #endregion

        #region Dealers

        private Dictionary<String, SLCustomers> _Dealers;

        /// <summary>Property Dealers of type Dictionary&lt;String, SLCustomers&gt;</summary>
        public Dictionary<String, SLCustomers> Dealers
        {
            get
            {
                if (_Dealers == null) _Dealers = new Dictionary<String, SLCustomers>();
                return _Dealers;
            }
        }

        #endregion

        #region Priorities

        List<FS_Prior_Code> Priorities { get; set; }

        #endregion

        #region Statuses

        List<FS_Stat_Code> Statuses { get; set; }

        #endregion

        #region Sites

        List<Sites> Sites { get; set; }

        #endregion

        #region SRO_Types

        List<FS_SRO_Type> SRO_Types { get; set; }

        #endregion

        #region Destinations

        List<String> Destinations { get; set; }

        #endregion

        #region Transactions

        private List<String> _Transactions;

        /// <summary>Property Transactions of type List&lt;String&gt;</summary>
        public List<String> Transactions
        {
            get
            {
                if ((_Transactions == null) || (_Transactions.Count == 0))
                {
                    String path = Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "Transactions.xml");
                    FileInfo fi = new FileInfo(path);
                    if (fi.Directory.Exists == false) fi.Directory.Create();
                    if (fi.Exists == true)
                    {
                        _Transactions = Serialization.DeserializeFromFile<List<String>>(fi);
                    }
                    if ((_Transactions == null) || (_Transactions.Count == 0))
                    {
                        _Transactions = new List<String>();
                        _Transactions.Add("Inventory Issue");
                        _Transactions.Add("Inventory Return");
                        _Transactions.Add("Exchange Shipment");
                        _Transactions.Add("Exchange Return");
                        _Transactions.Add("Customer Shipment");
                        _Transactions.Add("Customer Return");
                        _Transactions.Add("Loaner Shipment");
                        _Transactions.Add("Loaner Return");
                        Serialization.SerializeToFile<List<String>>(_Transactions, fi);
                    }
                }
                return _Transactions;
            }
            set
            {
                _Transactions = value;
                OnPropertyChanged("Transactions");
            }
        }

        #endregion

        #region Prices

        List<PriceCode> Prices { get; set; }

        #endregion

        #region EndUsers


        /// <summary>Public Property - EndUsers</summary>
        public List<Seg_End_User_Channel> EndUsers { get; set; }

        #endregion

        #region Regions

        private Dictionary<String, String> _Regions;

        /// <summary>Property Regions of type Dictionary&lt;String,String&gt;</summary>
        public Dictionary<String, String> Regions
        {
            get
            {
                if (_Regions == null) _Regions = new Dictionary<String, String>();
                return _Regions;
            }
        }

        #endregion

        #region Region

        /// <summary>Property Region of type Regional_Settings</summary>
        public Regional_Settings Region
        {
            get { return InfrastructureModule.RegionSettings; }
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

        #region sro

        private FS_SRO _sro;

        /// <summary>Property sro of type FSSROs</summary>
        public FS_SRO sro
        {
            get { return _sro; }
            set
            {
                _sro = value;
                ReasonAddCommand.RaiseCanExecuteChanged();
                NoteSubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region inc

        private FS_Incident _inc;

        /// <summary>Property inc of type FSIncidents</summary>
        public FS_Incident inc
        {
            get { return _inc; }
            set { _inc = value; }
        }

        #endregion


        #region Specific_Reasons


        List<FS_Reas_Spec> Specific_Reasons { get; set; }

        #endregion

        #region OperationCodes

        private static Dictionary<String, String> _OperationCodes;

        /// <summary>Property OperationCodes of type Dictionary&lt;String,String&gt;</summary>
        public static Dictionary<String, String> OperationCodes
        {
            get
            {
                if (_OperationCodes == null)
                {
                    _OperationCodes = new Dictionary<String, String>();
                }
                return _OperationCodes;
            }
            set { _OperationCodes = value; }
        }

        #endregion

        FS_SRO_Line sroline = null;

        FS_Partner partner = null;

        FS_Unit fsunit = null;

        List<FS_Reas_Gen> Reasons { get; set; }

        private static readonly String SRODefaultFileName = "SRO Defaults.xml";

        private String Last10SROsFilename = null;

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        #region SerialNumber

        private String _SerialNumber;

        /// <summary>Property SerialNumber of type String</summary>
        public String SerialNumber
        {
            get { return _SerialNumber; }
            set
            {
                _SerialNumber = value;
                try
                {
                    if (String.IsNullOrEmpty(_SerialNumber) == true)
                    {
                        PartNumber = null;
                        Note_List = new ObservableCollection<Note_Data>();
                        SRO_Number = null;
                    }
                    else
                    {
                        Last_10.Update_Most_Recent(Last_10.Last_PT_FileName, _SerialNumber);
                        Task back = new Task(Serial_Back);
                        back.ContinueWith(Serial_ExceptionHander, TaskContinuationOptions.OnlyOnFaulted);
                        back.Start();
                        logger.Debug("Started background process: Serial_Back");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString(ex));
                    //throw;
                }
                finally
                {
                    SubmitSROCommand.RaiseCanExecuteChanged();
                    NoteSubmitCommand.RaiseCanExecuteChanged();
                    ReasonAddCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged("SerialNumber");
                }
            }
        }

        private void Clear_Data(Boolean full = false)
        {
            if (full == true)
            {
                _SerialNumber = null;
                _PartNumber = null;
                _Part_Description = null;
                _Dealer_Num = null;
                _Dealer_Seq = 0;
                _Dealer_Description = null;

                OnPropertyChanged("SerialNumber");
                OnPropertyChanged("PartNumber");
                OnPropertyChanged("Part_Description");
                OnPropertyChanged("Dealer_Num");
                OnPropertyChanged("Dealer_Seq");
                OnPropertyChanged("Dealer_Description");
            }
            SRO_Number = null;
            _Consumer_Num = null;
            _Consumer_Seq = 0;
            _Consumer_Description = null;
            _Contact_Name = null;
            _Contact_Phone = null;

            OnPropertyChanged("Consumer_Num");
            OnPropertyChanged("Consumer_Seq");
            OnPropertyChanged("Consumer_Description");
            OnPropertyChanged("Contact_Name");
            OnPropertyChanged("Contact_Phone");




            Selected_SRO_Type = null;
            Selected_Priority = null;
            Selected_Status = null;
            Selected_Site = null;
            Selected_Dest = null;
            Selected_EndUser = null;
            Selected_Price = null;
            Selected_Region = null;
            Incident_Description = null;
            if (partner != null)
            {
                Partner_ID = partner.Partner_ID;
                Partner_Description = partner.Name;
            }
            Note_List = new ObservableCollection<Note_Data>();
            Reason_List = new ObservableCollection<Incident_Reason_Data>();
        }

        #endregion

        #region PartNumber

        private String _PartNumber;

        /// <summary>Property PartNumber of type String</summary>
        public String PartNumber
        {
            get { return _PartNumber; }
            set
            {
                try
                {
                    _PartNumber = value;
                    if (String.IsNullOrEmpty(_PartNumber) == true)
                    {
                        Part_Description = null;
                    }
                    else if (Items.ContainsKey(_PartNumber) == false)
                    {
                        String desc = Syteline_Items_Web_Service_Client_REST.Select_Items_Description(Token, _PartNumber);
                        if (String.IsNullOrEmpty(desc) == false)
                        {
                            Items[_PartNumber] = desc;
                            Part_Description = desc;
                        }
                    }
                    else
                    {
                        Part_Description = Items[_PartNumber];
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString(ex));
                    //throw;
                }
                finally
                {
                    SubmitSROCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged("PartNumber");
                }
            }
        }

        #endregion

        #region Part_Description

        private String _Part_Description;

        /// <summary>Property Part_Description of type String</summary>
        public String Part_Description
        {
            get { return _Part_Description; }
            set
            {
                _Part_Description = value;
                OnPropertyChanged("Part_Description");
            }
        }

        #endregion

        #region Incident_Description

        private String _Incident_Description;

        /// <summary>Property Incident_Description of type String</summary>
        public String Incident_Description
        {
            get { return _Incident_Description; }
            set
            {
                _Incident_Description = value;
                OnPropertyChanged("Incident_Description");
            }
        }

        #endregion

        #region Partner_ID

        private String _Partner_ID;

        /// <summary>Property Partner_ID of type String</summary>
        public String Partner_ID
        {
            get { return _Partner_ID; }
            set
            {
                _Partner_ID = value;
                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Partner_ID");
            }
        }

        #endregion

        #region Partner_Description

        private String _Partner_Description;

        /// <summary>Property Partner_Description of type String</summary>
        public String Partner_Description
        {
            get { return _Partner_Description; }
            set
            {
                _Partner_Description = value;
                OnPropertyChanged("Partner_Description");
            }
        }

        #endregion

        #region Dealer_Num

        private String _Dealer_Num;

        /// <summary>Property Dealer_Num of type String</summary>
        public String Dealer_Num
        {
            get { return _Dealer_Num; }
            set
            {
                String store = _Dealer_Num;
                _Dealer_Num = value;
                if (String.IsNullOrEmpty(_Dealer_Num) == true)
                {
                    Dealer_Seq = 0;
                    Dealer_Description = null;
                }
                else if (store != _Dealer_Num)
                {
                    SLCustomers cust = Set_Customer();
                    if (cust != null) Dealer_Description = cust.Name;
                    else Dealer_Description = null;
                    Load_ShipTos();
                }

                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Dealer_Num");
            }
        }

        #endregion

        #region Dealer_Seq

        private Int32 _Dealer_Seq;

        /// <summary>Property Dealer_Seq of type Int32</summary>
        public Int32 Dealer_Seq
        {
            get { return _Dealer_Seq; }
            set
            {
                int store = _Dealer_Seq;
                _Dealer_Seq = value;
                if (store != _Dealer_Seq)
                {
                    SLCustomers cust = Set_Customer();
                    if (cust != null) Dealer_Description = cust.Name;
                }

                OnPropertyChanged("Dealer_Seq");
            }
        }

        #endregion

        #region Dealer_Description

        private String _Dealer_Description;

        /// <summary>Property Dealer_Description of type String</summary>
        public String Dealer_Description
        {
            get { return _Dealer_Description; }
            set
            {
                _Dealer_Description = value;
                OnPropertyChanged("Dealer_Description");
            }
        }

        #endregion

        #region Consumer_Num

        private String _Consumer_Num;

        /// <summary>Property Consumer_Num of type String</summary>
        public String Consumer_Num
        {
            get { return _Consumer_Num; }
            set
            {
                _Consumer_Num = value;
                OnPropertyChanged("Consumer_Num");
            }
        }

        #endregion

        #region Consumer_Seq

        private Int32 _Consumer_Seq;

        /// <summary>Property Consumer_Seq of type Int32</summary>
        public Int32 Consumer_Seq
        {
            get { return _Consumer_Seq; }
            set
            {
                _Consumer_Seq = value;
                OnPropertyChanged("Consumer_Seq");
            }
        }

        #endregion

        #region Consumer_Description

        private String _Consumer_Description;

        /// <summary>Property Consumer_Description of type String</summary>
        public String Consumer_Description
        {
            get { return _Consumer_Description; }
            set
            {
                _Consumer_Description = value;
                OnPropertyChanged("Consumer_Description");
            }
        }

        #endregion

        #region Priority_List

        private ObservableCollection<String> _Priority_List;

        /// <summary>Property Priority_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Priority_List
        {
            get { return _Priority_List; }
            set
            {
                _Priority_List = value;
                OnPropertyChanged("Priority_List");
            }
        }

        #endregion

        #region Selected_Priority

        private String _Selected_Priority;

        /// <summary>Property Selected_Priority of type String</summary>
        public String Selected_Priority
        {
            get { return _Selected_Priority; }
            set
            {
                _Selected_Priority = value;
                if (_Selected_Priority == null) Priority_Description = null;
                else
                {
                    FS_Prior_Code p = null;
                    if (Priorities != null) p = Priorities.Where(x => x.Prior_Code == _Selected_Priority).FirstOrDefault();
                    if (p != null) Priority_Description = p.Description;
                    else Priority_Description = null;
                }
                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Selected_Priority");
            }
        }

        #endregion

        #region Priority_Description

        private String _Priority_Description;

        /// <summary>Property Priority_Description of type String</summary>
        public String Priority_Description
        {
            get { return _Priority_Description; }
            set
            {
                _Priority_Description = value;
                OnPropertyChanged("Priority_Description");
            }
        }

        #endregion

        #region Status_List

        private ObservableCollection<String> _Status_List;

        /// <summary>Property Status_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Status_List
        {
            get { return _Status_List; }
            set
            {
                _Status_List = value;
                OnPropertyChanged("Status_List");
            }
        }

        #endregion

        #region Selected_Status

        private String _Selected_Status;

        /// <summary>Property Selected_Status of type String</summary>
        public String Selected_Status
        {
            get { return _Selected_Status; }
            set
            {
                _Selected_Status = value;
                if (_Selected_Status == null) Status_Description = null;
                else
                {
                    FS_Stat_Code stat = null;
                    if (Statuses != null) stat = Statuses.Where(x => x.Stat_Code == _Selected_Status).FirstOrDefault();
                    if (stat == null) Status_Description = null;
                    else Status_Description = stat.Description;
                }

                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Selected_Status");
            }
        }

        #endregion

        #region Status_Description

        private String _Status_Description;

        /// <summary>Property Status_Description of type String</summary>
        public String Status_Description
        {
            get { return _Status_Description; }
            set
            {
                _Status_Description = value;
                OnPropertyChanged("Status_Description");
            }
        }

        #endregion

        #region Site_List

        private ObservableCollection<String> _Site_List;

        /// <summary>Property Site_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Site_List
        {
            get { return _Site_List; }
            set
            {
                _Site_List = value;
                OnPropertyChanged("Site_List");
            }
        }

        #endregion

        #region Selected_Site

        private String _Selected_Site;

        /// <summary>Property Selected_Site of type String</summary>
        public String Selected_Site
        {
            get { return _Selected_Site; }
            set
            {
                _Selected_Site = value;
                if (_Selected_Site == null) Site_Description = null;
                else
                {
                    Sites s = null;
                    if (Sites != null) s = Sites.Where(x => x.Site == Selected_Site).FirstOrDefault();
                    if (s != null) Site_Description = s.Description;
                    else Site_Description = null;
                }
                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Selected_Site");
            }
        }

        #endregion

        #region Site_Description

        private String _Site_Description;

        /// <summary>Property Site_Description of type String</summary>
        public String Site_Description
        {
            get { return _Site_Description; }
            set
            {
                _Site_Description = value;
                OnPropertyChanged("Site_Description");
            }
        }

        #endregion

        #region SRO_Type_List

        private ObservableCollection<String> _SRO_Type_List;

        /// <summary>Property SRO_Type_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> SRO_Type_List
        {
            get { return _SRO_Type_List; }
            set
            {
                _SRO_Type_List = value;
                OnPropertyChanged("SRO_Type_List");
            }
        }

        #endregion

        #region Selected_SRO_Type

        private String _Selected_SRO_Type;

        /// <summary>Property Selected_SRO_Type of type String</summary>
        public String Selected_SRO_Type
        {
            get { return _Selected_SRO_Type; }
            set
            {
                _Selected_SRO_Type = value;
                if (String.IsNullOrEmpty(_Selected_SRO_Type) == true) SRO_Type_Description = null;
                else
                {
                    FS_SRO_Type sro_t = null;
                    if (SRO_Types != null)
                    {
                        sro_t = SRO_Types.Where(x => x.SRO_Type == Selected_SRO_Type).FirstOrDefault();
                    }
                    if (sro_t != null) SRO_Type_Description = sro_t.Description;
                    else SRO_Type_Description = null;
                }
                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Selected_SRO_Type");
            }
        }

        #endregion

        #region SRO_Type_Description

        private String _SRO_Type_Description;

        /// <summary>Property SRO_Type_Description of type String</summary>
        public String SRO_Type_Description
        {
            get { return _SRO_Type_Description; }
            set
            {
                _SRO_Type_Description = value;
                OnPropertyChanged("SRO_Type_Description");
            }
        }

        #endregion

        #region Dest_List

        private ObservableCollection<String> _Dest_List;

        /// <summary>Property Dest_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Dest_List
        {
            get { return _Dest_List; }
            set
            {
                _Dest_List = value;
                OnPropertyChanged("Dest_List");
            }
        }

        #endregion

        #region Selected_Dest

        private String _Selected_Dest;

        /// <summary>Property Selected_Dest of type String</summary>
        public String Selected_Dest
        {
            get { return _Selected_Dest; }
            set
            {
                _Selected_Dest = value;
                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Selected_Dest");
            }
        }

        #endregion

        #region Trans_List

        private ObservableCollection<String> _Trans_List;

        /// <summary>Property Trans_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Trans_List
        {
            get { return _Trans_List; }
            set
            {
                _Trans_List = value;
                OnPropertyChanged("Trans_List");
            }
        }

        #endregion

        #region Selected_Trans

        private String _Selected_Trans;

        /// <summary>Property Selected_Trans of type String</summary>
        public String Selected_Trans
        {
            get { return _Selected_Trans; }
            set
            {
                _Selected_Trans = value;
                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Selected_Trans");
            }
        }

        #endregion

        #region SRO_Number

        private String _SRO_Number;

        /// <summary>Property SRO_Number of type String</summary>
        public String SRO_Number
        {
            get { return _SRO_Number; }
            set
            {
                _SRO_Number = value;
                OnPropertyChanged("SRO_Number");
                ClipBoardCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region IsWarranty

        //private Boolean _IsWarranty;

        /// <summary>Property IsWarranty of type Boolean</summary>
        public Boolean IsWarranty
        {
            get
            {
                if (sro == null) return false;
                if (sro.Seg_Is_Warr.HasValue == false) return false;
                return sro.Seg_Is_Warr.Value;
            }
            set
            {
                if (sro == null) return;
                sro.Seg_Is_Warr = value;
                OnPropertyChanged("IsWarranty");
            }
        }

        #endregion

        #region Price_List

        private ObservableCollection<String> _Price_List;

        /// <summary>Property Price_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Price_List
        {
            get { return _Price_List; }
            set
            {
                _Price_List = value;
                OnPropertyChanged("Price_List");
            }
        }

        #endregion

        #region Selected_Price

        private String _Selected_Price;

        /// <summary>Property Selected_Price of type String</summary>
        public String Selected_Price
        {
            get { return _Selected_Price; }
            set
            {
                _Selected_Price = value;
                if (_Selected_Price == null) Price_Description = null;
                else
                {
                    PriceCode price = null;
                    if (Prices != null) price = Prices.Where(x => x.Pricecode == _Selected_Price).FirstOrDefault();
                    if (price == null) Price_Description = null;
                    else Price_Description = price.Description;
                }
                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Selected_Price");
            }
        }

        #endregion

        #region Price_Description

        private String _Price_Description;

        /// <summary>Property Price_Description of type String</summary>
        public String Price_Description
        {
            get { return _Price_Description; }
            set
            {
                _Price_Description = value;
                OnPropertyChanged("Price_Description");
            }
        }

        #endregion

        #region EndUser_List

        private ObservableCollection<String> _EndUser_List;

        /// <summary>Property EndUser_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> EndUser_List
        {
            get { return _EndUser_List; }
            set
            {
                _EndUser_List = value;
                OnPropertyChanged("EndUser_List");
            }
        }

        #endregion

        #region Selected_EndUser

        private String _Selected_EndUser;

        /// <summary>Property Selected_EndUser of type String</summary>
        public String Selected_EndUser
        {
            get { return _Selected_EndUser; }
            set
            {
                _Selected_EndUser = value;
                if (_Selected_EndUser == null) EndUser_Description = null;
                else
                {
                    Seg_End_User_Channel eu = null;
                    if (EndUsers != null) eu = EndUsers.Where(x => x.End_User_Type == _Selected_EndUser).FirstOrDefault();
                    if (eu == null) EndUser_Description = null;
                    else EndUser_Description = eu.Channel;
                }
                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Selected_EndUser");
            }
        }

        #endregion

        #region EndUser_Description

        private String _EndUser_Description;

        /// <summary>Property EndUser_Description of type String</summary>
        public String EndUser_Description
        {
            get { return _EndUser_Description; }
            set
            {
                _EndUser_Description = value;
                OnPropertyChanged("EndUser_Description");
            }
        }

        #endregion

        #region Region_List

        private ObservableCollection<String> _Region_List;

        /// <summary>Property Region_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Region_List
        {
            get { return _Region_List; }
            set
            {
                _Region_List = value;
                OnPropertyChanged("Region_List");
            }
        }

        #endregion

        #region Selected_Region

        private String _Selected_Region;

        /// <summary>Property Selected_Region of type String</summary>
        public String Selected_Region
        {
            get { return _Selected_Region; }
            set
            {
                _Selected_Region = value;
                if (_Selected_Region == null) Region_Description = null;
                else if (Regions.ContainsKey(_Selected_Region) == true)
                {
                    Region_Description = Regions[_Selected_Region];
                }
                else Region_Description = null;
                SubmitSROCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Selected_Region");
            }
        }

        #endregion

        #region Region_Description

        private String _Region_Description;

        /// <summary>Property Region_Description of type String</summary>
        public String Region_Description
        {
            get { return _Region_Description; }
            set
            {
                _Region_Description = value;
                OnPropertyChanged("Region_Description");
            }
        }

        #endregion

        #region Contact_Name

        private String _Contact_Name;

        /// <summary>Property Contact_Name of type String</summary>
        public String Contact_Name
        {
            get { return _Contact_Name; }
            set
            {
                _Contact_Name = value;
                OnPropertyChanged("Contact_Name");
            }
        }

        #endregion

        #region Contact_Phone

        private String _Contact_Phone;

        /// <summary>Property Contact_Phone of type String</summary>
        public String Contact_Phone
        {
            get { return _Contact_Phone; }
            set
            {
                _Contact_Phone = value;
                OnPropertyChanged("Contact_Phone");
            }
        }

        #endregion

        #region Contact_Email

        private String _Contact_Email;

        /// <summary>Property Contact_Email of type String</summary>
        public String Contact_Email
        {
            get { return _Contact_Email; }
            set
            {
                _Contact_Email = value;
                OnPropertyChanged("Contact_Email");
            }
        }

        #endregion

        #region SRO_Notes

        private ObservableCollection<Note_Data> _Note_List;

        /// <summary>Property SRO_Notes of type ObservableCollection&lt;Note_Data&gt;</summary>
        public ObservableCollection<Note_Data> Note_List
        {
            get { return _Note_List; }
            set
            {
                _Note_List = value;
                OnPropertyChanged("Note_List");
            }
        }

        #endregion

        #region SRO_Note

        private String _SRO_Note;

        /// <summary>Property SRO_Note of type String</summary>
        public String SRO_Note
        {
            get { return _SRO_Note; }
            set
            {
                _SRO_Note = value;
                OnPropertyChanged("SRO_Note");
            }
        }

        #endregion

        #region Reason_List

        private ObservableCollection<Incident_Reason_Data> _Reason_List;

        /// <summary>Property Reason_List of type ObservableCollection&lt;FS_Inc_Reason&gt;</summary>
        public ObservableCollection<Incident_Reason_Data> Reason_List
        {
            get { return _Reason_List; }
            set
            {
                _Reason_List = value;
                OnPropertyChanged("Reason_List");
            }
        }

        #endregion

        #region Selected_Reason

        private Incident_Reason_Data _Selected_Reason;

        /// <summary>Property Selected_Reason of type FS_Inc_Reason</summary>
        public Incident_Reason_Data Selected_Reason
        {
            get { return _Selected_Reason; }
            set
            {
                _Selected_Reason = value;
                if (_Selected_Reason == null)
                {
                    Specific_Code = null;
                    Specific_Description = null;
                }
                else
                {
                    Specific_Code = _Selected_Reason.Specific_Reason_Code;
                    Specific_Description = _Selected_Reason.Specific_Reason_Description;
                }
                OnPropertyChanged("Selected_Reason");
                ReasonDelCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Specific_Code

        private String _Specific_Code;

        /// <summary>Property Specific_Code of type String</summary>
        public String Specific_Code
        {
            get { return _Specific_Code; }
            set
            {
                _Specific_Code = value;
                OnPropertyChanged("Specific_Code");
            }
        }

        #endregion

        #region Specific_Description

        private String _Specific_Description;

        /// <summary>Property Specific_Description of type String</summary>
        public String Specific_Description
        {
            get { return _Specific_Description; }
            set
            {
                _Specific_Description = value;
                OnPropertyChanged("Specific_Description");
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers


        #region SubmitSROCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: SubmitSROCommand</summary>
        public DelegateCommand SubmitSROCommand { get; set; }

        private Boolean CanCommandSubmitSRO()
        {
            if (String.IsNullOrEmpty(SerialNumber) == true) return false;
            if (String.IsNullOrEmpty(PartNumber) == true) return false;
            if (String.IsNullOrEmpty(Partner_ID) == true) return false;
            if (String.IsNullOrEmpty(Dealer_Num) == true) return false;
            if (String.IsNullOrEmpty(Selected_Priority) == true) return false;
            if (String.IsNullOrEmpty(Selected_Status) == true) return false;
            if (String.IsNullOrEmpty(Selected_Site) == true) return false;
            if (String.IsNullOrEmpty(Selected_SRO_Type) == true) return false;
            if (String.IsNullOrEmpty(Selected_Dest) == true) return false;
            //if (String.IsNullOrEmpty(Selected_Trans) == true) return false;
            if (String.IsNullOrEmpty(Selected_Price) == true) return false;
            if (String.IsNullOrEmpty(Selected_EndUser) == true) return false;
            return true;
        }

        private void CommandSubmitSRO()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Task back = new Task(CommandSubmitSRO_Back);
                back.ContinueWith(Task_ExceptionHander, TaskContinuationOptions.OnlyOnFaulted);
                back.Start();
                logger.Debug("Started background process: CommandSubmitSRO_Back");
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region ClearSROCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearSROCommand</summary>
        public DelegateCommand ClearSROCommand { get; set; }
        private Boolean CanCommandClearSRO() { return true; }
        private void CommandClearSRO()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Clear_Data(true);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region SetDefaultsCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: SetDefaultsCommand</summary>
        public DelegateCommand SetDefaultsCommand { get; set; }
        private Boolean CanCommandSetDefaults() { return true; }
        private void CommandSetDefaults()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                String fname = $"{Application_Helper.Application_Data_Folder_Name()}\\SRO Defaults - {Selected_Region}.xml";

                FileInfo fi = new FileInfo(fname);
                if (fi.Directory.Exists == false) fi.Directory.Create();
                else if (fi.Exists == true) fi.Delete();

                SRO_Defaults def = new SRO_Defaults()
                {
                    Destination = Selected_Dest,
                    End_User_Type = Selected_EndUser,
                    Price = Selected_Price,
                    Priority = Selected_Priority,
                    Region = Selected_Region,
                    Site = Selected_Site,
                    SRO_Type = Selected_SRO_Type,
                    Status = Selected_Status
                };
                Serialization.SerializeToFile<SRO_Defaults>(def, fi);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region UseDefaultsCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: UseDefaultsCommand</summary>
        public DelegateCommand UseDefaultsCommand { get; set; }
        private Boolean CanCommandUseDefaults() { return true; }
        private void CommandUseDefaults()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                DirectoryInfo di = new DirectoryInfo(Application_Helper.Application_Data_Folder_Name());
                FileInfo[] files = di.GetFiles("SRO Default*.xml");
                if (files.Length == 0)
                {
                    Message_Window.Warning("No Defaults have been set").ShowDialog();
                    return;
                }
                else if (files.Length == 1)
                {
                    SRO_Defaults def = Serialization.DeserializeFromFile<SRO_Defaults>(files[0]);
                    Selected_Dest = def.Destination;
                    Selected_EndUser = def.End_User_Type;
                    Selected_Price = def.Price;
                    Selected_Priority = def.Priority;
                    Selected_Region = def.Region;
                    Selected_Site = def.Site;
                    Selected_SRO_Type = def.SRO_Type;
                    Selected_Status = def.Status;
                }
                else if (files.Length > 1)
                {
                    List<String> fileNames = files.OrderBy(x => x.Name).Select(x => x.Name).ToList();
                    Message_Window mw = Message_Window.Custom_List(fileNames, "Select Defaults", Window_Sizes.Small, Window_Sizes.Medium, MessageButtons.EnterCancel);
                    mw.ShowDialog();
                    if (mw.dp_DialogResult == MessageButtons.Enter)
                    {
                        String selected = mw.dp_ComboBoxSelected.ToString();
                        selected = Path.Combine(Application_Helper.Application_Data_Folder_Name(), selected);

                        SRO_Defaults def = Serialization.DeserializeFromFile<SRO_Defaults>(selected);
                        Selected_Dest = def.Destination;
                        Selected_EndUser = def.End_User_Type;
                        Selected_Price = def.Price;
                        Selected_Priority = def.Priority;
                        Selected_Region = def.Region;
                        Selected_Site = def.Site;
                        Selected_SRO_Type = def.SRO_Type;
                        Selected_Status = def.Status;
                    }
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region ReasonAddCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ReasonAddCommand</summary>
        public DelegateCommand ReasonAddCommand { get; set; }

        private Boolean CanCommandReasonAdd()
        {
            if (sro == null) return false;
            return true;
        }

        private void CommandReasonAdd()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                FS_Reas_Gen.ToStringHandler = (x) => { return $"{((FS_Reas_Gen)x).ReasonGen} - {((FS_Reas_Gen)x).Description}"; };
                Message_Window ew = new Message_Window(Window_Add_Types.CustomList, MessageButtons.OkCancel, ImageType.Info);
                foreach (var reason in Reasons) ew.Add_Custom_List_Item(reason);
                ew.Set_Window_Size(Window_Sizes.MediumSmall, Window_Sizes.Small);
                ew.dp_Label = "Select Reason";
                ew.ShowDialog();
                if (ew.dp_DialogResult == MessageButtons.OK)
                {
                    String Reason_General = ((FS_Reas_Gen)ew.dp_ComboBoxSelected).ReasonGen;
                    String Reason_Specific = null;
                    var specs = Specific_Reasons.Where(x => x.ReasonGen == Reason_General);

                    FS_Reas_Spec.ToStringHandler = (x) => { return String.Format("{0} - {1}", ((FS_Reas_Spec)x).ReasonSpec, ((FS_Reas_Spec)x).Description); };
                    ew = new Message_Window(Window_Add_Types.CustomList, MessageButtons.OkCancel, ImageType.Info);
                    foreach (var spec in specs) ew.Add_Custom_List_Item(spec);
                    ew.Set_Window_Size(Window_Sizes.MediumSmall, Window_Sizes.Small);
                    ew.dp_Label = "Select Specific";
                    ew.ShowDialog();
                    if (ew.dp_DialogResult == MessageButtons.OK)
                    {
                        Reason_Specific = ((FS_Reas_Spec)ew.dp_ComboBoxSelected).ReasonSpec;
                    }


                    FS_Inc_Reason r = Syteline_IncReas_Web_Service_Client_REST.Select_Incident_Reason_INCNUM_REAGEN_REASPEC(Token, inc.Inc_Num, Reason_General, Reason_Specific);
                    if (r != null)
                    {
                        String msg = String.Format("The general reason: {0} with specific reason: {1} already exists for SRO/Incident: {2}/{3}", Reason_General, Reason_Specific, sro.SRO_Num, inc.Inc_Num);
                        logger.Warn(msg);
                        if (Application.Current != null)
                        {
                            if (Application.Current.Dispatcher != null)
                            {
                                Application.Current.Dispatcher.Invoke((Action)delegate ()
                                {
                                    Message_Window.Warning(msg).ShowDialog();
                                });
                            }
                        }
                        return;
                    }

                    r = Syteline_IncReas_Web_Service_Client_REST.Insert_FS_Inc_Reason(Token, sro.SRO_Num, Reason_General, Reason_Specific);
                    if (r == null)
                    {
                        String msg = String.Format("The general reason: {0} with specific reason: {1} could not be created for SRO/Incident: {2}/{3}", Reason_General, Reason_Specific, sro.SRO_Num, inc.Inc_Num);
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
                        return;
                    }

                    Get_Reason_Data(inc.Inc_Num);
                }
                FS_Reas_Gen.ToStringHandler = null;
                FS_Reas_Spec.ToStringHandler = null;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region ReasonDelCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ReasonDelCommand</summary>
        public DelegateCommand ReasonDelCommand { get; set; }
        private Boolean CanCommandReasonDel() { return Selected_Reason != null; }
        private void CommandReasonDel()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Syteline_IncReas_Web_Service_Client_REST.Delete_Incident_Reason(Token, SRO_Number, Selected_Reason.General_Reason_Code, Selected_Reason.Specific_Reason_Code) == true)
                {
                    Reason_List.Remove(Selected_Reason);
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region NoteSubmitCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: NoteSubmitCommand</summary>
        public DelegateCommand NoteSubmitCommand { get; set; }

        private Boolean CanCommandNoteSubmit()
        {
            if (sro == null) return false;
            return true;
        }

        private void CommandNoteSubmit()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Task back = new Task(CommandNoteSubmit_Back);
                back.ContinueWith(Task_ExceptionHander, TaskContinuationOptions.OnlyOnFaulted);
                back.Start();
                logger.Debug("Started background process: CommandNoteSubmit_Back");
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region ClipBoardCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClipBoardCommand</summary>
        public DelegateCommand ClipBoardCommand { get; set; }

        private Boolean CanCommandClipBoard() { return String.IsNullOrEmpty(SRO_Number) == false; }
        private void CommandClipBoard()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Clipboard.SetText(SRO_Number);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region DealerLookupCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: DealerLookupCommand</summary>
        public DelegateCommand DealerLookupCommand { get; set; }
        private Boolean CanCommandDealerLookup()
        {
            return true;
        }

        private void CommandDealerLookup()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region LookupDealerCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: LookupDealerCommand</summary>
        public DelegateCommand LookupDealerCommand { get; set; }
        private Boolean CanCommandLookupDealer()
        {
            return true;
        }

        private void CommandLookupDealer()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if ((InfrastructureModule.Customer_Address_List == null) || (InfrastructureModule.Customer_Address_List.Count == 0))
                {
                    Message_Window.Warning("No Customer records could be found", "No Data", Window_Sizes.Small, Window_Sizes.Small).ShowDialog();
                    return;
                }

                Customer_Lookup_Window cl = new Customer_Lookup_Window();
                cl.dp_lstAddresses = InfrastructureModule.Customer_Address_List;
                cl.dp_Title = "Dealer/Distributor List";
                cl.dp_Region_Visibility = Visibility.Visible;
                cl.dp_IsAPAC_Checked = Region.IsAPAC;
                cl.dp_IsEMEA_Checked = Region.IsEMEA;
                cl.dp_IsCANA_Checked = Region.IsCANA;
                cl.dp_IsLTAM_Checked = Region.IsLTAM;
                cl.dp_IsUSA_Checked = Region.IsUSA;
                cl.ShowDialog();
                if (cl.dp_DialogResult == true)
                {
                    Dealer_Num = cl.dp_Selected_Address.Account_Number;
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

        private void Application_Login_Handler(Object obj)
        {
            logger.Fatal(MethodBase.GetCurrentMethod().Name);
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                try
                {
                    partner = Syteline_FSPartner_Web_Service_Client_REST.Select_FS_Partner(Token);
                    if (partner != null)
                    {
                        Partner_ID = partner.Partner_ID;
                        Partner_Description = partner.Name;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString(ex));
                }

                Run_Background(Load_Priorities, Priorities_Completed_Hander);
                Run_Background(Load_Statuses, Status_Completed_Hander);
                Run_Background(Load_Sites, Sites_Completed_Hander);
                Run_Background(Load_Types, SROTypes_Completed_Hander);
                Run_Background(Load_Prices, Prices_Completed_Hander);
                Run_Background(Load_EndUsers, EndUsers_Completed_Hander);
                Run_Background(Load_Regions, Regions_Completed_Hander);
                Run_Background(Load_Destinations, Destinations_Completed_Hander);
                Run_Background(Load_Reasons, Reasons_Completed_Hander);
                Run_Background(Load_Specific_Reasons, Specific_Completed_Hander);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
                //return null;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void Run_Background(Action bgProc, Action<Task> bgProcComp, Action<Task> bgProcErr = null)
        {
            if (bgProc != null)
            {
                Task back = new Task(bgProc);
                if (bgProcComp != null) back.ContinueWith(bgProcComp, TaskContinuationOptions.OnlyOnRanToCompletion);
                if (bgProcErr != null) back.ContinueWith(bgProcErr, TaskContinuationOptions.OnlyOnFaulted);
                back.Start();
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_Dealers_Loaded_Handler  -- Event: SART_Dealers_Loaded_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_Dealers_Loaded_Handler(Boolean loaded)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (loaded == true)
                {
                    if (InfrastructureModule.Container.IsRegistered<Dealer_Info>(Dealer_Info.Name) == false)
                    {
                        logger.Error("Dealer_Info: {0} is not registered", Dealer_Info.Name);
                        return;
                    }
                    Dealer_Info DealerInfo = InfrastructureModule.Container.Resolve<Dealer_Info>(Dealer_Info.Name);
                    if (DealerInfo == null)
                    {
                        logger.Error("Dealer_Info: {0} could not be resolved", Dealer_Info.Name);
                        return;
                    }

                    if ((DealerInfo != null) && (DealerInfo.Dealer_List != null))
                    {
                        if (Application.Current != null)
                        {
                            if (Application.Current.Dispatcher != null)
                            {
                                Application.Current.Dispatcher.Invoke((Action)delegate ()
                                {
                                    ContextMenu cm = new ContextMenu();
                                    foreach (String dealer in DealerInfo.Dealer_List)
                                    {
                                        if (String.IsNullOrEmpty(dealer) == false)
                                        {
                                            MenuItem mi = new MenuItem();
                                            mi.Header = dealer;
                                            mi.Name = "N" + DealerInfo.Accounts[dealer].Trim();
                                            mi.Click += Dealer_Selection_Click;
                                            cm.Items.Add(mi);
                                        }
                                    }
                                    ((Create_SRO_Control)View).txtDealNum.ContextMenu = cm;
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
                //return null;
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

        /// <summary>Public Method - IsNavigationTarget</summary>
        /// <param name="navigationContext">NavigationContext</param>
        /// <returns>bool</returns>
        public bool IsNavigationTarget(NavigationContext navigationContext) { return true; }

        /// <summary>Public Method - OnNavigatedFrom</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Initialize_SRO_ContextMenu();
        }


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private SLCustomers Set_Customer()
        {
            SLCustomers cust = null;
            String key = String.Format("{0}-{1}", _Dealer_Num, _Dealer_Seq);
            if (Dealers.ContainsKey(key) == false)
            {
                cust = Syteline_SLCustomers_Web_Service_Client_REST.Select_SLCustomers_CUSTNUM_CUSTSEQ(Token, _Dealer_Num, _Dealer_Seq);
                if (cust != null)
                {
                    Dealers[key] = cust;
                }
            }
            else cust = Dealers[key];
            return cust;
        }

        private void Load_Priorities()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (Token != null)
                {
                    List<FS_Prior_Code> priorties = Syteline_Priority_Web_Service_Client_REST.Select_FS_Prior_Code_All(Token);
                    if (priorties == null) Priorities = new List<FS_Prior_Code>();
                    else Priorities = priorties.OrderBy(x => x.Prior_Code).ToList();
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


        private void Load_Statuses()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (Token != null)
                {
                    List<FS_Stat_Code> stats = Syteline_FSStatCode_Web_Service_Client_REST.Select_FS_Stat_Code_All(Token);
                    if (stats == null) Statuses = new List<FS_Stat_Code>();
                    else Statuses = stats.OrderBy(x => x.Stat_Code).ToList();
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


        private void Load_Sites()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (Token != null)
                {
                    List<Sites> sites = Syteline_Sites_Web_Service_Client_REST.Select_Sites_All(Token);
                    if (sites != null) Sites = sites.OrderBy(x => x.Site).ToList();
                    else Sites = new List<Sites>();
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


        private void Load_Types()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Token != null)
                {
                    List<FS_SRO_Type> types = Syteline_SROType_Web_Service_Client_REST.Select_FS_SRO_Type_All(Token);
                    if (types == null) SRO_Types = new List<FS_SRO_Type>();
                    SRO_Types = types.OrderBy(x => x.SRO_Type).ToList();
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

        private void Load_Prices()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Token != null)
                {
                    List<PriceCode> pcs = Syteline_Price_Web_Service_Client_REST.Select_PriceCode_All(Token);
                    if (pcs == null) pcs = new List<PriceCode>();
                    Prices = pcs.OrderBy(x => x.Pricecode).ToList();
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


        private void Load_EndUsers()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Token != null)
                {
                    List<Seg_End_User_Channel> eus = Syteline_EndUsrChnl_Web_Service_Client_REST.Select_Seg_End_User_Channel_All(Token);
                    if (eus == null) eus = new List<Seg_End_User_Channel>();
                    EndUsers = eus.OrderBy(x => x.End_User_Type).ToList();
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


        private void Load_Regions()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (Token != null)
                {
                    List<FS_Region> regions = Syteline_FSReg_Web_Service_Client_REST.Select_FS_Region_All(Token);
                    if (regions != null)
                    {
                        foreach (FS_Region reg in regions)
                        {
                            Regions[reg.Region] = reg.Description;
                        }
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
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private void Load_Reasons()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                List<FS_Reas_Gen> reasonList = Syteline_ReasGen_Web_Service_Client_REST.Select_FS_Reas_Gen_All(Token);
                Reasons = reasonList.OrderBy(x => x.ReasonGen).ToList();
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


        private void Load_Specific_Reasons()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                List<FS_Reas_Spec> reasonList = Syteline_ReasSpec_Web_Service_Client_REST.Select_FS_Reas_Spec_All(Token);
                Specific_Reasons = reasonList.OrderBy(x => x.ReasonGen).ThenBy(y => y.ReasonSpec).ToList();
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


        private void Load_Destinations()
        {
            String path = Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "Destinations.xml");
            FileInfo fi = new FileInfo(path);
            if (fi.Directory.Exists == false) fi.Directory.Create();
            if (fi.Exists == true)
            {
                Destinations = Serialization.DeserializeFromFile<List<String>>(fi);
            }
            if ((Destinations == null) || (Destinations.Count == 0))
            {
                Destinations = new List<String>();
                Destinations.Add("RMA");
                Destinations.Add("Customer Order");
                Destinations.Add("Purchase Order");
                Destinations.Add("PO Requisition");
                Destinations.Add("SRO");
                Serialization.SerializeToFile<List<String>>(Destinations, fi);
            }
        }


        private void Task_ExceptionHander(Task obj)
        {
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


        private void CommandSubmitSRO_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Processing Incident");
                Boolean created = inc == null;
                ///////////////////////////////////////////////////////////////////////////
                // Create/Update Incident
                if (inc == null)
                {
                    inc = new FS_Incident()
                    {
                        Inc_Date = DateTime.Now,
                        Meter_Date = DateTime.Today,
                        Ref_Site = Get_Site_DB(),
                        Site = Get_Site_DB(),
                        U_M = "EA",
                        Work_Site = Get_Site_DB(),
                        Created_By = LoginContext.UserName,
                        Dept = "Svc"
                    };
                }
                inc.Contact = Contact_Name;
                inc.Cust_Num = Dealer_Num;
                inc.Cust_Seq = Dealer_Seq;
                inc.Description = Incident_Description;
                inc.Email = Contact_Email;
                inc.Item = PartNumber;
                inc.Owner = Partner_ID;
                //inc.Owner_Name = Partner_Description;
                inc.Phone = Contact_Phone;
                inc.Prior_Code = Selected_Priority;
                //inc.Prior_Code_Desc = Priority_Description;
                inc.Ser_Num = SerialNumber;
                inc.Ssr = Partner_ID;
                //inc.SSRName = Partner_Description;
                inc.Stat_Code = Selected_Status;
                inc.Usr_Num = Consumer_Num;
                inc.Usr_Seq = Consumer_Seq;
                inc.Updated_By = LoginContext.UserName;

                if ((inc.RowPointer.HasValue == false) || (inc.RowPointer == Guid.Empty))
                {
                    inc = Syteline_Incident_Web_Service_Client_REST.Insert_FS_Incident_Object(Token, inc);
                    if (inc == null) throw new Exception("Could not create Incident.");
                }
                else if (Syteline_Incident_Web_Service_Client_REST.Update_FS_Incident_Object(Token, inc) == false)
                {
                    throw new Exception("Could not update Incident.");
                }
                // Create/Update Incident
                ///////////////////////////////////////////////////////////////////////////


                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Processing SRO Record");
                ///////////////////////////////////////////////////////////////////////////
                // Create/Update SRO
                if (sro == null)
                {
                    sro = new FS_SRO()
                    {
                        //Slsman = "60",
                        Accum_Wip = true,
                        Bill_Code = 'L',
                        Bill_Stat = 'N',
                        Bill_Type = 'C',
                        Cgs_Rev_Loc_Labor = 'S',
                        Cgs_Rev_Loc_Matl = 'S',
                        Cgs_Rev_Loc_Misc = 'S',
                        Dept = "Svc",
                        Drop_Type = 'N',
                        Exch_Rate = 1,
                        Fixed_Rate = true,
                        Inc_Num = inc.Inc_Num,
                        Product_Code = "300",
                        Ship_Code = "SPEC",
                        SRO_Stat = 'O',
                        Tax_Code1 = "NT",
                        Terms_Code = "020",
                        Whse = "SERV",
                    };
                }

                sro.Cust_Num = Dealer_Num;
                sro.Cust_Seq = Dealer_Seq;
                sro.Description = Incident_Description;
                sro.End_User_Type = Selected_EndUser;
                sro.Open_Date = inc.Inc_Date;
                sro.Partner_ID = Partner_ID;
                //sro.part = Partner_Description;
                sro.Pricecode = Selected_Price;
                sro.Region = Selected_Region;
                //sro.Region = Region_Description;
                sro.SRO_Type = Selected_SRO_Type;
                sro.Stat_Code = Selected_Status;
                sro.Usr_Num = Consumer_Num;
                sro.Usr_Seq = Consumer_Seq;
                sro.Contact = Contact_Name;
                sro.Phone = Contact_Phone;
                sro.Record_Date = DateTime.Now;
                sro.Create_Date = DateTime.Now;
                sro.Created_By = LoginContext.UserName;
                sro.Updated_By = LoginContext.UserName;
                if (sro.RowPointer == Guid.Empty)
                {
                    logger.Debug("Attempting to add an SRO record for unit: {0}", SerialNumber);
                    var srorec = Syteline_FSSRO_Web_Service_Client_REST.Insert_FS_SRO_Object(Token, sro);
                    if (srorec == null)
                    {
                        Syteline_Incident_Web_Service_Client_REST.Delete_FS_Incident_Key(Token, inc.RowPointer.Value);
                        inc = null;
                        String msg = "An error occurred and the SRO could not be generated. Please contact Segway Service Software for assistance.";
                        logger.Error(msg);

                        if (Application.Current != null)
                        {
                            if (Application.Current.Dispatcher != null)
                            {
                                Application.Current.Dispatcher.Invoke((Action)delegate ()
                                {
                                    Message_Window m = new Message_Window(msg, MessageButtons.OK, ImageType.Error);
                                    m.dp_Label = "Error";
                                    m.Set_Window_Size(Window_Sizes.Large, Window_Sizes.MediumSmall);
                                    m.dp_MessageVertAlign = VerticalAlignment.Center;
                                    m.dp_MessageHorizAlign = HorizontalAlignment.Center;
                                    m.ShowDialog();
                                });
                            }
                        }
                        return;
                    }
                    sro = srorec;
                    SRO_Number = sro.SRO_Num;
                    logger.Debug("Successfully created an SRO record for unit: {0}, SRO: {1}", SerialNumber, SRO_Number);
                }
                else
                {
                    logger.Debug("Attempting to update an SRO record ({1}) for unit: {0}", SerialNumber, SRO_Number);
                    if (Syteline_FSSRO_Web_Service_Client_REST.Update_FS_SRO_Object(Token, sro) == false)
                    {
                        String msg = "An error occurred and the SRO may not have been updated. Please contact Segway Service Software for assistance.";
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

                        return;
                    }
                }

                // Create/Update SRO
                ////////////////////////////////////////////////////////////////////////////////////////////

                var customer = Syteline_Customer_Web_Service_Client_REST.Select_Customer_CUSTNUM_and_SEQNUM(Token, Token.LoginContext.Customer_ID, 0);


                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Processing SRO Line Record");
                ////////////////////////////////////////////////////////////////////////////////////////////
                // Create Service Request Line (fs_sro_line)

                logger.Debug("Searching for SRO Line: {0}", sro.SRO_Num);
                sroline = Syteline_SroLine_Web_Service_Client_REST.Select_SRO_Line_SRO_LINE(Token, sro.SRO_Num, 1);
                if (sroline == null)
                {
                    logger.Debug("Did not find SRO Line: {0}", sro.SRO_Num);
                    if (Create_SRO_Line() == false)
                    {
                        return;
                    }
                }

                // Create Service Request Line (fs_sro_line)
                ////////////////////////////////////////////////////////////////////////////////////////////



                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Processing SRO Operation Record");
                ////////////////////////////////////////////////////////////////////////////////////////////
                // Create Service Request Operation (fs_sro_oper)

                logger.Debug("Searching for SRO Operation for: {0}, line: {1}", sro.SRO_Num, sroline.SRO_Line);
                FS_SRO_Oper sroOper = Syteline_SroOp_Web_Service_Client_REST.Select_FS_SRO_Oper_SRO_LINE_OP(Token, sro.SRO_Num, sroline.SRO_Line, 10);
                if (sroOper == null)
                {
                    sroOper = Create_SRO_Operation();
                }

                // Create Service Request Operation (fs_sro_oper)
                ////////////////////////////////////////////////////////////////////////////////////////////


                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Processing SRO Rec Test Record");
                ////////////////////////////////////////////////////////////////////////////////////////////
                // Create Seg_FS_Sroline_Rec_Test
                Seg_FS_Sroline_Rec_Test sroRecTest = Syteline_FSSROLRT_Web_Service_Client_REST.Select_Seg_FS_Sroline_Rec_Test(Token, sro.SRO_Num, sroline.SRO_Line);
                if (sroRecTest == null)
                {
                    sroRecTest = new Seg_FS_Sroline_Rec_Test()
                    {
                        SRO_Num = sro.SRO_Num,
                        SRO_Line = sroline.SRO_Line,
                        Create_Date = DateTime.Now,
                        Record_Date = DateTime.Now,
                        Created_By = LoginContext.UserName,
                        Updated_By = LoginContext.UserName,
                        RowPointer = Guid.NewGuid()
                    };
                    sroRecTest = Syteline_FSSROLRT_Web_Service_Client_REST.Insert_Seg_FS_Sroline_Rec_Test_Object(Token, sroRecTest);
                }

                // Create Seg_FS_Sroline_Rec_Test
                ////////////////////////////////////////////////////////////////////////////////////////////


                if ((sro.Seg_Is_Warr.HasValue == false) || (sro.Seg_Is_Warr.Value == false))
                {
                    var warrlist = Syteline_UntWarr_Web_Service_Client_REST.Select_Unit_Warranty_Serial(Token, sro.Ser_Num);
                    if ((warrlist != null) && (warrlist.Count > 0))
                    {
                        DateTime dt = DateTime.Today;
                        warrlist = warrlist.Where(x => (x.Start_Date.Date <= dt) && (x.End_Date.Date >= dt)).ToList();
                        if (warrlist.Count > 0)
                        {
                            IsWarranty = true;
                        }
                    }
                }

                if (created == true)
                {
                    if (Application.Current != null)
                    {
                        if (Application.Current.Dispatcher != null)
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate ()
                            {
                                String Msg = String.Format("The SRO: {0} was successfully created.", sro.SRO_Num);
                                logger.Info(Msg);
                                Message_Window mw = new Message_Window(Msg, MessageButtons.OK, ImageType.Success, Window_Add_Types.TextDisplay);
                                mw.ShowDialog();
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Application.Current != null)
                {
                    if (Application.Current.Dispatcher != null)
                    {
                        Application.Current.Dispatcher.Invoke((Action)delegate ()
                        {
                            String msg = Exception_Helper.FormatExceptionString(ex);
                            logger.Error(msg);
                            Message_Window m = new Message_Window(msg, MessageButtons.OK, ImageType.Error);
                            m.dp_Label = "Error";
                            m.Set_Window_Size(Window_Sizes.Large, Window_Sizes.MediumSmall);
                            m.dp_MessageVertAlign = VerticalAlignment.Center;
                            m.dp_MessageHorizAlign = HorizontalAlignment.Center;
                            m.ShowDialog();
                        });
                    }
                }
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private Boolean Create_SRO_Line()
        {
            sroline = new FS_SRO_Line();
            sroline.SRO_Num = sro.SRO_Num;
            sroline.SRO_Line = 1;
            sroline.Record_Date = DateTime.Now;
            sroline.Create_Date = DateTime.Now;
            sroline.Created_By = LoginContext.UserName;
            sroline.Updated_By = LoginContext.UserName;

            ////////////////////////////////
            // units is queried above
            if (fsunit != null)
            {
                sroline.Product_Code = sro.Product_Code;
                //sroline._SEG_untCust_num = Dealer_Num;
                //sroline._SEG_untCust_seq = Dealer_Seq;
                //sroline._SEG_untUsr_num = Consumer_Num;
                //sroline._SEG_untUsr_seq = Consumer_Seq;
                //sroline._SEG_IncSsr = inc.SSR;
                //sroline._SEG_IncDescription = inc.Description;
                sroline.Description = inc.Description;
                //sroline.Datefld = fssros.Datefld;
                if (sro.Accum_Wip.HasValue) sroline.Accum_Wip = sro.Accum_Wip;
                sroline.Awaiting_Parts = sro.Awaiting_Parts;
                sroline.Bill_Code = sro.Bill_Code.ToString();
                sroline.Bill_Stat = sro.Bill_Stat.ToString();
                sroline.Bill_Type = sro.Bill_Type.ToString();
                sroline.Cgs_Rev_Loc_Labor = sro.Cgs_Rev_Loc_Labor.ToString();
                sroline.Cgs_Rev_Loc_Matl = sro.Cgs_Rev_Loc_Matl.ToString();
                sroline.Cgs_Rev_Loc_Misc = sro.Cgs_Rev_Loc_Misc.ToString();
                sroline.Description = fsunit.Description;
                sroline.Dept = sro.Dept;
                sroline.Inc_Num = inc.Inc_Num;
                sroline.Item = fsunit.Item;
                sroline.Pricecode = sro.Pricecode;
                sroline.Product_Code = sro.Product_Code;
                sroline.Partner_ID = Partner_ID;
                sroline.Ser_Num = SerialNumber;
                sroline.SRO_Type = sro.SRO_Type;
                sroline.Stat = "O"; // Format_Incident_Status_For_SROLine(IncData.Incident_Status_Name);
                //sroline.SRO_Whse = sro.Whse;
                sroline.U_M = "EA";


                sroline.Qty = 1;
                sroline.Qty_Conv = 1;
                if (sro.Accum_Wip.HasValue) sroline.Accum_Wip = sro.Accum_Wip.Value;
                sroline.Awaiting_Parts = sro.Awaiting_Parts;

                if ((sroline.RowPointer.HasValue == false) || (sroline.RowPointer.Value == Guid.Empty))
                {
                    logger.Debug("Adding SRO Line: {0},{1}", sroline.SRO_Num, sroline.SRO_Line);
                    var newline = Syteline_SroLine_Web_Service_Client_REST.Insert_FS_SRO_Line_Object(Token, sroline);
                    if (newline == null)
                    {
                        String msg = String.Format("An error occurred during the creation of the SRO Line for SRO: {0}. Please contact Segway Service Software for assistance.", sroline.SRO_Num);
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
                        return false;
                    }
                    else sroline = newline;
                }
                else if (Syteline_SroLine_Web_Service_Client_REST.Update_FS_SRO_Line_Object(Token, sroline) == false)
                {
                    String msg = String.Format("An error occurred during the update of the SRO Line for SRO: {0}. Please contact Segway Service Software for assistance.", sroline.SRO_Num);
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
                    return false;
                }
            }
            else
            {
                logger.Warn("Could not find unit: {0} in Syteline", inc.Ser_Num);
                return false;
            }
            return true;
        }

        private void CommandNoteSubmit_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                Note_Data nd = new Note_Data()
                {
                    User_Name = Token.LoginContext.UserName,
                    Note = SRO_Note,
                    ObjectTypeNumber = inc.Inc_Num,
                    ObjectTypeID = inc.RowPointer.Value,
                    Creation_Date = DateTime.Now,
                    Header = "fs_sro"
                };

                if (Syteline_NoteData_Web_Service_Client_REST.Insert_Note_Data_Object(Token, nd) != null)
                {
                    Get_Note_Data();
                    SRO_Note = null;
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
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
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void Get_Note_Data()
        {
            List<Object_Notes_Data> objnotes = Syteline_ObjNotDat_Web_Service_Client_REST.Select_Object_Notes_Data_Key(Token, inc.RowPointer.Value);

            //List<Note_Data> notes = Syteline_NoteData_Web_Service_Client_REST.Select_Note_Data_INCNUM(Token, inc.Inc_Num);
            //if (notes != null)
            //{
            //    notes.Sort(new Note_Data_Comparer());
            //}
            //else
            //{
            List<Note_Data> notes = new List<Note_Data>();
            //}

            Application.Current.Dispatcher.Invoke((Action)delegate ()
            {
                Note_List = new ObservableCollection<Note_Data>(notes);
            });
        }

        private static String Get_Site_DB()
        {
            String site = Serialization.DeserializeFromFile<String>(Path.Combine(Application_Helper.Application_Folder_Name(), "Site.xml"));
            if (String.IsNullOrEmpty(site) == true)
            {
                switch (RunMode.Mode)
                {
                    case RunTimeMode.Production:
                        site = "BDNH";
                        break;
                    case RunTimeMode.Test:
                    case RunTimeMode.Local:
                    default:
                        site = "BDNHPLT2";
                        break;
                }
                //Serialization.SerializeToFile<String>(site, Path.Combine(Application_Helper.Application_Folder_Name(), "Site.xml"));
            }
            return site;
        }


        ////////////////////////////////////////
        // Serial_Back - Background Process
        private void Serial_Back()
        {
            if (String.IsNullOrEmpty(_SerialNumber) == true) return;
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Retrieving Unit Record");
                fsunit = Syteline_Units_Web_Service_Client_REST.Select_FS_Unit_SER_NUM(Token, _SerialNumber);
                if (fsunit != null)
                {
                    if ((String.IsNullOrEmpty(fsunit.Unit_Stat_Code) == true) ||
                        (String.Compare(fsunit.Unit_Stat_Code, "Registered", true) == 0) ||
                        (String.Compare(fsunit.Unit_Stat_Code, "Valid", true) == 0))
                    {
                        PartNumber = fsunit.Item;
                        Dealer_Num = LoginContext.Customer_ID;
                        Dealer_Seq = 0;
                        Consumer_Num = fsunit.Usr_Num;
                        if (fsunit.Usr_Seq.HasValue == false) Consumer_Seq = 0;
                        else Consumer_Seq = (int)fsunit.Usr_Seq.Value;
                    }
                    else throw new Exception(String.Format("An SRO can't be generated for a unit with status code: {0}", fsunit.Unit_Stat_Code));
                }
                else
                {
                    Clear_Data();
                    throw new Exception(String.Format("Serial: {0} does not exist", _SerialNumber));
                }

                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Retrieving Customer Record");
                Customer customer = Syteline_Customer_Web_Service_Client_REST.Select_Customer_CUSTNUM_and_SEQNUM(Token, Dealer_Num, Dealer_Seq);
                if (customer != null) Selected_Region = customer.Charfld1;


                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Retrieving Incident and SRO Records");

                SART_Work_Order wo = Syteline_WO_Web_Service_Client_REST.Get_Open_SRO(Token, _SerialNumber);
                if (wo != null)
                {
                    inc = Syteline_Incident_Web_Service_Client_REST.Select_FS_Incident_Key(Token, wo.inc_RowPointer.Value);
                    SRO_Number = wo.SRO_Num;
                    Dealer_Num = wo.Cust_Num;
                    Dealer_Seq = (Int32)wo.Cust_Seq;
                    Selected_SRO_Type = wo.SRO_Type;
                    Selected_Priority = inc.Prior_Code;
                    Selected_Status = wo.Stat_Code;
                    Selected_Site = inc.Site;
                    Selected_Dest = "SRO";
                    if (String.IsNullOrEmpty(Dealer_Num) == true) Dealer_Num = inc.Cust_Num;
                    Dealer_Seq = (int)inc.Cust_Seq;
                    Selected_EndUser = wo.End_User_Type;
                    Selected_Price = wo.Price_Code;
                    Selected_Region = wo.Region;
                    if (Partner_ID != wo.Partner_ID)
                    {
                        Partner_ID = wo.Partner_ID;
                        Partner_Description = Syteline_FSPartner_Web_Service_Client_REST.Select_FS_Partner_PARTNERID_NAME(Token, wo.Partner_ID);
                    }
                    //Partner_Description = inc.SSRName;
                    Incident_Description = inc.Description;
                    Contact_Name = wo.Contact;
                    Contact_Phone = wo.Phone;

                    Get_Note_Data();
                    Get_Reason_Data(inc.Inc_Num);

                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Retrieving SRO Record");
                    sro = Syteline_FSSRO_Web_Service_Client_REST.Select_FS_SRO_SRONUM(Token, wo.SRO_Num);

                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Retrieving SRO Line Record");
                    sroline = Syteline_SroLine_Web_Service_Client_REST.Select_SRO_Line_SRO_LINE(Token, wo.SRO_Num, 1);
                    if (sroline == null)
                    {
                        if (Create_SRO_Line() == false)
                        {
                            throw new Exception(String.Format("Unable to Open/Create SRO: {0} -- SRO Line Error.", wo.SRO_Num));
                        }
                    }

                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Retrieving SRO Operation Record");
                    FS_SRO_Oper sroOper = Syteline_SroOp_Web_Service_Client_REST.Select_FS_SRO_Oper_SRO_LINE_OP(Token, wo.SRO_Num, sroline.SRO_Line, 10);
                    if (sroOper == null)
                    {
                        sroOper = Create_SRO_Operation();
                    }

                    Last_10.Update_Most_Recent(Last10SROsFilename, wo.SRO_Num);

                    if (Application.Current != null)
                    {
                        if (Application.Current.Dispatcher != null)
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate ()
                            {
                                Initialize_SRO_ContextMenu();
                            });
                        }
                    }
                    return;
                }
                else
                {
                    sro = null;
                    inc = null;
                    sroline = null;
                    Clear_Data(false);
                }

                List<Unit_Warranty> warranties = Syteline_UntWarr_Web_Service_Client_REST.Select_Unit_Warranty_Serial(Token, SerialNumber);
                if (warranties == null) IsWarranty = false;
                else
                {
                    var warr = warranties.Where(x => (x.Start_Date.Date <= DateTime.Now) && (x.End_Date.Date > DateTime.Now) && (x.Warr_Code == "W1Yr") && (x.Item == fsunit.Item)).FirstOrDefault();
                    if (warr == null) IsWarranty = false;
                    else IsWarranty = true;
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                throw;
            }
            finally
            {
                ReasonAddCommand.RaiseCanExecuteChanged();
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        // Serial_Back - Background Process
        ////////////////////////////////////////

        ////////////////////////////////////////
        // Serial Exception Handler
        private void Serial_ExceptionHander(Task obj)
        {
            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        String msg = obj.Exception.InnerException.Message;
                        //                        String msg = Exception_Helper.FormatExceptionString(obj.Exception.InnerException);
                        logger.Error(msg);
                        Message_Window m = new Message_Window(msg, MessageButtons.OK, ImageType.Error);
                        m.dp_Label = "Error";
                        m.Set_Window_Size(Window_Sizes.Large, Window_Sizes.MediumSmall);
                        m.dp_MessageVertAlign = VerticalAlignment.Center;
                        m.dp_MessageHorizAlign = HorizontalAlignment.Center;
                        m.ShowDialog();
                    });
                }
            }
        }
        // Serial Exception Handler
        ////////////////////////////////////////


        private void Get_Reason_Data(String incNum)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                List<FS_Inc_Reason> reasons = Syteline_IncReas_Web_Service_Client_REST.Select_FS_Inc_Reason_INC_NUM(Token, incNum);
                if (reasons == null) reasons = new List<FS_Inc_Reason>();
                else reasons = reasons.OrderBy(x => x.Reason_Gen).ToList();
                List<Incident_Reason_Data> irdList = new List<Incident_Reason_Data>();
                foreach (var reason in reasons)
                {
                    Incident_Reason_Data ird = new Incident_Reason_Data() { General_Reason_Code = reason.Reason_Gen, Specific_Reason_Code = reason.Reason_Spec };

                    var reas = Reasons.Where(x => x.ReasonGen == reason.Reason_Gen).FirstOrDefault();
                    if (reas != null)
                    {
                        ird.General_Reason_Description = reas.Description;
                    }

                    var specRea = Specific_Reasons.Where(x => (x.ReasonGen == reason.Reason_Gen) && (x.ReasonSpec == reason.Reason_Spec)).FirstOrDefault();
                    if (specRea != null)
                    {
                        ird.Specific_Reason_Description = specRea.Description;
                    }
                    irdList.Add(ird);
                }
                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    Reason_List = new ObservableCollection<Incident_Reason_Data>(irdList);
                });
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


        private void Copy_Common_Fields(Object dest, Object line, Object src2)
        {
            Type destType = dest.GetType();
            Type src2Type = src2.GetType();
            Type src1Type = line.GetType();

            foreach (PropertyInfo opPI in destType.GetProperties())
            {
                Object o = opPI.GetValue(dest, null);
                if (o != null)
                {
                    continue;
                }

                try
                {
                    // Get property info from SRO Line
                    PropertyInfo pi = src1Type.GetProperty(opPI.Name);
                    if (pi != null)
                    {
                        // Get value from SRO Line
                        Object fromObj = pi.GetValue(line, null);
                        if (fromObj != null)
                        {
                            // Set value from SRO Line into matching field in SRO Operation
                            opPI.SetValue(dest, fromObj, null);
                            continue;
                        }
                    }

                    // Get property info from SRO
                    pi = src2Type.GetProperty(opPI.Name);
                    if (pi != null)
                    {
                        // Get value from SRO
                        Object fromObj = pi.GetValue(src2, null);
                        if (fromObj != null)
                        {
                            // Set value from SRO into matching field in SRO Operation
                            opPI.SetValue(dest, fromObj, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(ex));
                }
            }
        }


        private FS_SRO_Oper Create_SRO_Operation()
        {
            FS_SRO_Oper sroOper = new FS_SRO_Oper();
            sroOper.SRO_Oper = 10;
            sroOper.Record_Date = DateTime.Now;
            sroOper.Create_Date = DateTime.Now;
            sroOper.Created_By = LoginContext.UserName;
            sroOper.Updated_By = LoginContext.UserName;

            Copy_Common_Fields(sroOper, sroline, sro);
            sroOper.Stat = 'O';
            if (sroOper.SRO_Line == 0) sroOper.SRO_Line = 1;
            //sroOper.SROSroStat = "Open";
            //sroOper.SROLineStat = "Open";
            if ((partner != null) && (String.IsNullOrEmpty(partner.UF_Seg_Login) == false))
            {
                if (partner.Charfld1 == null)
                {
                    sroOper.Oper_Code = "REPAIR";
                }
                else if (partner.Charfld1.ToUpper() == "APAC")
                {
                    sroOper.Oper_Code = "SINGAPORE";
                }
                else if (partner.Charfld1.ToUpper() == "EMEA")
                {
                    sroOper.Oper_Code = "GMBH";
                }
                else
                {
                    sroOper.Oper_Code = "REPAIR";
                }
            }
            else
            {
                sroOper.Oper_Code = "REPAIR";
            }

            if (OperationCodes.ContainsKey(sroOper.Oper_Code) == false)
            {
                FS_Oper_Code code = Syteline_OpCode_Web_Service_Client_REST.Select_FS_Oper_Code_OPER_CODE(Token, sroOper.Oper_Code);
                if (code != null)
                {
                    OperationCodes[code.Oper_Code] = code.Description;
                    sroOper.Description = OperationCodes[sroOper.Oper_Code];
                }
            }
            else
            {
                sroOper.Description = OperationCodes[sroOper.Oper_Code];
            }

            sroOper = Syteline_SroOp_Web_Service_Client_REST.Insert_FS_SRO_Oper_Object(Token, sroOper);
            if (sroOper != null) logger.Debug("SRO Op - SRO: {0}, Line: {1}, Op: {2}", sroOper.SRO_Num, sroOper.SRO_Line, sroOper.SRO_Oper);
            else logger.Warn("Attempt to create an SRO Operation Failed");
            return sroOper;
        }

        private void Dealer_Selection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (sender == null) return;
                if ((sender is MenuItem) == false) return;
                Dealer_Num = ((MenuItem)sender).Name.Substring(1).Trim();
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


        private void Load_ShipTos()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                List<int> shipList = Syteline_Customer_Web_Service_Client_REST.Get_Dealer_ShipTos(Token, Dealer_Num);
                if (shipList == null) return;


                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    ContextMenu cm = new ContextMenu();
                    foreach (int ship in shipList)
                    {
                        MenuItem mi = new MenuItem();
                        mi.Header = ship.ToString();
                        mi.Name = "I" + ship.ToString();
                        mi.Click += ShipTo_Click;
                        cm.Items.Add(mi);
                    }
                });
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


        private void ShipTo_Click(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            if (((MenuItem)sender).Header == null) return;
            if (int.TryParse(((MenuItem)sender).Header.ToString(), out _Dealer_Seq) == true)
            {
                OnPropertyChanged("Dealer_Seq");
            }
        }


        ////////////////////////////////////////
        // Priorities Completion Handler
        private void Priorities_Completed_Hander(Task obj)
        {
            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        if (Priorities != null)
                        {
                            logger.Debug("Loading: Priority_List");
                            //List<String> list = new List<String>(Priorities.Keys);
                            //list.Sort();
                            Priority_List = new ObservableCollection<String>(Priorities.OrderBy(x => x.Prior_Code).Select(y => y.Prior_Code));
                        }
                    });
                }
            }
        }
        // Priorities Completion Handler
        ////////////////////////////////////////



        ////////////////////////////////////////
        // Status Completion Handler
        private void Status_Completed_Hander(Task obj)
        {
            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        if (Statuses != null)
                        {
                            logger.Debug("Loading: Status_List");
                            //List<String> list = new List<String>(Statuses.Keys);
                            //list.Sort();
                            Status_List = new ObservableCollection<String>(Statuses.OrderBy(x => x.Stat_Code).Select(y => y.Stat_Code));
                        }
                    });
                }
            }
        }
        // Status Completion Handler
        ////////////////////////////////////////




        ////////////////////////////////////////
        // Sites Completion Handler
        private void Sites_Completed_Hander(Task obj)
        {
            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        if (Sites != null)
                        {
                            logger.Debug("Loading: Site_List");
                            //List<String> list = new List<String>(Sites.Keys);
                            //list.Sort();
                            Site_List = new ObservableCollection<String>(Sites.OrderBy(x => x.Site).Select(y => y.Site).ToList());
                        }
                    });
                }
            }
        }
        // Sites Completion Handler
        ////////////////////////////////////////




        ////////////////////////////////////////
        // SROTypes Completion Handler
        private void SROTypes_Completed_Hander(Task obj)
        {
            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        if (SRO_Types != null)
                        {
                            logger.Debug("Loading: SRO_Type_List");
                            SRO_Type_List = new ObservableCollection<String>(SRO_Types.OrderBy(y => y.SRO_Type).Select(x => x.SRO_Type).ToList());
                        }
                    });
                }
            }
        }
        // SROTypes Completion Handler
        ////////////////////////////////////////



        ////////////////////////////////////////
        // Prices Completion Handler
        private void Prices_Completed_Hander(Task obj)
        {
            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        if (Prices != null)
                        {
                            logger.Debug("Loading: Price_List");
                            //List<String> list = new List<String>(Prices.Keys);
                            //list.Sort();
                            Price_List = new ObservableCollection<String>(Prices.Select(x => x.Pricecode));
                        }
                    });
                }
            }
        }
        // Prices Completion Handler
        ////////////////////////////////////////




        ////////////////////////////////////////
        // EndUsers Completion Handler
        private void EndUsers_Completed_Hander(Task obj)
        {
            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {

                        if (EndUsers != null)
                        {
                            logger.Debug("Loading: EndUser_List");
                            //List<String> list = new List<String>(EndUsers.Keys);
                            //list.Sort();
                            EndUser_List = new ObservableCollection<String>(EndUsers.OrderBy(x => x.End_User_Type).Select(y => y.End_User_Type));
                        }
                    });
                }
            }
        }
        // EndUsers Completion Handler
        ////////////////////////////////////////


        ////////////////////////////////////////
        // Regions Completion Handler
        private void Regions_Completed_Hander(Task obj)
        {
            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        if (Regions != null)
                        {
                            logger.Debug("Loading: Region_List");
                            List<String> list = new List<String>(Regions.Keys);
                            list.Sort();
                            Region_List = new ObservableCollection<String>(list);
                        }
                    });
                }
            }
        }
        // Regions Completion Handler
        ////////////////////////////////////////



        ////////////////////////////////////////
        // Destinations Completion Handler
        private void Destinations_Completed_Hander(Task obj)
        {

            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        if (Destinations != null)
                        {
                            logger.Debug("Loading: Dest_List");
                            Destinations.Sort();
                            Dest_List = new ObservableCollection<String>(Destinations);
                        }
                    });
                }
            }
        }
        // Destinations Completion Handler
        ////////////////////////////////////////



        ////////////////////////////////////////
        // Reasons Completion Handler
        private void Reasons_Completed_Hander(Task obj)
        {
        }
        // Reasons Completion Handler
        ////////////////////////////////////////


        ////////////////////////////////////////
        // Reasons Completion Handler
        private void Specific_Completed_Hander(Task obj)
        {
        }
        // Reasons Completion Handler
        ////////////////////////////////////////



        private void Initialize_SRO_ContextMenu()
        {
            List<String> l10 = Last_10.Get_Most_Recent(Last10SROsFilename);
            ContextMenu cm = new ContextMenu();
            foreach (String s in l10)
            {
                MenuItem mi = new MenuItem()
                {
                    Header = s,
                    Name = s
                };
                mi.Click += Select_Last_SRO_Click;
                cm.Items.Add(mi);
            }
            ((Create_SRO_Control)View).lblSRO_Num.ContextMenu = cm;
        }


        private void Select_Last_SRO_Click(object sender, RoutedEventArgs e)
        {
            String sroNum = (String)((MenuItem)sender).Header;

            //SART_2012_Web_Service_Client_REST.Select_Incident_SRONUM(Token, sroNum);
            FS_SRO_Line sroLine = Syteline_FSSROL_Web_Service_Client_REST.Get_SRO_Line(Token, sroNum);
            if (sroLine == null) throw new Exception();
            CommandClearSRO();
            SerialNumber = sroLine.Ser_Num;
        }


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
