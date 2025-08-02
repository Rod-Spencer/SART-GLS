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
using Segway.Service.DatabaseHelper;
using Segway.Service.Objects;
using Segway.Service.SART2012.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Media;



namespace Segway.Modules.Administration
{
    public class User_Management_ViewModel : ViewModelBase, User_Management_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        public User_Management_ViewModel(User_Management_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

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


        #region User_Access_Popup_Open

        private Boolean _User_Access_Popup_Open;

        /// <summary>Property User_Access_Popup_Open of type Boolean</summary>
        public Boolean User_Access_Popup_Open
        {
            get { return _User_Access_Popup_Open; }
            set
            {
                _User_Access_Popup_Open = value;
                OnPropertyChanged("User_Access_Popup_Open");
            }
        }

        #endregion

        #region User_Access_Change_Message

        private String _User_Access_Change_Message;

        /// <summary>Property User_Access_Change_Message of type String</summary>
        public String User_Access_Change_Message
        {
            get { return _User_Access_Change_Message; }
            set
            {
                _User_Access_Change_Message = value;
                OnPropertyChanged("User_Access_Change_Message");
            }
        }

        #endregion

        #region User_Access_Change_Message_Line2

        private String _User_Access_Change_Message_Line2;

        /// <summary>Property User_Access_Change_Message_Line2 of type String</summary>
        public String User_Access_Change_Message_Line2
        {
            get { return _User_Access_Change_Message_Line2; }
            set
            {
                _User_Access_Change_Message_Line2 = value;
                OnPropertyChanged("User_Access_Change_Message_Line2");
            }
        }

        #endregion


        #region userdealer Popup Controls

        #region userdealer_PopupMessage

        private String _userdealer_PopupMessage;

        /// <summary>ViewModel Property: userdealer_PopupMessage of type: String</summary>
        public String userdealer_PopupMessage
        {
            get { return _userdealer_PopupMessage; }
            set
            {
                _userdealer_PopupMessage = value;
                OnPropertyChanged("userdealer_PopupMessage");
            }
        }

        #endregion

        #region userdealer PopupOpen

        private Boolean _userdealer_PopupOpen;

        /// <summary>ViewModel Property: userdealer_PopupOpen of type: Boolean</summary>
        public Boolean userdealer_PopupOpen
        {
            get { return _userdealer_PopupOpen; }
            set
            {
                _userdealer_PopupOpen = value;
                OnPropertyChanged("userdealer_PopupOpen");
            }
        }

        #endregion

        #region userdealer PopupColor

        private Brush _userdealer_PopupColor;

        /// <summary>ViewModel Property: userdealer_PopupColor of type: Brush</summary>
        public Brush userdealer_PopupColor
        {
            get { return _userdealer_PopupColor; }
            set
            {
                _userdealer_PopupColor = value;
                OnPropertyChanged("userdealer_PopupColor");
            }
        }

        #endregion

        #endregion






        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        public bool IsNavigationTarget(NavigationContext navigationContext) { return true; }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            //eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(false);
        }

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
