using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Login.Objects;
using Segway.Modules.ShellControls;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.SART.Client.REST;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Media;

namespace Segway.Modules.Administration
{
    /// <summary>Public Class - Settings_ViewModel</summary>
    public class Settings_ViewModel : ViewModelBase, Settings_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">Settings_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Settings_ViewModel(Settings_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates

            SaveSARTSettingsCommand = new DelegateCommand(CommandSaveSARTSettings, CanCommandSaveSARTSettings);
            LoadSARTSettingsCommand = new DelegateCommand(CommandLoadSARTSettings, CanCommandLoadSARTSettings);
            AddSARTSettingsCommand = new DelegateCommand(CommandAddSARTSettings, CanCommandAddSARTSettings);
            DelSARTSettingsCommand = new DelegateCommand(CommandDelSARTSettings, CanCommandDelSARTSettings);
            CopySARTSettingsCommand = new DelegateCommand(CommandCopySARTSettings, CanCommandCopySARTSettings);

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

        #region Popup SART Settings Controls

        #region Popup_SART_Settings_Message

        private String _Popup_SART_Settings_Message;

        /// <summary>ViewModel Property: Popup_SART_Settings_Message of type: String</summary>
        public String Popup_SART_Settings_Message
        {
            get { return _Popup_SART_Settings_Message; }
            set
            {
                _Popup_SART_Settings_Message = value;
                OnPropertyChanged("Popup_SART_Settings_Message");
            }
        }

        #endregion

        #region Popup SART Settings Open

        private Boolean _Popup_SART_Settings_Open;

        /// <summary>ViewModel Property: Popup_SART_Settings_Open of type: Boolean</summary>
        public Boolean Popup_SART_Settings_Open
        {
            get { return _Popup_SART_Settings_Open; }
            set
            {
                _Popup_SART_Settings_Open = value;
                OnPropertyChanged("Popup_SART_Settings_Open");
            }
        }

        #endregion

        #region Popup SART Settings Color

        private Brush _Popup_SART_Settings_Color;

        /// <summary>ViewModel Property: Popup_SART_Settings_Color of type: Brush</summary>
        public Brush Popup_SART_Settings_Color
        {
            get { return _Popup_SART_Settings_Color; }
            set
            {
                _Popup_SART_Settings_Color = value;
                OnPropertyChanged("Popup_SART_Settings_Color");
            }
        }

        #endregion

        #endregion


        #region Settings_List

        private ObservableCollection<SART_Settings> _Settings_List;

        /// <summary>Property Settings_List of type ObservableCollection&lt;SART_Settings&gt;</summary>
        public ObservableCollection<SART_Settings> Settings_List
        {
            get
            {
                if (_Settings_List == null)
                {
                    if (Token != null)
                    {
                        var settings = SART_Settings_Web_Service_Client_REST.Select_SART_Settings_All(Token);
                        if (settings != null) _Settings_List = new ObservableCollection<SART_Settings>(settings);
                    }
                }
                return _Settings_List;
            }
            set
            {
                _Settings_List = value;
                OnPropertyChanged("Settings_List");
            }
        }

        #endregion


        #region Selected_Settings

        private SART_Settings _Selected_Settings;

