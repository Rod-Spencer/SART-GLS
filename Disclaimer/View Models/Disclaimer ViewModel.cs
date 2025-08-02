using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Login.Objects;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.SART.Client.REST;
using System;
using System.Windows.Media.Imaging;

namespace Segway.Service.Disclaimer
{
    /// <summary>Public Class - Disclaimer_ViewModel</summary>
    public class Disclaimer_ViewModel : ViewModelBase, Disclaimer_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">Disclaimer_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="aggregator">IEventAggregator</param>
        public Disclaimer_ViewModel(Disclaimer_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator aggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = aggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<SART_Disclaimer_Accept_Navigate_Event>().Subscribe(SART_Disclaimer_Accept_Navigate_Handler, true);
            eventAggregator.GetEvent<SART_Disclaimer_Reject_Navigate_Event>().Subscribe(SART_Disclaimer_Reject_Navigate_Handler, true);

            #endregion

            #region Command Setups

            DisclaimerAcceptCommand = new DelegateCommand(CommandDisclaimerAccept, CanCommandDisclaimerAccept);
            DisclaimerRejectCommand = new DelegateCommand(CommandDisclaimerReject, CanCommandDisclaimerReject);

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

        #region Navigate_On_Reject

        private String _Navigate_On_Reject;

        /// <summary>Property Navigate_On_Reject of type String</summary>
        public String Navigate_On_Reject
        {
            get { return _Navigate_On_Reject; }
            set { _Navigate_On_Reject = value; }
        }

        #endregion


        #region Navigate_On_Accept

        private String _Navigate_On_Accept;

        /// <summary>Property Navigate_On_Accept of type String</summary>
        public String Navigate_On_Accept
        {
            get { return _Navigate_On_Accept; }
            set { _Navigate_On_Accept = value; }
        }

        #endregion


        #region Selected_Work_Order

        /// <summary>Property Selected_Work_Order of type SART_Work_Order</summary>
        public SART_Work_Order Selected_Work_Order
        {
            get { return InfrastructureModule.Current_Work_Order; }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        #region TitleImage

        private BitmapImage _TitleImage;

        /// <summary>Property TitleImage of type BitmapImage</summary>
        public BitmapImage TitleImage
        {
            get
            {
                if (_TitleImage == null) _TitleImage = Image_Helper.ImageFromEmbedded("TitleImage.png");
                return _TitleImage;
            }
            set
            {
                _TitleImage = value;
                OnPropertyChanged("TitleImage");
            }
        }

        #endregion

        #region DisclaimerImage

        private BitmapImage _DisclaimerImage;

        /// <summary>Property DisclaimerImage of type BitmapImage</summary>
        public BitmapImage DisclaimerImage
        {
            get
            {
                if (_DisclaimerImage == null) _DisclaimerImage = Image_Helper.ImageFromEmbedded("DisclaimerImage.png");
                return _DisclaimerImage;
            }
            set
            {
                _DisclaimerImage = value;
                OnPropertyChanged("DisclaimerImage");
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region DisclaimerAcceptCommand

        /// <summary>Delegate Command: DisclaimerAcceptCommand</summary>
        public DelegateCommand DisclaimerAcceptCommand { get; set; }
        private Boolean CanCommandDisclaimerAccept() { return true; }

        private void CommandDisclaimerAccept()
        {
            try
            {
                logger.Debug("Entered");
                if (Selected_Work_Order == null)
                {
                    if (String.IsNullOrEmpty(Navigate_On_Reject) == false)
                    {
                        regionManager.RequestNavigate(RegionNames.MainRegion, Navigate_On_Reject);
                    }
                    return;
                }

                SART_Disclaimer notice = new SART_Disclaimer(LoginContext.UserName, Selected_Work_Order.Work_Order_ID, Disclaimer_Statuses.Accepted);
                logger.Debug("Inserting disclaimer object: {0}", notice);
                notice = SART_DISCLAIMER_Web_Service_Client_REST.Insert_SART_Disclaimer_Key(InfrastructureModule.Token, notice);
                if (notice == null)
                {
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to record acceptance of disclaimer.");
                }
                else
                {
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Recorded acceptance of disclaimer.");
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
            }
            finally
            {
                eventAggregator.GetEvent<SART_Disclaimer_Accept_Event>().Publish("");
                if (String.IsNullOrEmpty(Navigate_On_Accept) == false)
                {
                    regionManager.RequestNavigate(RegionNames.MainRegion, Navigate_On_Accept);
                }
                logger.Debug("Leaving");
            }
        }

        #endregion

        #region DisclaimerRejectCommand

        /// <summary>Delegate Command: DisclaimerRejectCommand</summary>
        public DelegateCommand DisclaimerRejectCommand { get; set; }
        private Boolean CanCommandDisclaimerReject() { return true; }

        private void CommandDisclaimerReject()
        {
            logger.Debug("Entered");

            try
            {
                SART_Disclaimer notice = new SART_Disclaimer(LoginContext.UserName, Selected_Work_Order.Work_Order_ID, Disclaimer_Statuses.Rejected);
                logger.Debug("Inserting disclaimer object: {0}", notice);
                notice = SART_DISCLAIMER_Web_Service_Client_REST.Insert_SART_Disclaimer_Key(InfrastructureModule.Token, notice);
                if (notice == null)
                {
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to record rejection of disclaimer.");
                }
                else
                {
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Recorded rejection of disclaimer.");
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
            }
            finally
            {
                eventAggregator.GetEvent<SART_Disclaimer_Reject_Event>().Publish("");
                if (String.IsNullOrEmpty(Navigate_On_Reject) == false)
                {
                    regionManager.RequestNavigate(RegionNames.MainRegion, Navigate_On_Reject);
                }
                logger.Debug("Leaving");
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_Disclaimer_Reject_Navigate_Handler  -- Event: SART_Disclaimer_Reject_Navigate_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_Disclaimer_Reject_Navigate_Handler(String navigate)
        {
            Navigate_On_Reject = navigate;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_Disclaimer_Accept_Navigate_Handler  -- Event: SART_Disclaimer_Accept_Navigate_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_Disclaimer_Accept_Navigate_Handler(String navigate)
        {
            Navigate_On_Accept = navigate;
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
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

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
