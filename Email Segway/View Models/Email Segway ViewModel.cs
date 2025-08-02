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
using Segway.Service.ExceptionHelper;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;



namespace Segway.Service.SART.Email
{
    /// <summary>Public Class - Email_Segway_ViewModel</summary>
    public class Email_Segway_ViewModel : ViewModelBase, Email_Segway_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">EmailSegway_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Email_Segway_ViewModel(EmailSegway_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, true);
            eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, ThreadOption.UIThread, true);

            #endregion

            #region Command Setups

            SubmitCommand = new DelegateCommand(CommandSubmit, CanCommandSubmit);

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


        #region Work_Order

        private String _Work_Order;

        /// <summary>Property Work_Order of type String</summary>
        public String Work_Order
        {
            get { return _Work_Order; }
            set
            {
                _Work_Order = value;
                OnPropertyChanged("Work_Order");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region PT_Serial

        private String _PT_Serial;

        /// <summary>Property PT_Serial of type String</summary>
        public String PT_Serial
        {
            get { return _PT_Serial; }
            set
            {
                _PT_Serial = value;
                OnPropertyChanged("PT_Serial");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Technician

        private String _Technician;

        /// <summary>Property Technician of type String</summary>
        public String Technician
        {
            get
            {
                if (String.IsNullOrEmpty(_Technician) == true)
                {
                    if (LoginContext != null)
                    {
                        _Technician = LoginContext.UserName;
                    }
                }
                return _Technician;
            }
            set
            {
                _Technician = value;
                OnPropertyChanged("Technician");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region ErrorCodes

        private String _ErrorCodes;

        /// <summary>Property ErrorCodes of type String</summary>
        public String ErrorCodes
        {
            get { return _ErrorCodes; }
            set
            {
                _ErrorCodes = value;
                OnPropertyChanged("ErrorCodes");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region CU - A

        #region IsCUA_Yes_Checked

        private Boolean _IsCUA_Yes_Checked;

        /// <summary>Property IsCUA_Yes_Checked of type Boolean</summary>
        public Boolean IsCUA_Yes_Checked
        {
            get { return _IsCUA_Yes_Checked; }
            set
            {
                _IsCUA_Yes_Checked = value;
                OnPropertyChanged("IsCUA_Yes_Checked");
                OnPropertyChanged("CUA_Visibility");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region IsCUA_No_Checked

        private Boolean _IsCUA_No_Checked;

        /// <summary>Property IsCUA_No_Checked of type Boolean</summary>
        public Boolean IsCUA_No_Checked
        {
            get { return _IsCUA_No_Checked; }
            set
            {
                _IsCUA_No_Checked = value;
                OnPropertyChanged("IsCUA_No_Checked");
                OnPropertyChanged("CUA_Visibility");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region CUA_Reason

        private String _CUA_Reason;

        /// <summary>Property CUA_Reason of type String</summary>
        public String CUA_Reason
        {
            get { return _CUA_Reason; }
            set
            {
                _CUA_Reason = value;
                OnPropertyChanged("CUA_Reason");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region CUA_Visibility

        /// <summary>Property CUA_Visibility of type Visibility</summary>
        public Visibility CUA_Visibility
        {
            get
            {
                if (IsCUA_No_Checked == false) return Visibility.Collapsed;
                return Visibility.Visible;
            }
            set { OnPropertyChanged("CUA_Visibility"); }
        }

        #endregion

        #endregion

        #region CU - B

        #region IsCUB_Yes_Checked

        private Boolean _IsCUB_Yes_Checked;

        /// <summary>Property IsCUB_Yes_Checked of type Boolean</summary>
        public Boolean IsCUB_Yes_Checked
        {
            get { return _IsCUB_Yes_Checked; }
            set
            {
                _IsCUB_Yes_Checked = value;
                OnPropertyChanged("IsCUB_Yes_Checked");
                OnPropertyChanged("CUB_Visibility");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region IsCUB_No_Checked

        private Boolean _IsCUB_No_Checked;

        /// <summary>Property IsCUB_No_Checked of type Boolean</summary>
        public Boolean IsCUB_No_Checked
        {
            get { return _IsCUB_No_Checked; }
            set
            {
                _IsCUB_No_Checked = value;
                OnPropertyChanged("IsCUB_No_Checked");
                OnPropertyChanged("CUB_Visibility");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region CUB_Visibility

        /// <summary>Property CUB_Visibility of type Visibility</summary>
        public Visibility CUB_Visibility
        {
            get
            {
                if (IsCUB_No_Checked == false) return Visibility.Collapsed;
                return Visibility.Visible;
            }
            set { OnPropertyChanged("CUB_Visibility"); }
        }

        #endregion

        #region CUB_Reason

        private String _CUB_Reason;

        /// <summary>Property CUB_Reason of type String</summary>
        public String CUB_Reason
        {
            get { return _CUB_Reason; }
            set
            {
                _CUB_Reason = value;
                OnPropertyChanged("CUB_Reason");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion

        #region Start Configuration

        #region IsCnf_Yes_Checked

        private Boolean _IsCnf_Yes_Checked;

        /// <summary>Property IsCnf_Yes_Checked of type Boolean</summary>
        public Boolean IsCnf_Yes_Checked
        {
            get { return _IsCnf_Yes_Checked; }
            set
            {
                _IsCnf_Yes_Checked = value;
                OnPropertyChanged("IsCnf_Yes_Checked");
                OnPropertyChanged("Cnf_Visibility");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region IsCnf_No_Checked

        private Boolean _IsCnf_No_Checked;

        /// <summary>Property IsCnf_No_Checked of type Boolean</summary>
        public Boolean IsCnf_No_Checked
        {
            get { return _IsCnf_No_Checked; }
            set
            {
                _IsCnf_No_Checked = value;
                OnPropertyChanged("IsCnf_No_Checked");
                OnPropertyChanged("Cnf_Visibility");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Cnf_Visibility

        /// <summary>Property Cnf_Visibility of type Visibility</summary>
        public Visibility Cnf_Visibility
        {
            get
            {
                if (IsCnf_No_Checked == false) return Visibility.Collapsed;
                return Visibility.Visible;
            }
            set { OnPropertyChanged("Cnf_Visibility"); }
        }

        #endregion

        #region Cnf_Reason

        private String _Cnf_Reason;

        /// <summary>Property Cnf_Reason of type String</summary>
        public String Cnf_Reason
        {
            get { return _Cnf_Reason; }
            set
            {
                _Cnf_Reason = value;
                OnPropertyChanged("Cnf_Reason");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion

        #region Email_Address

        private String _Email_Address;

        /// <summary>Property Email_Address of type String</summary>
        public String Email_Address
        {
            get
            {
                if (LoginContext == null) return null;
                if (String.IsNullOrEmpty(_Email_Address) == true) _Email_Address = LoginContext.Email;
                return _Email_Address;
            }
            set
            {
                _Email_Address = value;
                OnPropertyChanged("Email_Address");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Comments

        private String _Comments;

        /// <summary>Property Comments of type String</summary>
        public String Comments
        {
            get { return _Comments; }
            set
            {
                _Comments = value;
                OnPropertyChanged("Comments");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region SubmitCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: SubmitCommand</summary>
        public DelegateCommand SubmitCommand { get; set; }
        private Boolean CanCommandSubmit()
        {
            if (String.IsNullOrEmpty(Work_Order) == true) return false;
            if (String.IsNullOrEmpty(Technician) == true) return false;
            if ((IsCUA_Yes_Checked ^ IsCUA_No_Checked) == false) return false;
            if (IsCUA_No_Checked == true)
            {
                if (String.IsNullOrEmpty(CUA_Reason) == true) return false;
            }

            if ((IsCUB_Yes_Checked ^ IsCUB_No_Checked) == false) return false;
            if (IsCUB_No_Checked == true)
            {
                if (String.IsNullOrEmpty(CUB_Reason) == true) return false;
            }

            if ((IsCnf_Yes_Checked ^ IsCnf_No_Checked) == false) return false;
            if (IsCnf_No_Checked == true)
            {
                if (String.IsNullOrEmpty(Cnf_Reason) == true) return false;
            }

            if (String.IsNullOrEmpty(Email_Address) == true) return false;

            if (String.IsNullOrEmpty(Comments) == true) return false;

            return true;
        }


        private void CommandSubmit()
        {
            try
            {
                logger.Debug("Entered");
                Email_Data ed = new Email_Data();
                ed.Cnf_Reason = Cnf_Reason;
                ed.Codes = ErrorCodes;
                ed.Comments = Comments;
                ed.CUA_Reason = CUA_Reason;
                ed.CUB_Reason = CUB_Reason;
                ed.Dealer_ID = LoginContext.Customer_ID;
                ed.From = Email_Address;
                ed.PT_Serial = PT_Serial;
                ed.Technician = Technician;
                ed.User = LoginContext.UserName;
                ed.Work_Order = Work_Order;

                PopupOpen = true;
                if (SART_2012_Web_Service_Client.Send_SART_Email(InfrastructureModule.Token, ed) == true)
                {
                    PopupMessage = "The email has successfully been sent to Segway.";
                    PopupColor = Brushes.LightGreen;
                    Reset();
                }
                else
                {
                    PopupMessage = "The submitting of the email has failed.";
                    PopupColor = Brushes.LightGoldenrodYellow;
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
            }
            finally
            {
                logger.Debug("Leaving");
            }
        }

        /////////////////////////////////////////////
        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Close_Handler  -- Event: SART_WorkOrder_Close_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Close_Handler(Boolean closed)
        {
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
        #region Application_Login_Handler  -- Event: Application_Login_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Login_Handler(String msg)
        {
            _LoginContext = null;
            _Token = null;
            Technician = LoginContext.UserName;
            Email_Address = LoginContext.Email;
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
        }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);

            if (InfrastructureModule.Current_Work_Order != null)
            {
                if (String.IsNullOrEmpty(PT_Serial) == true) PT_Serial = InfrastructureModule.Current_Work_Order.PT_Serial;
                if (String.IsNullOrEmpty(Work_Order) == true) Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
            }
            OnPropertyChanged("Work_Order");
            OnPropertyChanged("PT_Serial");
            OnPropertyChanged("Technician");
            OnPropertyChanged("ErrorCodes");
            OnPropertyChanged("IsCUA_Yes_Checked");
            OnPropertyChanged("IsCUA_No_Checked");
            OnPropertyChanged("CUA_Reason");
            OnPropertyChanged("CUA_Visibility");
            OnPropertyChanged("IsCUB_Yes_Checked");
            OnPropertyChanged("IsCUB_No_Checked");
            OnPropertyChanged("CUB_Reason");
            OnPropertyChanged("CUB_Visibility");
            OnPropertyChanged("IsCnf_Yes_Checked");
            OnPropertyChanged("IsCnf_No_Checked");
            OnPropertyChanged("Cnf_Reason");
            OnPropertyChanged("Cnf_Visibility");
            OnPropertyChanged("Email_Address");
            OnPropertyChanged("Comments");
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private void Reset()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Work_Order = null;
                PT_Serial = null;
                Technician = null;
                ErrorCodes = null;
                IsCUA_Yes_Checked = false;
                IsCUA_No_Checked = false;
                CUA_Reason = null;
                IsCUB_Yes_Checked = false;
                IsCUB_No_Checked = false;
                CUB_Reason = null;
                IsCnf_Yes_Checked = false;
                IsCnf_No_Checked = false;
                Cnf_Reason = null;
                Email_Address = null;
                Comments = null;
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