        /// <summary>Property Selected_Settings of type SART_Settings</summary>
        public SART_Settings Selected_Settings
        {
            get { return _Selected_Settings; }
            set
            {
                _Selected_Settings = value;
                if (value != null) eventAggregator.GetEvent<Admin_Selected_Settings_Name_Event>().Publish(value.Name);
                else eventAggregator.GetEvent<Admin_Selected_Settings_Name_Event>().Publish(null);
                OnPropertyChanged("Selected_Settings");
                SaveSARTSettingsCommand.RaiseCanExecuteChanged();
                DelSARTSettingsCommand.RaiseCanExecuteChanged();
                CopySARTSettingsCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion



        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region CopySARTSettingsCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: CopySARTSettingsCommand</summary>
        public DelegateCommand CopySARTSettingsCommand { get; set; }
        private Boolean CanCommandCopySARTSettings()
        {
            return Selected_Settings != null;
        }
        private void CommandCopySARTSettings()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Admin_Selected_Settings_Name_Event>().Publish(Selected_Settings.Name);
                //Settings_Name = Selected_Settings.Name;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Popup_SART_Settings_Color = Brushes.Pink;
                Popup_SART_Settings_Message = msg;
                Popup_SART_Settings_Open = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region SaveSARTSettingsCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: SaveSARTSettingsCommand</summary>
        public DelegateCommand SaveSARTSettingsCommand { get; set; }
        private Boolean CanCommandSaveSARTSettings() { return Selected_Settings != null; }
        private void CommandSaveSARTSettings()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (SART_Settings_Web_Service_Client_REST.Update_SART_Settings_Object(Token, Selected_Settings) == true)
                {
                    String msg = "SART Setting was successfully updated";
                    logger.Debug(msg);
                    Popup_SART_Settings_Color = Brushes.LightGreen;
                    Popup_SART_Settings_Message = msg;
                    Popup_SART_Settings_Open = true;
                    eventAggregator.GetEvent<Admin_Settings_Changed_Event>().Publish(true);
                }
                else
                {
                    String msg = "An error occurred in trying to save the selected SART Setting.\r\n\r\nPlease try again.\r\n\r\nIf the error persists, please contact Segway Technical Support.";
                    logger.Warn(msg);
                    Popup_SART_Settings_Color = Brushes.LightGoldenrodYellow;
                    Popup_SART_Settings_Message = msg;
                    Popup_SART_Settings_Open = true;
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Popup_SART_Settings_Color = Brushes.Pink;
                Popup_SART_Settings_Message = msg;
                Popup_SART_Settings_Open = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region AddSARTSettingsCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: AddSARTSettingsCommand</summary>
        public DelegateCommand AddSARTSettingsCommand { get; set; }
        private Boolean CanCommandAddSARTSettings() { return true; }
        private void CommandAddSARTSettings()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SART_Settings setting = new SART_Settings();
                setting.Delay_Wake_Start = 2000;
                setting.Timeout_Start_Applet = 20;
                setting.Timeout_Start_Applet_Response = 2000;
                setting = SART_Settings_Web_Service_Client_REST.Insert_SART_Settings_Key(Token, setting);
                if (setting != null)
                {
                    Settings_List.Add(setting);
                    Selected_Settings = setting;
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Popup_SART_Settings_Color = Brushes.Pink;
                Popup_SART_Settings_Message = msg;
                Popup_SART_Settings_Open = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region LoadSARTSettingsCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: LoadSARTSettingsCommand</summary>
        public DelegateCommand LoadSARTSettingsCommand { get; set; }
        private Boolean CanCommandLoadSARTSettings() { return true; }
        private void CommandLoadSARTSettings()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Settings_List = null;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Popup_SART_Settings_Color = Brushes.Pink;
                Popup_SART_Settings_Message = msg;
                Popup_SART_Settings_Open = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region DelSARTSettingsCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: DelSARTSettingsCommand</summary>
        public DelegateCommand DelSARTSettingsCommand { get; set; }
        private Boolean CanCommandDelSARTSettings() { return Selected_Settings != null; }
        private void CommandDelSARTSettings()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SART_Settings setting = Selected_Settings;
                Settings_List.Remove(Selected_Settings);
                if (SART_Settings_Web_Service_Client_REST.Delete_SART_Settings_Key(Token, setting.ID) == true)
                {
                    String msg = "The selected setting has been deleted";
                    logger.Debug(msg);
                    Popup_SART_Settings_Color = Brushes.LightGreen;
                    Popup_SART_Settings_Message = msg;
                    Popup_SART_Settings_Open = true;
                }
                else
                {
                    String msg = "An error occurred trying to delete the selected setting";
                    logger.Warn(msg);
                    Popup_SART_Settings_Color = Brushes.LightGoldenrodYellow;
                    Popup_SART_Settings_Message = msg;
                    Popup_SART_Settings_Open = true;
                    CommandLoadSARTSettings();
                }
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                Popup_SART_Settings_Color = Brushes.Pink;
                Popup_SART_Settings_Message = msg;
                Popup_SART_Settings_Open = true;
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
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods
        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
