using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using PdfSharp.Drawing;
using Segway.Database.Objects;
using Segway.Login.Objects;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.Modules.AddWindow;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.PDFHelper;
using Segway.Syteline.Client.REST;
using Segway.Syteline.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace Segway.Service.SART
{
    /// <summary>Public Class - BlackBox_Open_ViewModel</summary>
    public class BlackBox_Open_ViewModel : ViewModelBase, BlackBox_Open_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">BlackBox_Open_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public BlackBox_Open_ViewModel(BlackBox_Open_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);
#if SART 
            eventAggregator.GetEvent<WorkOrder_Opened_Event>().Subscribe(WorkOrder_Opened_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<BlackBox_Open_Extract_Panel_Event>().Subscribe(BlackBox_Open_Extract_Panel_Handler, true);
#endif
            eventAggregator.GetEvent<BlackBox_Refresh_Event>().Subscribe(BlackBox_Refresh_Handler, true);

            #endregion

            #region Command Delegates

            ApplyCommand = new DelegateCommand(CommandApply, CanCommandApply);
            ClearCommand = new DelegateCommand(CommandClear, CanCommandClear);
            BlackBoxMergeCommand = new DelegateCommand(CommandBlackBoxMerge, CanCommandBlackBoxMerge);
            BlackBoxOpenCommand = new DelegateCommand(CommandBlackBoxOpen, CanCommandBlackBoxOpen);
            BlackBoxGraphCommand = new DelegateCommand(CommandBlackBoxGraph, CanCommandBlackBoxGraph);
            AddGraphCommand = new DelegateCommand(CommandAddGraph, CanCommandAddGraph);
            RemGraphCommand = new DelegateCommand(CommandRemGraph, CanCommandRemGraph);
            RemAllGraphCommand = new DelegateCommand(CommandRemAllGraph, CanCommandRemAllGraph);
            AddAllGraphCommand = new DelegateCommand(CommandAddAllGraph, CanCommandAddAllGraph);
            DrawGraphCommand = new DelegateCommand(CommandDrawGraph, CanCommandDrawGraph);
            CancelCommand = new DelegateCommand(CommandCancel, CanCommandCancel);

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

        #region ProcessingReport

        private Boolean _ProcessingReport = false;

        /// <summary>Property ProcessingReport of type Boolean</summary>
        public Boolean ProcessingReport
        {
            get
            {
                return _ProcessingReport;
            }
            set
            {
                _ProcessingReport = value;
                BlackBoxOpenCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region ProcessingGraphs

        private Boolean _ProcessingGraphs;

        /// <summary>Property ProcessingGraphs of type Boolean</summary>
        public Boolean ProcessingGraphs
        {
            get
            {
                return _ProcessingGraphs;
            }
            set
            {
                _ProcessingGraphs = value;
                BlackBoxGraphCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

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

        #region GraphSelect Popup Controls


        #region GraphSelect PopupOpen

        private Boolean _GraphSelect_PopupOpen;

        /// <summary>ViewModel Property: GraphSelect_PopupOpen of type: Boolean</summary>
        public Boolean GraphSelect_PopupOpen
        {
            get { return _GraphSelect_PopupOpen; }
            set
            {
                _GraphSelect_PopupOpen = value;
                OnPropertyChanged("GraphSelect_PopupOpen");
            }
        }

        #endregion

        #region Graph_Defs

        private ObservableCollection<String> _Graph_Defs;

        /// <summary>Property Graph_Defs of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Graph_Defs
        {
            get
            {
                if (_Graph_Defs == null)
                {
                    Black_Box_Graphs graphs = new Black_Box_Graphs();
                    graphs.Load();
                    List<String> graphnames = new List<string>();
                    foreach (var g in graphs.Graphs) graphnames.Add(g.Graph_Name);
                    _Graph_Defs = new ObservableCollection<String>(graphnames);
                }
                return _Graph_Defs;
            }
            set
            {
                _Graph_Defs = value;
                OnPropertyChanged("Graph_Defs");
            }
        }

        #endregion

        #region Selected_GraphDef

        private String _Selected_GraphDef;

        /// <summary>Property Selected_GraphDef of type String</summary>
        public String Selected_GraphDef
        {
            get { return _Selected_GraphDef; }
            set
            {
                _Selected_GraphDef = value;
                OnPropertyChanged("Selected_GraphDef");
            }
        }

        #endregion

        #region Graph_Sels

        private ObservableCollection<String> _Graph_Sels;

        /// <summary>Property Graph_Sels of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Graph_Sels
        {
            get
            {
                if (_Graph_Sels == null) _Graph_Sels = new ObservableCollection<String>();
                return _Graph_Sels;
            }
            set
            {
                _Graph_Sels = value;
                OnPropertyChanged("Graph_Sels");
            }
        }

        #endregion

        #region Selected_GraphSel

        private String _Selected_GraphSel;

        /// <summary>Property Selected_GraphSel of type String</summary>
        public String Selected_GraphSel
        {
            get { return _Selected_GraphSel; }
            set
            {
                _Selected_GraphSel = value;
                OnPropertyChanged("Selected_GraphSel");
            }
        }

        #endregion

        #endregion


        #region BlackBox_List

        private ObservableCollection<BSA_Black_Box> _BlackBox_List;

        /// <summary>Property BlackBox_List of type ObservableCollection&lt;BSA_Black_Box&gt;</summary>
        public ObservableCollection<BSA_Black_Box> BlackBox_List
        {
            get
            {
                if (_BlackBox_List == null) _BlackBox_List = new ObservableCollection<BSA_Black_Box>();
                return _BlackBox_List;
            }
            set
            {
                _BlackBox_List = value;
                OnPropertyChanged("BlackBox_List");
                OnPropertyChanged("Log_Count");
            }
        }

        #endregion

        #region Selected_BlackBox

        private BSA_Black_Box _Selected_BlackBox;

        /// <summary>Property Selected_BlackBox of type BSA_Black_Box</summary>
        public BSA_Black_Box Selected_BlackBox
        {
            get { return _Selected_BlackBox; }
            set
            {
                _Selected_BlackBox = value;
                OnPropertyChanged("Selected_BlackBox");
                BlackBoxOpenCommand.RaiseCanExecuteChanged();
                BlackBoxMergeCommand.RaiseCanExecuteChanged();
                BlackBoxGraphCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region IsExpanded

        private Boolean _IsExpanded;

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

        #region Log_Count

        /// <summary>Property Log_Count of type int</summary>
        public int Log_Count
        {
            get
            {
                if (BlackBox_List == null) return 0;
                return BlackBox_List.Count;
            }
            set
            {
                OnPropertyChanged("Log_Count");
            }
        }

        #endregion

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
                ApplyCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region BSASerialNumber

        private String _BSASerialNumber;

        /// <summary>Property BSASerialNumber of type String</summary>
        public String BSASerialNumber
        {
            get { return _BSASerialNumber; }
            set
            {
                _BSASerialNumber = value;
                OnPropertyChanged("BSASerialNumber");
                ApplyCommand.RaiseCanExecuteChanged();
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
                ApplyCommand.RaiseCanExecuteChanged();
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
                    _Header_Image = Image_Helper.ImageFromEmbedded(".Images.BBopen.png");
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
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region ApplyCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ApplyCommand</summary>
        public DelegateCommand ApplyCommand { get; set; }
        private Boolean CanCommandApply()
        {
            if (String.IsNullOrEmpty(PTSerialNumber) == false) return true;
            if (String.IsNullOrEmpty(BSASerialNumber) == false) return true;
            if (String.IsNullOrEmpty(WorkOrderNumber) == false) return true;
            return false;
        }

        private void CommandApply()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Thread back = new Thread(new ThreadStart(CommandApply_Back));
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


        #region ClearCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ClearCommand</summary>
        public DelegateCommand ClearCommand { get; set; }
        private Boolean CanCommandClear() { return true; }
        private void CommandClear()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                WorkOrderNumber = null;
                PTSerialNumber = null;
                BSASerialNumber = null;
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


        #region BlackBoxOpenCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BlackBoxOpenCommand</summary>
        public DelegateCommand BlackBoxOpenCommand { get; set; }
        private Boolean CanCommandBlackBoxOpen()
        {
            if (ProcessingReport == true) return false;
            return Selected_BlackBox != null;
        }
        private void CommandBlackBoxOpen()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Thread back = new Thread(new ThreadStart(CommandOpen_Back));
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


        #region BlackBoxMergeCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BlackBoxMergeCommand</summary>
        public DelegateCommand BlackBoxMergeCommand { get; set; }
        private Boolean CanCommandBlackBoxMerge()
        {
            if (((BlackBox_Open_Control)View).BB_List.SelectedItems.Count > 1)
            {
                BSA_Black_Box test = (BSA_Black_Box)((BlackBox_Open_Control)View).BB_List.SelectedItems[0];
                foreach (BSA_Black_Box item in ((BlackBox_Open_Control)View).BB_List.SelectedItems)
                {
                    if (item.BSA_Serial_Number != test.BSA_Serial_Number) return false;
                    if (item.Side != test.Side) return false;
                    if (item.Unit_ID_Serial_Number != test.Unit_ID_Serial_Number) return false;
                    if (item.User_Name != test.User_Name) return false;
                    if (item.Work_Order != test.Work_Order) return false;
                }
                return true;
            }
            return false;
        }

        private void CommandBlackBoxMerge()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                BSA_Black_Box test = (BSA_Black_Box)((BlackBox_Open_Control)View).BB_List.SelectedItems[0];
                foreach (BSA_Black_Box item in ((BlackBox_Open_Control)View).BB_List.SelectedItems)
                {
                    if (item == test) continue;
                    BSA_Black_Box bb = SART_BBB_Web_Service_Client_REST.Select_BSA_Black_Box_Key(Token, item.ID);

                    var headList = SART_BBBH_Web_Service_Client_REST.Select_BSA_Black_Box_Header_BLACKBOX_KEY(Token, bb.Black_Box_Key);
                    if (headList != null)
                    {
                        foreach (BSA_Black_Box_Header head in headList)
                        {
                            Guid orgKey = head.Blackbox_Key;
                            head.Blackbox_Key = test.Black_Box_Key;
                            if (SART_BBBH_Web_Service_Client_REST.Update_BSA_Black_Box_Header_Key(Token, head) == false)
                            {
                                throw new Exception(String.Format("Unable to merge BSA Black Box Header: {0}", orgKey));
                            }
                        }
                    }
                    SART_BBB_Web_Service_Client_REST.Delete_BSA_Black_Box_Key(Token, bb.ID);
                }
                eventAggregator.GetEvent<BlackBox_Refresh_Event>().Publish(true);
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


        #region BlackBoxGraphCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BlackBoxGraphCommand</summary>
        public DelegateCommand BlackBoxGraphCommand { get; set; }
        private Boolean CanCommandBlackBoxGraph()
        {
            if (ProcessingGraphs == true) return false;
            return Selected_BlackBox != null;
        }

        private void CommandBlackBoxGraph()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Graph_Sels.Clear();
                GraphSelect_PopupOpen = true;
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



        #region RemGraphCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: RemGraphCommand</summary>
        public DelegateCommand RemGraphCommand { get; set; }
        private Boolean CanCommandRemGraph() { return true; }
        private void CommandRemGraph()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Graph_Sels.Remove(Selected_GraphSel);
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


        #region RemAllGraphCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: RemAllGraphCommand</summary>
        public DelegateCommand RemAllGraphCommand { get; set; }
        private Boolean CanCommandRemAllGraph() { return true; }
        private void CommandRemAllGraph()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Graph_Sels.Clear();
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

        #region AddGraphCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: AddGraphCommand</summary>
        public DelegateCommand AddGraphCommand { get; set; }
        private Boolean CanCommandAddGraph() { return true; }
        private void CommandAddGraph()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Graph_Sels.Add(Selected_GraphDef);
                Selected_GraphSel = Selected_GraphDef;
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


        #region AddAllGraphCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: AddAllGraphCommand</summary>
        public DelegateCommand AddAllGraphCommand { get; set; }
        private Boolean CanCommandAddAllGraph() { return true; }
        private void CommandAddAllGraph()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                foreach (String name in Graph_Defs)
                {
                    Graph_Sels.Add(name);
                    Selected_GraphSel = name;
                }
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


        #region DrawGraphCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: DrawGraphCommand</summary>
        public DelegateCommand DrawGraphCommand { get; set; }
        private Boolean CanCommandDrawGraph() { return true; }
        private void CommandDrawGraph()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                GraphSelect_PopupOpen = false;
                Thread back = new Thread(new ThreadStart(CommandBlackBoxGraph_Back));
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
                GraphSelect_PopupOpen = false;
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

#if SART
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Opened_Handler  -- Event: WorkOrder_Opened_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Opened_Handler(Boolean opened)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Reset();
                Set_Defaults();
                Query_BlackBox();
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
#endif

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
        #region BlackBox_Refresh_Handler  -- Event: BlackBox_Refresh_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BlackBox_Refresh_Handler(Boolean refresh)
        {
            if (refresh == true)
            {
                CommandApply();
            }
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
            _Token = null;
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
        public void OnNavigatedTo(NavigationContext navigationContext) { }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private void Reset()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                PTSerialNumber = null;
                WorkOrderNumber = null;
                BSASerialNumber = null;
                BlackBox_List = null;
                Selected_BlackBox = null;
                IsExpanded = false;

                OnPropertyChanged("Orgainization_Visibility");
                OnPropertyChanged("IsDecodeVisibility");
                OnPropertyChanged("Log_Count");
                OnPropertyChanged("ShowRawData_Visibility");
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



        /// <summary>Public Method - Set_Defaults</summary>
        public void Set_Defaults()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (container.IsRegistered<SART_Work_Order>("Current_WorkOrder") == true)
                {
                    var cwo = container.Resolve<SART_Work_Order>("Current_WorkOrder");
                    if (cwo != null)
                    {
                        PTSerialNumber = cwo.PT_Serial;
                        WorkOrderNumber = cwo.Work_Order_ID;
                    }
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                throw;
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


        private void Query_BlackBox()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                if (String.IsNullOrEmpty(PTSerialNumber) == false) criteria.Add(new FieldData("Unit_ID_Serial_Number", PTSerialNumber, FieldCompareOperator.Contains));
                if (String.IsNullOrEmpty(BSASerialNumber) == false) criteria.Add(new FieldData("BSA_Serial_Number", BSASerialNumber, FieldCompareOperator.Contains));
                if (String.IsNullOrEmpty(WorkOrderNumber) == false) criteria.Add(new FieldData("Work_Order", WorkOrderNumber, FieldCompareOperator.Contains));
                List<BSA_Black_Box> BBList = null;
                if (criteria.FieldData_List.Count != 0)
                {
                    logger.Debug("criteria: {0}", criteria);
                    BBList = SART_BBB_Web_Service_Client_REST.Select_BSA_Black_Box_Criteria(Token, criteria);
                }
                if ((BBList == null) || (BBList.Count == 0))
                {
                    logger.Warn("No Black Box data");
                    BBList = new List<BSA_Black_Box>();
                }

                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    BlackBox_List = new ObservableCollection<BSA_Black_Box>(BBList);
                });
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


        private void CommandApply_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                IsExpanded = false;
                Query_BlackBox();
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

        private void CommandOpen_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                ProcessingReport = true;

                PDF_Helper pdf = new PDF_Helper();
                pdf.Create_Document();
                List<BSA_Black_Box> bbList = new List<BSA_Black_Box>();
                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    foreach (BSA_Black_Box bb in ((BlackBox_Open_Control)View).BB_List.SelectedItems)
                    {
                        bbList.Add(bb);
                    }
                });

                foreach (BSA_Black_Box bb in bbList)
                {
                    Black_Box_Services.Report(pdf, bb, Token);
                }

                String path = Path.Combine(Application_Helper.Application_Folder_Name(), "Reports", String.Format("{0}-{1} {2}.pdf", Selected_BlackBox.Unit_ID_Serial_Number,
                      Selected_BlackBox.Side, Selected_BlackBox.Date_Time_Entered.Value.ToString("yyyy-MM-dd HHmmss")));
                FileInfo fi = new FileInfo(path);
                if (fi.Directory.Exists == false) fi.Directory.Create();
                else if (fi.Exists == true) fi.Delete();
                pdf.Save(fi.FullName);
                ProcessHelper.Run(fi.FullName, redirectOutput: false, redirectError: false);
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
                ProcessingReport = false;
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private void CommandBlackBoxGraph_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                ProcessingGraphs = true;

                PDF_Helper pdf = new PDF_Helper();
                pdf.CurrentFont = new XFont("Times New Roman", 8);
                pdf.CurrentFontBold = new XFont("Times New Roman", 8, XFontStyle.Bold);

                pdf.Create_Document();
                List<BSA_Black_Box> bbList = new List<BSA_Black_Box>();
                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    foreach (BSA_Black_Box bb in ((BlackBox_Open_Control)View).BB_List.SelectedItems)
                    {
                        bbList.Add(bb);
                    }
                });

                Manufacturing_Models model = Manufacturing_Models.NotDefined;
                foreach (BSA_Black_Box bb in bbList)
                {
                    if (model == Manufacturing_Models.NotDefined)
                    {
                        FS_Unit unit = null;
                        var units = Syteline_Units_Web_Service_Client_REST.Select_FS_Unit_SER_NUM_All(Token, bb.Unit_ID_Serial_Number);
                        if (units != null)
                        {
                            if (units.Count == 1)
                            {
                                unit = units[0];
                            }
                            else if (units.Count > 1)
                            {
                                foreach (var u in units)
                                {
                                    if (String.IsNullOrEmpty(u.Unit_Stat_Code) == true)
                                    {
                                        unit = u;
                                        break;
                                    }
                                    else if ("Expired|Scrapped".Contains(u.Unit_Stat_Code) == true) continue;
                                    else
                                    {
                                        unit = u;
                                        break;
                                    }
                                }
                            }
                        }

                        if ((units == null) || (units.Count == 0))
                        {
                            var plas = Manufacturing_PLA_Web_Service_Client_REST.Select_Production_Line_Assembly_SERIAL_NUMBER(Token, bb.Unit_ID_Serial_Number);
                            if ((plas == null) || (plas.Count == 0)) throw new Exception(String.Format("Unable to retrieve Unit information or Production_Line_Assembly information for serial: {0}", bb.Unit_ID_Serial_Number));
                            foreach (var pla in plas)
                            {
                                if ((pla.Start_Date.HasValue == true) && (pla.End_Date.HasValue == false))
                                {
                                    if (String.IsNullOrEmpty(pla.Model) == true) throw new Exception(String.Format("Model information for serial: {0} does not exist in Production_Line_Assembly", bb.Unit_ID_Serial_Number));

                                    if (pla.Model.Substring(3).ToUpper() == "I2") model = Manufacturing_Models.G2_I2;
                                    else if (pla.Model.Substring(3).ToUpper() == "X2") model = Manufacturing_Models.G2_X2;
                                    else throw new Exception(String.Format("Invalid model information ({1}) for serial: {0} is invalid in Production_Line_Assembly", bb.Unit_ID_Serial_Number, pla.Model));
                                }
                            }
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(unit.Item) == true) throw new Exception(String.Format("Item information is empty for serial: {0}", bb.Unit_ID_Serial_Number));

                            Items item = Syteline_Items_Web_Service_Client_REST.Select_Items_ITEM(Token, unit.Item);
                            if (item == null) throw new Exception(String.Format("Unable to retrieve Syteline Item: {0}", unit.Item));
                            if (String.IsNullOrEmpty(item.Charfld4) == true) throw new Exception(String.Format("Item: {0} does not have the flight code indicator", unit.Item));


                            if (item.Charfld4.ToUpper() == "I2") model = Manufacturing_Models.G2_I2;
                            if (item.Charfld4.ToUpper() == "X2") model = Manufacturing_Models.G2_X2;
                        }
                        if (model == Manufacturing_Models.NotDefined)
                        {
                            throw new Exception(String.Format("Unable to determine model for serial: {0}", bb.Unit_ID_Serial_Number));
                        }
                    }
                    Black_Box_Services_Graph.Report(pdf, bb, Token, model, new List<String>(Graph_Sels));
                }

                String path = Path.Combine(Application_Helper.Application_Folder_Name(), "Graphs", String.Format("{0}-{1} {2}.pdf", Selected_BlackBox.Unit_ID_Serial_Number,
                      Selected_BlackBox.Side, Selected_BlackBox.Date_Time_Entered.Value.ToString("yyyy-MM-dd HHmmss")));
                FileInfo fi = new FileInfo(path);
                if (fi.Directory.Exists == false) fi.Directory.Create();
                else if (fi.Exists == true) fi.Delete();
                pdf.Save(fi.FullName);
                ProcessHelper.Run(fi.FullName, redirectOutput: false, redirectError: false);
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
                ProcessingGraphs = false;
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
