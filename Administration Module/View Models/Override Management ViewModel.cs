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
using Segway.Service.SART2012.Client;
using System;
using System.Reflection;
using System.Windows.Media;



namespace Segway.Modules.Administration
{
    public class Override_Management_ViewModel : ViewModelBase, Override_Management_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        public Override_Management_ViewModel(Override_Management_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            #endregion

            #region Command Delegates

            OverrideClearCommand = new DelegateCommand(CommandOverrideClear, CanCommandOverrideClear);
            OverrideSetCommand = new DelegateCommand(CommandOverrideSet, CanCommandOverrideSet);
            ConfigSetCommand = new DelegateCommand(CommandConfigSet, CanCommandConfigSet);
            ConfigClearCommand = new DelegateCommand(CommandConfigClear, CanCommandConfigClear);

            #endregion
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties
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

        #region Override_WOID

        private String _Override_WOID;

        /// <summary>Property Override_WOID of type String</summary>
        public String Override_WOID
        {
            get { return _Override_WOID; }
            set
            {
                _Override_WOID = value;
                OnPropertyChanged("Override_WOID");
                OverrideSetCommand.RaiseCanExecuteChanged();
                OverrideClearCommand.RaiseCanExecuteChanged();
                ConfigSetCommand.RaiseCanExecuteChanged();
                ConfigClearCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Override_Reason

        private String _Override_Reason;

        /// <summary>Property OverrideReason of type String</summary>
        public String Override_Reason
        {
            get { return _Override_Reason; }
            set
            {
                _Override_Reason = value;
                OnPropertyChanged("Override_Reason");
                OverrideSetCommand.RaiseCanExecuteChanged();
                OverrideClearCommand.RaiseCanExecuteChanged();
                ConfigSetCommand.RaiseCanExecuteChanged();
                ConfigClearCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Override_Final_WOID

        private String _Override_Final_WOID;

        /// <summary>Property Override_Final_WOID of type String</summary>
        public String Override_Final_WOID
        {
            get { return _Override_Final_WOID; }
            set
            {
                _Override_Final_WOID = value;
                OnPropertyChanged("Override_Final_WOID");
                OverrideFinalSetCommand.RaiseCanExecuteChanged();
                OverrideFinalClearCommand.RaiseCanExecuteChanged();
                FinalConfigSetCommand.RaiseCanExecuteChanged();
                FinalConfigClearCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Override_Final_Reason

        private String _Override_Final_Reason;

        /// <summary>Property Override_Final_Reason of type String</summary>
        public String Override_Final_Reason
        {
            get { return _Override_Final_Reason; }
            set
            {
                _Override_Final_Reason = value;
                OnPropertyChanged("Override_Final_Reason");
                OverrideFinalSetCommand.RaiseCanExecuteChanged();
                OverrideFinalClearCommand.RaiseCanExecuteChanged();
                FinalConfigSetCommand.RaiseCanExecuteChanged();
                FinalConfigClearCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Force_Close_WOID

        private String _Force_Close_WOID;

        /// <summary>Property Force_Close_WOID of type String</summary>
        public String Force_Close_WOID
        {
            get { return _Force_Close_WOID; }
            set
            {
                _Force_Close_WOID = value;
                OnPropertyChanged("Force_Close_WOID");
                ForceCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Force_Close_Reason

        private String _Force_Close_Reason;

        /// <summary>Property Force_Close_Reason of type String</summary>
        public String Force_Close_Reason
        {
            get { return _Force_Close_Reason; }
            set
            {
                _Force_Close_Reason = value;
                OnPropertyChanged("Force_Close_Reason");
                ForceCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region OverrideClearCommand

        /// <summary>Delegate Command: OverrideClearCommand</summary>
        public DelegateCommand OverrideClearCommand { get; set; }


        private Boolean CanCommandOverrideClear()
        {
            return ((String.IsNullOrEmpty(Override_Reason) == false) && (String.IsNullOrEmpty(Override_WOID) == false));
        }

        private void CommandOverrideClear()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Update_Start_Config_Override(false);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
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


        #region OverrideSetCommand

        /// <summary>Delegate Command: OverrideSetCommand</summary>
        public DelegateCommand OverrideSetCommand { get; set; }


        private Boolean CanCommandOverrideSet()
        {
            return ((String.IsNullOrEmpty(Override_Reason) == false) && (String.IsNullOrEmpty(Override_WOID) == false));
        }

        private void CommandOverrideSet()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Update_Start_Config_Override(true);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
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


        #region ConfigSetCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ConfigSetCommand</summary>
        public DelegateCommand ConfigSetCommand { get; set; }
        private Boolean CanCommandConfigSet()
        {
            return ((String.IsNullOrEmpty(Override_Reason) == false) && (String.IsNullOrEmpty(Override_WOID) == false));
        }
        private void CommandConfigSet()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Update_Start_Configuration(true);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
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


        #region ConfigClearCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ConfigClearCommand</summary>
        public DelegateCommand ConfigClearCommand { get; set; }
        private Boolean CanCommandConfigClear()
        {
            return ((String.IsNullOrEmpty(Override_Reason) == false) && (String.IsNullOrEmpty(Override_WOID) == false));
        }
        private void CommandConfigClear()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Update_Start_Configuration(false);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
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


        #region OverrideFinalSetCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: OverrideFinalSetCommand</summary>
        public DelegateCommand OverrideFinalSetCommand { get; set; }
        private Boolean CanCommandOverrideFinalSet()
        {
            if (String.IsNullOrEmpty(Override_Final_WOID) == true) return false;
            if (String.IsNullOrEmpty(Override_Final_Reason) == true) return false;
            return true;
        }
        private void CommandOverrideFinalSet()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Update_Final_Config_Override(true);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }

            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
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


        #region OverrideFinalClearCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: OverrideFinalClearCommand</summary>
        public DelegateCommand OverrideFinalClearCommand { get; set; }
        private Boolean CanCommandOverrideFinalClear()
        {
            if (String.IsNullOrEmpty(Override_Final_WOID) == true) return false;
            if (String.IsNullOrEmpty(Override_Final_Reason) == true) return false;
            return true;
        }
        private void CommandOverrideFinalClear()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Update_Final_Config_Override(false);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
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


        #region FinalConfigSetCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: FinalConfigSetCommand</summary>
        public DelegateCommand FinalConfigSetCommand { get; set; }
        private Boolean CanCommandFinalConfigSet()
        {
            if (String.IsNullOrEmpty(Override_Final_WOID) == true) return false;
            if (String.IsNullOrEmpty(Override_Final_Reason) == true) return false;
            return true;
        }

        private void CommandFinalConfigSet()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Update_Final_Configuration(true);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
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


        #region FinalConfigClearCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: FinalConfigClearCommand</summary>
        public DelegateCommand FinalConfigClearCommand { get; set; }
        private Boolean CanCommandFinalConfigClear()
        {
            if (String.IsNullOrEmpty(Override_Final_WOID) == true) return false;
            if (String.IsNullOrEmpty(Override_Final_Reason) == true) return false;
            return true;
        }
        private void CommandFinalConfigClear()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Update_Final_Configuration(false);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
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


        #region ForceCloseCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: ForceCloseCommand</summary>
        public DelegateCommand ForceCloseCommand { get; set; }
        private Boolean CanCommandForceClose()
        {
            if (String.IsNullOrEmpty(Force_Close_Reason)) return false;
            if (String.IsNullOrEmpty(Force_Close_WOID)) return false;

            return true;
        }
        private void CommandForceClose()
        {
            logger.Trace("Entered");
            try
            {
                if ((InfrastructureModule.Current_Work_Order != null) && (InfrastructureModule.Current_Work_Order.Work_Order_ID == Force_Close_WOID))
                {
                    PopupMessage = String.Format("The work order: '{0}' is currenly opened by you. Please close the work order or chose a different work order.", Force_Close_WOID);
                    PopupOpen = true;
                    PopupColor = Brushes.LightGoldenrodYellow;
                    return;
                }
                logger.Debug("Closing Work Order: {0}", Force_Close_WOID);
                if (SART_2012_Web_Service_Client.Close_Work_Order(InfrastructureModule.Token, Force_Close_WOID) == false)
                {
                    PopupMessage = String.Format("The work order: '{0}' is invalid", Force_Close_WOID);
                    PopupOpen = true;
                    PopupColor = Brushes.Pink;
                    return;
                }


                if (SART_Common.Create_Event(WorkOrder_Events.Force_Closed_Work_Order, Event_Statuses.Passed, 0, Force_Close_Reason, Force_Close_WOID) == false)
                {
                    PopupMessage = String.Format("The work order: '{0}' was closed successfully.  However an error occurred while creating the event", Force_Close_WOID);
                    PopupOpen = true;
                    PopupColor = Brushes.LightGoldenrodYellow;
                    return;
                }

                PopupMessage = String.Format("The work order: '{0}' has been closed", Force_Close_WOID);
                PopupOpen = true;
                PopupColor = Brushes.LightGreen;
                return;
            }
            catch (Authentication_Exception ae)
            {
                //PopupMessage = "Login session has expired";
                //PopupOpen = true;
                //PopupColor = Brushes.Pink;
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
                return;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                PopupMessage = String.Format("The following error occurred: {0}", ex.Message);
                PopupColor = Brushes.Pink;
                PopupOpen = true;
            }
            finally
            {
                logger.Trace("Leaving");
                Force_Close_WOID = null;
                Force_Close_Reason = null;
            }
        }

        /////////////////////////////////////////////
        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers
        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        public bool IsNavigationTarget(NavigationContext navigationContext) { return true; }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            //eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(false);

            Override_WOID = "";
            Override_Reason = "";
            Override_Final_WOID = "";
            Override_Final_Reason = "";

        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private void Update_Start_Config_Override(Boolean set)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                String woID = Override_WOID;
                SART_Work_Order wo = SART_2012_Web_Service_Client.Select_SART_Work_Order_WORK_ORDER_ID(InfrastructureModule.Token, woID);
                if (wo == null)
                {
                    PopupMessage = String.Format("An error occurred while retrieving Work Order: {0}.  Please contact Segway Support.", woID);
                    PopupColor = Brushes.Pink;
                    PopupOpen = true;
                    return;
                }

                if (String.IsNullOrEmpty(wo.Opened_By) == false)  //Work_Order_WorkingStatuses.Opened.ToString())
                {
                    PopupMessage = String.Format("Start Configuration Override could not be {1} because the work order is currently open.\nPlease have {0} close the SART application and try again.", wo.Opened_By, set ? "set" : "cleared");
                    PopupColor = Brushes.LightGoldenrodYellow;
                    PopupOpen = true;
                    return;
                }

                wo.Is_Start_Config_Override = set;
                if (SART_2012_Web_Service_Client.Update_SART_Work_Order_Key(InfrastructureModule.Token, wo) == false)
                {
                    PopupMessage = String.Format("An error occurred while updating Work Order: {0}.  Please contact Segway Support.", woID);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("An error occurred while updating Work Order: {0}", woID));
                    PopupColor = Brushes.Pink;
                    PopupOpen = true;
                    return;
                }

                Boolean result = SART_Common.Create_Event(set ? WorkOrder_Events.Set_StartConfiguration_Override : WorkOrder_Events.Clear_StartConfiguration_Override, Event_Statuses.Passed, 0, Override_Reason, woID);
                if (result == false)
                {
                    PopupMessage = String.Format("The Start Configuration Override for Work Order: {0} has been {1}.\n\nHowever an error occurred while trying to create an event.", woID, set ? "set" : "cleared");
                    PopupColor = Brushes.LightGoldenrodYellow;
                    PopupOpen = true;
                    woID = "";
                    Override_Reason = "";
                    return;
                }

                eventAggregator.GetEvent<WorkOrder_Clear_List_Event>().Publish(true);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("{1} Start Configuration Override for Work Order: {0}", woID, set ? "Set" : "Cleared"));
                PopupMessage = String.Format("Start Configuration Override was {1} for work order {0}", woID, set ? "set" : "cleared");
                PopupColor = Brushes.LightGreen;
                PopupOpen = true;
                Override_WOID = "";
                Override_Reason = "";
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
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
        }

        private void Update_Final_Config_Override(Boolean set)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                String woID = Override_Final_WOID;
                SART_Work_Order wo = SART_2012_Web_Service_Client.Select_SART_Work_Order_WORK_ORDER_ID(InfrastructureModule.Token, woID);
                if (wo == null)
                {
                    PopupMessage = String.Format("An error occurred while retrieving Work Order: {0}.  Please contact Segway Support.", woID);
                    PopupColor = Brushes.Pink;
                    PopupOpen = true;
                    return;
                }

                if (String.IsNullOrEmpty(wo.Opened_By) == false)  //Work_Order_WorkingStatuses.Opened.ToString())
                {
                    PopupMessage = String.Format("Final Configuration Override could not be {1} because the work order is currently open.\nPlease have {0} close the SART application and try again.", wo.Opened_By, set ? "set" : "cleared");
                    PopupColor = Brushes.LightGoldenrodYellow;
                    PopupOpen = true;
                    return;
                }

                wo.Is_Final_Config_Override = set;
                if (SART_2012_Web_Service_Client.Update_SART_Work_Order_Key(InfrastructureModule.Token, wo) == false)
                {
                    PopupMessage = String.Format("An error occurred while updating Work Order: {0}.  Please contact Segway Support.", woID);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("An error occurred while updating Work Order: {0}", woID));
                    PopupColor = Brushes.Pink;
                    PopupOpen = true;
                    return;
                }

                Boolean result = SART_Common.Create_Event(set ? WorkOrder_Events.Set_FinalConfiguration_Override : WorkOrder_Events.Clear_FinalConfiguration_Override, Event_Statuses.Passed, 0, Override_Final_Reason, woID);
                if (result == false)
                {
                    PopupMessage = String.Format("The Final Configuration Override for Work Order: {0} has been {1}.\n\nHowever an error occurred while trying to create an event.", woID, set ? "set" : "cleared");
                    PopupColor = Brushes.LightGoldenrodYellow;
                    PopupOpen = true;
                    woID = "";
                    Override_Final_Reason = "";
                    return;
                }

                eventAggregator.GetEvent<WorkOrder_Clear_List_Event>().Publish(true);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("{1} Final Configuration Override for Work Order: {0}", woID, set ? "Set" : "Cleared"));
                PopupMessage = String.Format("Final Configuration Override was {1} for work order {0}", woID, set ? "set" : "cleared");
                PopupColor = Brushes.LightGreen;
                PopupOpen = true;
                Override_Final_WOID = "";
                Override_Final_Reason = "";
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
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
        }

        private void Update_Start_Configuration(Boolean set)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                String woID = Override_WOID;
                SART_Work_Order wo = SART_2012_Web_Service_Client.Select_SART_Work_Order_WORK_ORDER_ID(InfrastructureModule.Token, woID);
                if (wo == null)
                {
                    PopupMessage = String.Format("An error occurred while retrieving Work Order: {0}.  Please contact Segway Support.", woID);
                    PopupColor = Brushes.Pink;
                    PopupOpen = true;
                    return;
                }

                if (String.IsNullOrEmpty(wo.Opened_By) == false)  //Work_Order_WorkingStatuses.Opened.ToString())
                {
                    PopupMessage = String.Format("Start Configuration could not be {1} because the work order is currently open.\nPlease have {0} close the SART application and try again.", wo.Opened_By, set ? "set" : "cleared");
                    PopupColor = Brushes.LightGoldenrodYellow;
                    PopupOpen = true;
                    return;
                }

                wo.Is_Start_Config = set == false ? (Boolean?)null : true;
                if (SART_2012_Web_Service_Client.Update_SART_Work_Order_Key(InfrastructureModule.Token, wo) == false)
                {
                    PopupMessage = String.Format("An error occurred while updating Work Order: {0}.  Please contact Segway Support.", woID);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("An error occurred while updating Work Order: {0}", woID));
                    PopupColor = Brushes.Pink;
                    PopupOpen = true;
                    return;
                }


                Boolean result = SART_Common.Create_Event(set ? WorkOrder_Events.Set_StartConfiguration : WorkOrder_Events.Clear_StartConfiguration, Event_Statuses.Passed, 0, Override_Reason, woID);
                if (result == false)
                {
                    PopupMessage = String.Format("The Start Configuration for Work Order: {0} has been {1}.\n\nHowever an error occurred while trying to create an event.", woID, set ? "set" : "cleared");
                    PopupColor = Brushes.LightGoldenrodYellow;
                    PopupOpen = true;
                    woID = "";
                    Override_Reason = "";
                    return;
                }

                eventAggregator.GetEvent<WorkOrder_Clear_List_Event>().Publish(true);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("{1} Start Configuration for Work Order: {0}", woID, set ? "Set" : "Cleared"));
                PopupMessage = String.Format("Start Configuration was {1} for work order {0}", woID, set ? "set" : "cleared");
                PopupColor = Brushes.LightGreen;
                PopupOpen = true;
                Override_WOID = "";
                Override_Reason = "";
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
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
        }

        private void Update_Final_Configuration(Boolean set)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                String woID = Override_Final_WOID;
                SART_Work_Order wo = SART_2012_Web_Service_Client.Select_SART_Work_Order_WORK_ORDER_ID(InfrastructureModule.Token, woID);
                if (wo == null)
                {
                    PopupMessage = String.Format("An error occurred while retrieving Work Order: {0}.  Please contact Segway Support.", woID);
                    PopupColor = Brushes.Pink;
                    PopupOpen = true;
                    return;
                }

                if (String.IsNullOrEmpty(wo.Opened_By) == false)  //Work_Order_WorkingStatuses.Opened.ToString())
                {
                    PopupMessage = String.Format("Final Configuration could not be {1} because the work order is currently open.\nPlease have {0} close the SART application and try again.", wo.Opened_By, set ? "set" : "cleared");
                    PopupColor = Brushes.LightGoldenrodYellow;
                    PopupOpen = true;
                    return;
                }

                wo.Is_Final_Config = set == false ? (Boolean?)null : true;
                if (SART_2012_Web_Service_Client.Update_SART_Work_Order_Key(InfrastructureModule.Token, wo) == false)
                {
                    PopupMessage = String.Format("An error occurred while updating Work Order: {0}.  Please contact Segway Support.", woID);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("An error occurred while updating Work Order: {0}", woID));
                    PopupColor = Brushes.Pink;
                    PopupOpen = true;
                    return;
                }


                Boolean result = SART_Common.Create_Event(set ? WorkOrder_Events.Set_FinalConfiguration : WorkOrder_Events.Clear_FinalConfiguration, Event_Statuses.Passed, 0, Override_Final_Reason, woID);
                if (result == false)
                {
                    PopupMessage = String.Format("The Final Configuration for Work Order: {0} has been {1}.\n\nHowever an error occurred while trying to create an event.", woID, set ? "set" : "cleared");
                    PopupColor = Brushes.LightGoldenrodYellow;
                    PopupOpen = true;
                    woID = "";
                    Override_Final_Reason = "";
                    return;
                }

                eventAggregator.GetEvent<WorkOrder_Clear_List_Event>().Publish(true);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("{1} Final Configuration for Work Order: {0}", woID, set ? "Set" : "Cleared"));
                PopupMessage = String.Format("Final Configuration was {1} for work order {0}", woID, set ? "set" : "cleared");
                PopupColor = Brushes.LightGreen;
                PopupOpen = true;
                Override_Final_WOID = "";
                Override_Final_Reason = "";
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
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
        }



        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
