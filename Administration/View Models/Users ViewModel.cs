using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Database.Objects;
using Segway.Login.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.SART.Objects;
using Segway.Service.AppSettings.Helper;
using Segway.Service.Authentication.Client.REST;
using Segway.Service.Authentication.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.Modules.AddWindow;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace Segway.Modules.Administration
{
    /// <summary>Public Class - Users_ViewModel</summary>
    public class Users_ViewModel : ViewModelBase, Users_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">Users_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Users_ViewModel(Users_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<Admin_Selected_Settings_Name_Event>().Subscribe(Admin_Selected_Settings_Name_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<SART_Dealers_Loaded_Event>().Subscribe(SART_Dealers_Loaded_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates

            GetUserCommand = new DelegateCommand(CommandGetUser, CanCommandGetUser);
            PasteSARTSettingIDCommand = new DelegateCommand(CommandPasteSARTSettingID, CanCommandPasteSARTSettingID);
            NewUserSettingsCommand = new DelegateCommand(CommandNewUserSettings, CanCommandNewUserSettings);
            SaveUserSettingsCommand = new DelegateCommand(CommandSaveUserSettings, CanCommandSaveUserSettings);
            UpdateJTagCommand = new DelegateCommand(CommandUpdateJTag, CanCommandUpdateJTag);
            UpdateUserCommand = new DelegateCommand(CommandUpdateUser, CanCommandUpdateUser);

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

        Dictionary<String, Service_User_Access> AccessList = new Dictionary<String, Service_User_Access>();


        #region Region

        /// <summary>Property Region of type Regional_Settings</summary>
        public Regional_Settings Region
        {
            get { return InfrastructureModule.RegionSettings; }
        }

        #endregion


        #region Dealer_Information

        private Dealer_Info _Dealer_Information;

        /// <summary>Property Dealer_Information of type Dealer_Info</summary>
        public Dealer_Info Dealer_Information
        {
            get
            {
                if (_Dealer_Information == null)
                {
                    _Dealer_Information = container.Resolve<Dealer_Info>(Dealer_Info.Name);
                    if ((_Dealer_Information.Accounts == null) || (_Dealer_Information.Accounts.Count == 0) || (_Dealer_Information.Dealers == null) || (_Dealer_Information.Dealers.Count == 0))
                    {
                        _Dealer_Information.LoadDealer();
                    }
                }

                return _Dealer_Information;
            }
            set
            {
                _Dealer_Information = value;
                OnPropertyChanged("Dealer_Information");
            }
        }

        #endregion

        #region Available_Access_Levels

        private List<String> _Available_Access_Levels;

        /// <summary>Property Available_Access_Levels of type List&lt;String&gt;</summary>
        public List<String> Available_Access_Levels
        {
            get
            {
                if (_Available_Access_Levels == null)
                {
                    _Available_Access_Levels = new List<String>();
                    foreach (UserLevels level in Enum.GetValues(typeof(UserLevels)))
                    {
                        //UserLevels ul = (UserLevels)Enum.Parse(typeof(UserLevels), level);
                        if ((level > UserLevels.NotDefined) && (level < UserLevels.Expert))
                        {
                            _Available_Access_Levels.Add(level.ToString());
                        }
                    }
                }
                return _Available_Access_Levels;
            }
            set
            {
                _Available_Access_Levels = value;
                //OnPropertyChanged("Available_Access_Levels");
            }
        }

        #endregion

        #region Settings_Name

        private String _Settings_Name;

        /// <summary>Property Settings_ID of type String</summary>
        public String Settings_Name
        {
            get { return _Settings_Name; }
            set
            {
                _Settings_Name = value;
                PasteSARTSettingIDCommand.RaiseCanExecuteChanged();
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

        #region User Access Popup Controls

        #region User_Access_Change_Message

        private String _User_Access_Change_Message;

        /// <summary>ViewModel Property: User_Access_PopupMessage of type: String</summary>
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

        #region User_Access_Popup_Open

        private Boolean _User_Access_Popup_Open;

        /// <summary>ViewModel Property: User_Access_PopupOpen of type: Boolean</summary>
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

        #region User_Access_Change_Message_Line2

        private String _User_Access_Change_Message_Line2;

        /// <summary>ViewModel Property: User_Access_PopupColor of type: Brush</summary>
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

        #endregion


        #region User_List

        private ObservableCollection<String> _User_List = null;

        /// <summary>Property User_List of type List(String)</summary>
        public ObservableCollection<String> User_List
        {
            get
            {
                if (_User_List == null)
                {
                    if (_User_List == null) _User_List = new ObservableCollection<String>();
                    CommandGetUser();
                }
                return _User_List;
            }
            set
            {
                _User_List = value;
                OnPropertyChanged("User_List");
            }
        }

        #endregion

        #region Selected_User

        private String _Selected_User;

        /// <summary>Property Selected_User of type String</summary>
        public String Selected_User
        {
            get { return _Selected_User; }
            set
            {
                _Selected_User = value;
                Retrieve_User_Settings(_Selected_User);
                OnPropertyChanged("Selected_User");
                OnPropertyChanged("Access");
                OnPropertyChanged("Is_Access_Enabled");
                OnPropertyChanged("Selected_Level");
                OnPropertyChanged("Selected_Default");
                UpdateUserCommand.RaiseCanExecuteChanged();
                NewUserSettingsCommand.RaiseCanExecuteChanged();
                UpdateJTagCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion



        #region Access_Level_List

        private ObservableCollection<String> _Access_Level_List;

        /// <summary>Property Access_Level_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Access_Level_List
        {
            get
            {
                if (_Access_Level_List == null)
                {
                    _Access_Level_List = new ObservableCollection<String>(Available_Access_Levels);
                }
                return _Access_Level_List;
            }
            set
            {
                OnPropertyChanged("Access_Level_List");
            }
        }

        #endregion

        #region Selected_Level

        /// <summary>Property Selected_Level of type String</summary>
        public String Selected_Level
        {
            get
            {
                if (String.IsNullOrEmpty(Selected_User) == true) return null;
                if (AccessList == null) return null;
                if (AccessList.ContainsKey(Selected_User) == false) return null;
                return AccessList[Selected_User].Access_Level;
            }
            set
            {
                if (String.IsNullOrEmpty(Selected_User) == true) return;
                if (AccessList == null) return;
                if (AccessList.ContainsKey(Selected_User) == false) return;
                if ("Expert|Administrator|Master".Contains(value) == false)
                {
                    if (String.IsNullOrEmpty(value) == false)
                    {
                        AccessList[Selected_User].Access_Level = value;
                    }
                    UpdateUserCommand.RaiseCanExecuteChanged();
                }
                OnPropertyChanged("Selected_User");
                OnPropertyChanged("Selected_Level");
                OnPropertyChanged("Is_Access_Enabled");
            }
        }

        #endregion


        #region Is_Access_Enabled

        /// <summary>Property Is_Access_Enabled of type Boolean</summary>
        public Boolean Is_Access_Enabled
        {
            get
            {
                if (IsMe == true) return false;
                if (String.IsNullOrEmpty(Selected_Level)) return false;
                UserLevels ul = (UserLevels)Enum.Parse(typeof(UserLevels), Selected_Level);
                return ul < UserLevels.Expert;
            }
            set
            {
                OnPropertyChanged("Is_Access_Enabled");
            }
        }

        #endregion




        #region Default_Level_List

        private ObservableCollection<String> _Default_Level_List;

        /// <summary>Property Default_Level_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Default_Level_List
        {
            get
            {
                if (_Default_Level_List == null)
                {
                    _Default_Level_List = new ObservableCollection<String>(Available_Access_Levels);
                }
                return _Default_Level_List;
            }
            set
            {
                _Default_Level_List = value;
                OnPropertyChanged("Default_Level_List");
            }
        }

        #endregion

        #region Selected_Default

        /// <summary>Property Selected_Default of type String</summary>
        public String Selected_Default
        {
            get
            {
                if (String.IsNullOrEmpty(Selected_User) == true) return null;
                if (AccessList == null) return null;
                if (AccessList.ContainsKey(Selected_User) == false) return null;
                return AccessList[Selected_User].User_Default_Level;
            }
            set
            {
                if (String.IsNullOrEmpty(Selected_User) == true) return;
                if (AccessList == null) return;
                if (AccessList.ContainsKey(Selected_User) == false) return;
                UserLevels ul = (UserLevels)Enum.Parse(typeof(UserLevels), value);
                if (ul < UserLevels.Expert)
                {
                    if (String.IsNullOrEmpty(value) == false)
                    {
                        AccessList[Selected_User].User_Default_Level = value;
                    }
                    UpdateUserCommand.RaiseCanExecuteChanged();
                }
                OnPropertyChanged("Selected_User");
                OnPropertyChanged("Is_Access_Enabled");
                OnPropertyChanged("Selected_Default");
            }
        }

        #endregion


        #region User_Access_Jtag_Work_Order

        private String _User_Access_Jtag_Work_Order;

        /// <summary>Property User_Access_Jtag_Work_Order of type String</summary>
        public String User_Access_Jtag_Work_Order
        {
            get { return _User_Access_Jtag_Work_Order; }
            set
            {
                _User_Access_Jtag_Work_Order = value;
                OnPropertyChanged("User_Access_Jtag_Work_Order");
                UpdateJTagCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region User_Access_JTag_Start_DateTime

        private DateTime _User_Access_JTag_Start_DateTime;

        /// <summary>Property User_Access_JTag_Start_DateTime of type DateTime?</summary>
        public DateTime User_Access_JTag_Start_DateTime
        {
            get { return _User_Access_JTag_Start_DateTime; }
            set
            {
                _User_Access_JTag_Start_DateTime = value;
                OnPropertyChanged("User_Access_JTag_Start_DateTime");
            }
        }

        #endregion

        #region User_Access_JTag_End_DateTime

        private DateTime _User_Access_JTag_End_DateTime;

        /// <summary>Property User_Access_JTag_End_DateTime of type DateTime?</summary>
        public DateTime User_Access_JTag_End_DateTime
        {
            get { return _User_Access_JTag_End_DateTime; }
            set
            {
                _User_Access_JTag_End_DateTime = value;
                OnPropertyChanged("User_Access_JTag_End_DateTime");
            }
        }

        #endregion


        #region User Settings

        private SART_User_Settings _User_Settings = null;

        /// <summary>ViewModel property: User_Settings of type: SART_User_Settings</summary>
        public SART_User_Settings User_Settings
        {
            get { return _User_Settings; }
            set
            {
                _User_Settings = value;
                OnPropertyChanged("User_Settings");
                PasteSARTSettingIDCommand.RaiseCanExecuteChanged();
                SaveUserSettingsCommand.RaiseCanExecuteChanged();
                NewUserSettingsCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region IsMe

        private Boolean _IsMe;

        /// <summary>Property IsMe of type Boolean</summary>
        public Boolean IsMe
        {
            get { return _IsMe; }
            set
            {
                _IsMe = value;
                if (_IsMe == true)
                {
                    User_Settings = SART_Users_Web_Service_Client_REST.Select_SART_User_Settings_USER_NAME(Token, LoginContext.UserName);
                }
                else if (Selected_User != null)
                {
                    User_Settings = SART_Users_Web_Service_Client_REST.Select_SART_User_Settings_USER_NAME(Token, Selected_User);
                }
                else
                {
                    User_Settings = null;
                }
                OnPropertyChanged("IsMe");
                OnPropertyChanged("Is_Access_Enabled");
                GetUserCommand.RaiseCanExecuteChanged();
                UpdateUserCommand.RaiseCanExecuteChanged();
                NewUserSettingsCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion



        #region IsUSA

        /// <summary>Property IsAmericas of type Boolean</summary>
        public Boolean IsUSA
        {
            get { return Region.IsUSA; }
            set
            {
                Region.IsUSA = value;
                Configuration_Helper.SetConfigurationValue("Region USA", Region.IsUSA.ToString());
                OnPropertyChanged("IsUSA");
                eventAggregator.GetEvent<SART_Save_Regional_Data_Event>().Publish(true);
                eventAggregator.GetEvent<SART_Load_Dealers_Event>().Publish(true);
            }
        }

        #endregion

        #region IsAPAC

        /// <summary>Property IsAPAC of type Boolean</summary>
        public Boolean IsAPAC
        {
            get { return Region.IsAPAC; }
            set
            {
                Region.IsAPAC = value;
                Configuration_Helper.SetConfigurationValue("Region APAC", Region.IsAPAC.ToString());
                OnPropertyChanged("IsAPAC");
                eventAggregator.GetEvent<SART_Save_Regional_Data_Event>().Publish(true);
                eventAggregator.GetEvent<SART_Load_Dealers_Event>().Publish(true);
            }
        }

        #endregion

        #region IsEMEA

        /// <summary>Property IsEMEA of type Boolean</summary>
        public Boolean IsEMEA
        {
            get { return Region.IsEMEA; }
            set
            {
                Region.IsEMEA = value;
                Configuration_Helper.SetConfigurationValue("Region EMEA", Region.IsEMEA.ToString());
                OnPropertyChanged("IsEMEA");
                eventAggregator.GetEvent<SART_Save_Regional_Data_Event>().Publish(true);
                eventAggregator.GetEvent<SART_Load_Dealers_Event>().Publish(true);
            }
        }

        #endregion

        #region IsLTAM

        /// <summary>Property IsLTAM of type Boolean</summary>
        public Boolean IsLTAM
        {
            get { return Region.IsLTAM; }
            set
            {
                Region.IsLTAM = value;
                Configuration_Helper.SetConfigurationValue("Region Latin America", Region.IsLTAM.ToString());
                OnPropertyChanged("IsLTAM");
                eventAggregator.GetEvent<SART_Save_Regional_Data_Event>().Publish(true);
                eventAggregator.GetEvent<SART_Load_Dealers_Event>().Publish(true);
            }
        }

        #endregion

        #region IsCanada

        /// <summary>Property IsCanada of type Boolean</summary>
        public Boolean IsCanada
        {
            get { return Region.IsCANA; }
            set
            {
                Region.IsCANA = value;
                Configuration_Helper.SetConfigurationValue("Region Canada", Region.IsCANA.ToString());
                OnPropertyChanged("IsCanada");
                eventAggregator.GetEvent<SART_Save_Regional_Data_Event>().Publish(true);
                eventAggregator.GetEvent<SART_Load_Dealers_Event>().Publish(true);
            }
        }

        #endregion




        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region GetUserCommand

        /// <summary>Delegate Command: GetUserCommand</summary>
        public DelegateCommand GetUserCommand { get; set; }


        private Boolean CanCommandGetUser()
        {
            return !IsMe;
        }

        private void CommandGetUser()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (InfrastructureModule.Token == null) return;

                if (AccessList == null) AccessList = new Dictionary<String, Service_User_Access>();
                else AccessList.Clear();

                List<Service_Users> users = Authentication_User_Web_Service_Client_REST.Select_Service_Users_All(InfrastructureModule.Token);
                if ((users != null) && (users.Count > 0))
                {
                    logger.Debug("Retrieved {0} service user records", users.Count);

                    if (InfrastructureModule.Token.LoginContext.User_Level < UserLevels.Master)
                    {
                        if (users.Count > 1) users = users.Where(x => x.User_Name == InfrastructureModule.Token.LoginContext.UserName).ToList();
                    }
                    else
                    {
                        if (users.Count > 1) users = users.OrderBy(x => x.User_Name).ToList();
                    }
                    String tool = App_Settings_Helper.GetConfigurationValue("Remote Service Tool", "RST");
                    logger.Debug("Filtering list of users to those authorized for: {0}", tool);
                    users.ForEach(z =>
                    {
                        if (String.IsNullOrEmpty(z.User_Name) == false)
                        {
                            var access = z.Accesses.FirstOrDefault(x => x.Tool_Name == tool);
                            if (access != null)
                            {
                                AccessList[z.User_Name] = access;
                            }
                        }
                    });
                    //foreach (Service_Users user in users)
                    //{
                    //    if (user == null) continue;
                    //    if (String.IsNullOrEmpty(user.User_Name) == true) continue;
                    //    if (String.IsNullOrEmpty(user.Syteline_Account_ID) == true) continue;
                    //    //            if (Find_Account(user.Syteline_Account_ID) == false) continue;

                    //    var access = user.Accesses.FirstOrDefault(x => x.Tool_Name == tool);
                    //    if (access != null)
                    //    {
                    //        AccessList[user.User_Name] = access;
                    //    }
                    //}
                }
                else
                {
                    logger.Warn("No users found");
                }
                if ((AccessList != null) && (AccessList.Count > 0))
                {
                    User_List = new ObservableCollection<String>(AccessList.Keys);
                }
                else
                {
                    User_List = new ObservableCollection<String>();
                }
                logger.Info("User List has {0} entries", User_List.Count);
                Selected_User = null;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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


        #region UpdateUserCommand

        /// <summary>Delegate Command: UpdateUserCommand</summary>
        public DelegateCommand UpdateUserCommand { get; set; }


        private Boolean CanCommandUpdateUser()
        {
            if (IsMe == true) return false;
            if (String.IsNullOrEmpty(Selected_User) == true) return false;
            if (String.IsNullOrEmpty(Selected_Level) == true) return false;
            UserLevels ul = (UserLevels)Enum.Parse(typeof(UserLevels), Selected_Level);
            return ul < UserLevels.Expert;
        }
        private void CommandUpdateUser()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (AccessList.ContainsKey(_Selected_User))
                {
                    Service_User_Access sua = AccessList[_Selected_User];
                    if (Authentication_Access_Web_Service_Client_REST.Update_Service_User_Access_Object(InfrastructureModule.Token, sua) == true)
                    {
                        User_Access_Change_Message = String.Format("User {0} was changed to access level {1}", Selected_User, Selected_Level);
                        User_Access_Change_Message_Line2 = "The new permission levels will be in effect on the user's next login, and will automatically revert when the user logs out.";
                        User_Access_Popup_Open = true;
                    }
                }
                CommandSaveUserSettings();
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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


        #region UpdateJTagCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: UpdateJTagCommand</summary>
        public DelegateCommand UpdateJTagCommand { get; set; }
        private Boolean CanCommandUpdateJTag()
        {
            if (String.IsNullOrEmpty(Selected_User) == true) return false;
            if (String.IsNullOrEmpty(User_Access_Jtag_Work_Order) == true) return false;
            return true;
        }

        private void CommandUpdateJTag()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                criteria.Add(new FieldData("Work_Order_Number", User_Access_Jtag_Work_Order));
                criteria.Add(new FieldData("User_Name", Selected_User));

                SART_JTag_Visibility jtag = null;

                var jtags = SART_JTAGvis_Web_Service_Client_REST.Select_SART_JTag_Visibility_Criteria(Token, criteria);
                if ((jtags == null) || (jtags.Count == 0))
                {
                    jtag = new SART_JTag_Visibility();
                    jtag.Authority_Name = LoginContext.UserName;
                    jtag.User_Name = Selected_User;
                    jtag.Work_Order_Number = User_Access_Jtag_Work_Order;
                    jtag.Date_Time_Start = User_Access_JTag_Start_DateTime;
                    jtag.Date_Time_End = User_Access_JTag_End_DateTime;
                    SART_JTAGvis_Web_Service_Client_REST.Insert_SART_JTag_Visibility_Key(Token, jtag);
                }
                else
                {
                    jtag = jtags[0];
                    while (jtags.Count > 1) SART_JTAGvis_Web_Service_Client_REST.Delete_SART_JTag_Visibility_Key(Token, jtags[1].ID);
                    jtag.Work_Order_Number = User_Access_Jtag_Work_Order;
                    jtag.Date_Time_Start = User_Access_JTag_Start_DateTime;
                    jtag.Date_Time_End = User_Access_JTag_End_DateTime;
                    SART_JTAGvis_Web_Service_Client_REST.Update_SART_JTag_Visibility_Key(Token, jtag);
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


        #region NewUserSettingsCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: NewUserSettingsCommand</summary>
        public DelegateCommand NewUserSettingsCommand { get; set; }
        private Boolean CanCommandNewUserSettings()
        {
            if (IsMe == true) return false;
            if (String.IsNullOrEmpty(Selected_User) == true) return false;
            if (User_Settings == null) return true;
            return false;
        }

        private void CommandNewUserSettings()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                SART_User_Settings us = new SART_User_Settings();
                us.User_Name = Selected_User;
                us.Date_Time_Entered = us.Date_Time_Updated = DateTime.Now;
                User_Settings = SART_Users_Web_Service_Client_REST.Insert_SART_User_Settings_Key(InfrastructureModule.Token, us);
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


        #region SaveUserSettingsCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: SaveUserSettingsCommand</summary>
        public DelegateCommand SaveUserSettingsCommand { get; set; }
        private Boolean CanCommandSaveUserSettings()
        {
            if (User_Settings == null) return false;
            return true;
        }

        private void CommandSaveUserSettings()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (SART_Users_Web_Service_Client_REST.Update_SART_User_Settings_Key(InfrastructureModule.Token, User_Settings) == true)
                {
                    OnPropertyChanged("User_Settings");
                    String msg = "User settings have been saved";
                    logger.Debug(msg);
                    Message_Window.Success(msg, height: Window_Sizes.Small).ShowDialog();
                    eventAggregator.GetEvent<SART_UserSettings_Changed_Event>().Publish(User_Settings);
                }
                else
                {
                    throw new Exception("Unable to save user settings");
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


        #region PasteSARTSettingIDCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PasteSARTSettingIDCommand</summary>
        public DelegateCommand PasteSARTSettingIDCommand { get; set; }
        private Boolean CanCommandPasteSARTSettingID()
        {
            if (User_Settings == null) return false;
            if (String.IsNullOrEmpty(Settings_Name) == true) return false;
            return true;
        }

        private void CommandPasteSARTSettingID()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (IsMe == true)
                {
                    var setting = SART_Users_Web_Service_Client_REST.Select_SART_User_Settings_USER_NAME(Token, LoginContext.UserName);
                    setting.SART_Settings_Name = Settings_Name;
                    User_Settings = setting;
                }
                else if (String.IsNullOrEmpty(Selected_User) == false)
                {
                    var setting = SART_Users_Web_Service_Client_REST.Select_SART_User_Settings_USER_NAME(Token, Selected_User);
                    setting.SART_Settings_Name = Settings_Name;
                    User_Settings = setting;
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


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Admin_Selected_Settings_Name_Handler  -- Event: Admin_Selected_Settings_Name_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Admin_Selected_Settings_Name_Handler(String name)
        {
            Settings_Name = name;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_Dealers_Loaded_Handler  -- Event: SART_Dealers_Loaded_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_Dealers_Loaded_Handler(Boolean clear)
        {
            User_List = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers
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
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);

            if ((User_List == null) || (User_List.Count == 0)) CommandGetUser();
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private Boolean Find_Account(String acct)
        {
            if (Dealer_Information == null) return false;
            if (Dealer_Information.Dealers == null) return false;

            return Dealer_Information.Dealers.ContainsKey(acct);
        }


        private void Retrieve_User_Settings(String user)
        {
            if (String.IsNullOrEmpty(user) == false)
            {
                User_Settings = SART_Users_Web_Service_Client_REST.Select_SART_User_Settings_USER_NAME(Token, user);
            }
            else
            {
                User_Settings = null;
            }
        }


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
