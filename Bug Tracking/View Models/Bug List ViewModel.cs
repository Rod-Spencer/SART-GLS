using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.Login;
using Segway.Modules.ShellControls;
using Segway.Service.Authentication.Objects;
using Segway.Service.Bug.Objects;
using Segway.Service.Common;
using Segway.Service.Tools.BugZilla.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace Segway.Service.Bugs
{
    /// <summary>Public Class - Bug_List_ViewModel</summary>
    public class Bug_List_ViewModel : ViewModelBase, Bug_List_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">Bug_List_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Bug_List_ViewModel(Bug_List_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            //eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<Bug_Refresh_List_Event>().Subscribe(Bug_Refresh_List_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates

            OpenCommand = new DelegateCommand(CommandOpen, CanCommandOpen);
            NewCommand = new DelegateCommand(CommandNew, CanCommandNew);
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

        #region Bug_List

        private ObservableCollection<Bugs_Tracker> _Bug_List;

        /// <summary>Property Bug_List of type ObservableCollection&lt;Bugs_Tracker&gt;</summary>
        public ObservableCollection<Bugs_Tracker> Bug_List
        {
            get
            {
                if (_Bug_List == null) _Bug_List = new ObservableCollection<Bugs_Tracker>();
                return _Bug_List;
            }
            set
            {
                _Bug_List = value;
                OnPropertyChanged("Bug_List");
                OnPropertyChanged("Bug_Count");
            }
        }

        #endregion


        #region Selected_Bug

        private Bugs_Tracker _Selected_Bug;

        /// <summary>Property Selected_Bug of type Bugs_Tracker</summary>
        public Bugs_Tracker Selected_Bug
        {
            get { return _Selected_Bug; }
            set
            {
                _Selected_Bug = value;
                OnPropertyChanged("Selected_Bug");
                OpenCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion


        #region Bug_Count

        /// <summary>Property Bug_Count of type int</summary>
        public int Bug_Count
        {
            get { return Bug_List.Count; }
            set
            {
                OnPropertyChanged("Bug_Count");
            }
        }

        #endregion



        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers


        #region OpenCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: OpenCommand</summary>
        public DelegateCommand OpenCommand { get; set; }
        private Boolean CanCommandOpen()
        {
            return Selected_Bug != null;
        }

        private void CommandOpen()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                eventAggregator.GetEvent<Bug_Open_Event>().Publish(Selected_Bug);
                eventAggregator.GetEvent<Bug_Menu_Detail_Activate_Event>().Publish(true);
                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("TD");
                regionManager.RequestNavigate(RegionNames.MainRegion, Bug_Detail_Control.Control_Name);
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



        #region NewCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: NewCommand</summary>
        public DelegateCommand NewCommand { get; set; }
        private Boolean CanCommandNew() { return true; }
        private void CommandNew()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                eventAggregator.GetEvent<Bug_Open_Event>().Publish(new Bugs_Tracker()
                {
                    Status = "NEW",
                    Application = Application_Helper.GetConfigurationValue("ToolName")
                });
                eventAggregator.GetEvent<Bug_Menu_Detail_Activate_Event>().Publish(true);
                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("TD");
                regionManager.RequestNavigate(RegionNames.MainRegion, Bug_Detail_Control.Control_Name);
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



        #region RefreshCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: RefreshCommand</summary>
        public DelegateCommand RefreshCommand { get; set; }
        private Boolean CanCommandRefresh() { return true; }
        private void CommandRefresh()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Thread back = new Thread(new ThreadStart(CommandRefresh_Back));
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

        private void Application_Login_Handler(String obj)
        {
            _LoginContext = null;
            _Token = null;
            Bug_Refresh_List_Handler(true);
            eventAggregator.GetEvent<ToolBar_Clear_List_Active_Child_Event>().Publish("BTL");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Bug_Refresh_List_Handler  -- Event: Bug_Refresh_List_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Bug_Refresh_List_Handler(Boolean refresh)
        {
            List<String> tools = new List<String>();
            String tool = Application_Helper.GetConfigurationValue("ToolName");
            tools.Add(tool);
            if (tool == "SART Internal") tools.Add("Remote Service Tool");
            var bugs = BugZilla_Web_Service_Client.Select_BugZilla_Bugs_Tracker_LIST(Token, tools);

            Application.Current.Dispatcher.Invoke((Action)delegate ()
            {
                if ((bugs == null) || (bugs.Count == 0))
                {
                    Bug_List = new ObservableCollection<Bugs_Tracker>();
                    return;
                }
                bugs.Sort(new Bugs_Tracker_Priority_Comparer());

                if (tool == "SART Internal")
                {
                    Bug_List = new ObservableCollection<Bugs_Tracker>(bugs);
                }
                else
                {
                    Bug_List.Clear();
                    foreach (var bug in bugs)
                    {
                        if (bug.IS_Hidden.HasValue == false) Bug_List.Add(bug);
                        else if (bug.IS_Hidden.Value == false) Bug_List.Add(bug);
                    }
                    OnPropertyChanged("Bug_Count");
                }

                if (Bug_List.Count > 0)
                {
                    ((Bug_List_Control)View).lvBugList.ScrollIntoView(Bug_List[Bug_List.Count - 1]);
                }
            });
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
            //eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            //eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(false);
        }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);
            eventAggregator.GetEvent<Bug_Menu_Detail_Activate_Event>().Publish(true);
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private void CommandRefresh_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                Application_Login_Handler(null);
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
