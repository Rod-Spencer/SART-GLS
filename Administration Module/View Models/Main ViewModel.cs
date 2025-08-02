using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.Login;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.Controls.StatusBars;
using Segway.Service.DatabaseHelper;
using Segway.Service.Modules.AddWindow;
using Segway.Service.Objects;
using Segway.Service.SART2012.Client;
using Segway.Syteline.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows.Media;

namespace Segway.Modules.Administration
{
    /// <summary>Public Class</summary>
    public class Main_ViewModel : ViewModelBase, Main_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator aggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">Main_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Main_ViewModel(Main_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.aggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Shell_Close_Event>().Subscribe(Application_Closing_Handler, true);
            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates


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


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        //#region Access

        ///// <summary>Property Access of type String</summary>
        //public String Access
        //{
        //    get
        //    {
        //        if (Selected_User == null) return null;
        //        if (AccessList.ContainsKey(Selected_User) == false) return null;
        //        return AccessList[Selected_User].Tool_Name;
        //    }
        //    set { OnPropertyChanged("Access"); }
        //}

        //#endregion



        #region WorkOrder_Open

        private Boolean _WorkOrder_Open;

        /// <summary>Property Override_Popup_Open of type Boolean</summary>
        public Boolean WorkOrder_Open
        {
            get { return _WorkOrder_Open; }
            set
            {
                _WorkOrder_Open = value;
                OnPropertyChanged("WorkOrder_Open");
            }
        }

        #endregion

        #region WorkOrder_Message

        private String _WorkOrder_Message;

        /// <summary>Property Override_Message of type String</summary>
        public String WorkOrder_Message
        {
            get { return _WorkOrder_Message; }
            set
            {
                _WorkOrder_Message = value;
                OnPropertyChanged("WorkOrder_Message");
            }
        }

        #endregion

        #region WorkOrder_Color

        private Brush _WorkOrder_Color = Brushes.LightGoldenrodYellow;

        /// <summary>Property Override_Color of type Brush</summary>
        public Brush WorkOrder_Color
        {
            get { return _WorkOrder_Color; }
            set
            {
                _WorkOrder_Color = value;
                OnPropertyChanged("WorkOrder_Color");
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






        #region Popup User Settings Controls

        #region PopupUserSettingsMessage

        private String _PopupUserSettingsMessage;

        /// <summary>ViewModel Property: PopupUserSettingsMessage of type: String</summary>
        public String PopupUserSettingsMessage
        {
            get { return _PopupUserSettingsMessage; }
            set
            {
                _PopupUserSettingsMessage = value;
                OnPropertyChanged("PopupUserSettingsMessage");
            }
        }

        #endregion

        #region PopupUserSettingsOpen

        private Boolean _PopupUserSettingsOpen;

        /// <summary>ViewModel Property: PopupUserSettingsOpen of type: Boolean</summary>
        public Boolean PopupUserSettingsOpen
        {
            get { return _PopupUserSettingsOpen; }
            set
            {
                _PopupUserSettingsOpen = value;
                OnPropertyChanged("PopupUserSettingsOpen");
            }
        }

        #endregion

        #region PopupUserSettingsColor

        private Brush _PopupUserSettingsColor;

        /// <summary>ViewModel Property: PopupUserSettingsColor of type: Brush</summary>
        public Brush PopupUserSettingsColor
        {
            get { return _PopupUserSettingsColor; }
            set
            {
                _PopupUserSettingsColor = value;
                OnPropertyChanged("PopupUserSettingsColor");
            }
        }

        #endregion

        #endregion


        #region User / Dealer Management Controls




        #endregion

        #region SART Settings



        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers





        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Closing_Handler  -- Close_Shell_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Closing_Handler(String msg)
        {

        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        private void Updated_DealerList(Boolean clear)
        {
            //InfrastructureModule.Clear_DealerList();
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Login_Handler  -- Event: Application_Login_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Login_Handler(String login)
        {
            if (Models != null)
            {
                List<String> partnumbers = new List<String>(Models.Keys);
                partnumbers.Sort();
                ChngModelNewPartNumberList = new ObservableCollection<String>(partnumbers);
            }
            Settings_List = null;
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
            aggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Administration", "Main_Control"));
        }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                aggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
                aggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);
                aggregator.GetEvent<ToolBar_Selection_Event>().Publish("Administration");
                OnPropertyChanged("Part_List");
                OnPropertyChanged("User_List");
                OnPropertyChanged("User_Settings");

                OnPropertyChanged("Settings_List");
                OnPropertyChanged("Selected_Settings");
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

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods





        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
