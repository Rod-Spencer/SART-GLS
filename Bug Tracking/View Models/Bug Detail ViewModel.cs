using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using SART_Infrastructure;
using Segway.Modules.Controls.Comment;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.Login;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Service.Authentication.Objects;
using Segway.Service.Bug.Objects;
using Segway.Service.Common;
using Segway.Service.Controls.StatusBars;
using Segway.Service.DatabaseHelper;
using Segway.Service.Helper;
using Segway.Service.Modules.AddWindow;
using Segway.Service.Objects;
using Segway.Service.RT_Logs.Client;
using Segway.Service.Tools.BugZilla.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace Segway.Service.Bugs
{
    /// <summary>Public Class - Bug_Detail_ViewModel</summary>
    public class Bug_Detail_ViewModel : ViewModelBase, Bug_Detail_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private static IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">Bug_Detail_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Bug_Detail_ViewModel(Bug_Detail_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            Bug_Detail_ViewModel.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Application_Login_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<Bug_Open_Event>().Subscribe(Bug_Open_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<Application_Activated_Event>().Subscribe(Application_Activated_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<Bug_Menu_Detail_Activate_Event>().Subscribe(Bug_Menu_Detail_Activate_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates

            SubmitCommand = new DelegateCommand(CommandSubmit, CanCommandSubmit);
            PictureAddFileCommand = new DelegateCommand(CommandPictureAddFile, CanCommandPictureAddFile);
            PictureOpenCommand = new DelegateCommand(CommandPictureOpen, CanCommandPictureOpen);
            PictureDelCommand = new DelegateCommand(CommandPictureDel, CanCommandPictureDel);
            PictureEditCommand = new DelegateCommand(CommandPictureEdit, CanCommandPictureEdit);
            PictureAddClipCommand = new DelegateCommand(CommandPictureAddClip, CanCommandPictureAddClip);
            UploadLogCommand = new DelegateCommand(CommandUploadLog, CanCommandUploadLog);
            BugStatusAcceptCommand = new DelegateCommand(CommandBugStatusAccept, CanCommandBugStatusAccept);
            BugStatusResolveCommand = new DelegateCommand(CommandBugStatusResolve, CanCommandBugStatusResolve);
            BugStatusReOpenCommand = new DelegateCommand(CommandBugStatusReOpen, CanCommandBugStatusReOpen);
            BugStatusVerifyCommand = new DelegateCommand(CommandBugStatusVerify, CanCommandBugStatusVerify);
            BugStatusCloseCommand = new DelegateCommand(CommandBugStatusClose, CanCommandBugStatusClose);
            BugStatusStartCommand = new DelegateCommand(CommandBugStatusStart, CanCommandBugStatusStart);
            BugStatusRejectCommand = new DelegateCommand(CommandBugStatusReject, CanCommandBugStatusReject);

            #endregion
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties


        #region Token

        private static AuthenticationToken _Token;

        /// <summary>Property Token of type AuthenticationToken</summary>
        public static AuthenticationToken Token
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


        #region BugObject

        private Bugs_Tracker _BugObject;

        /// <summary>Property BugObject of type Bugs</summary>
        public Bugs_Tracker BugObject
        {
            get
            {
                return _BugObject;
            }
            set
            {
                _BugObject = value;
                Refresh();
                SubmitCommand.RaiseCanExecuteChanged();
                PictureAddClipCommand.RaiseCanExecuteChanged();
                PictureAddFileCommand.RaiseCanExecuteChanged();
                PictureDelCommand.RaiseCanExecuteChanged();
                PictureEditCommand.RaiseCanExecuteChanged();
                PictureOpenCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion


        #region Comment_List

        private List<Comment_Object> _Comment_List;

        /// <summary>Property Comment_List of type List&lt;Comment_Object&gt;</summary>
        public List<Comment_Object> Comment_List
        {
            get
            {
                if (_Comment_List == null) _Comment_List = new List<Comment_Object>();
                return _Comment_List;
            }
        }

        #endregion


        #region User_Profile

        private Profiles _User_Profile;

        /// <summary>Property User_Profile of type Profiles</summary>
        public Profiles User_Profile
        {
            get
            {
                if (_User_Profile == null)
                {
                    if (LoginContext != null)
                    {
                        if (String.IsNullOrEmpty(LoginContext.Email) == false)
                        {
                            _User_Profile = BugZilla_Web_Service_Client.Select_Profiles_LOGIN_NAME(Token, LoginContext.Email);
                            if (_User_Profile == null)
                            {
                                _User_Profile = new Profiles();
                                _User_Profile.Login_Name = LoginContext.Email;
                                _User_Profile.Real_Name = LoginContext.UserName;
                                _User_Profile.Refreshed_When = DateTime.Now;
                                _User_Profile.MY_Bugs_Link = true;
                                _User_Profile.Disabled_Text = null;

                                _User_Profile = BugZilla_Web_Service_Client.Insert_Profiles_Key(Token, _User_Profile);
                            }

                            if (_User_Profile != null)
                            {
                                Profile_List[_User_Profile.User_ID] = _User_Profile;
                            }
                        }
                    }
                }
                return _User_Profile;
            }
            set
            {
                _User_Profile = value;
                OnPropertyChanged("User_Profile");
            }
        }

        #endregion


        #region Bug_Types

        private List<String> _Bug_Types;

        /// <summary>Property Bug_Types of type List&lt;String&gt;</summary>
        public List<String> Bug_Types
        {
            get
            {
                if (_Bug_Types == null)
                {
                    String path = Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "Bug Types.xml");
                    FileInfo fi = new FileInfo(path);
                    _Bug_Types = Serialization.DeserializeFromFile<List<String>>(fi);
                    if (_Bug_Types == null)
                    {
                        _Bug_Types = new List<String>(new String[] { "Bug", "Change Request", "Enhancement" });
                        if (fi.Directory.Exists == false) fi.Directory.Create();
                        else if (fi.Exists == true) fi.Delete();
                        Serialization.SerializeToFile<List<String>>(_Bug_Types, fi);
                    }
                }
                return _Bug_Types;
            }
        }

        #endregion


        #region Reproduce_List

        private List<String> _Reproduce_List;

        /// <summary>Property Reproduce_List of type List&lt;String&gt;</summary>
        public List<String> Reproduce_List
        {
            get
            {
                if (_Reproduce_List == null)
                {
                    String path = Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "Reproduce Types.xml");
                    FileInfo fi = new FileInfo(path);
                    _Reproduce_List = Serialization.DeserializeFromFile<List<String>>(fi);
                    if (_Reproduce_List == null)
                    {
                        _Reproduce_List = new List<String>(new String[] { "Consistent", "Intermittent", "Random" });
                        if (fi.Directory.Exists == false) fi.Directory.Create();
                        else if (fi.Exists == true) fi.Delete();
                        Serialization.SerializeToFile<List<String>>(_Reproduce_List, fi);
                    }
                }
                return _Reproduce_List;
            }
        }

        #endregion


        #region Priorities

        private List<String> _Priorities;

        /// <summary>Property Priorities of type List&lt;String&gt;</summary>
        public List<String> Priorities
        {
            get
            {
                if (_Priorities == null)
                {
                    if (Token == null) return null;
                    var plist = BugZilla_Web_Service_Client.Select_Priority_Condition(Token, null);
                    _Priorities = new List<String>();
                    foreach (var p in plist) _Priorities.Add(p.Value);
                }
                return _Priorities;
            }
            set
            {
                _Priorities = value;
                OnPropertyChanged("Priorities");
            }
        }

        #endregion


        #region Bug_Statuses

        private List<String> _Bug_Statuses;

        /// <summary>Property Bug_Statuses of type List&lt;String&gt;</summary>
        public List<String> Bug_Statuses
        {
            get
            {
                if (_Bug_Statuses == null)
                {
                    var bList = BugZilla_Web_Service_Client.Select_Bug_Status_Criteria(Token, null);
                    if (bList != null)
                    {
                        _Bug_Statuses = new List<String>();
                        foreach (var b in bList) _Bug_Statuses.Add(b.Value);
                    }
                }
                return _Bug_Statuses;
            }
            set
            {
                _Bug_Statuses = value;
                OnPropertyChanged("Bug_Statuses");
            }
        }

        #endregion


        #region Profile_List

        private static Dictionary<int, Profiles> _Profile_List;

        /// <summary>Property Profile_List of type Dictionary&lt;int, Profiles&gt;</summary>
        public static Dictionary<int, Profiles> Profile_List
        {
            get
            {
                if (_Profile_List == null) _Profile_List = new Dictionary<int, Profiles>();
                return _Profile_List;
            }
        }

        #endregion


        #region Field_Definitions

        private List<Fielddefs> _Field_Definitions;

        /// <summary>Property Field_Definitions of type List&lt;Fielddefs&gt;</summary>
        public List<Fielddefs> Field_Definitions
        {
            get
            {
                if (_Field_Definitions == null) _Field_Definitions = new List<Fielddefs>();
                return _Field_Definitions;
            }
            set
            {
                _Field_Definitions = value;
            }
        }

        #endregion


        #region Dev_Defaults

        private Developer_Defaults _Dev_Defaults;

        /// <summary>Property Dev_Defaults of type Developer_Defaults</summary>
        public Developer_Defaults Dev_Defaults
        {
            get
            {
                if (_Dev_Defaults == null)
                {
                    String path = Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "Developer Defaults.xml");
                    FileInfo fi = new FileInfo(path);
                    if (fi.Exists)
                    {
                        _Dev_Defaults = Serialization.DeserializeFromFile<Developer_Defaults>(fi);
                    }
                    //else
                    //{
                    //    _Dev_Defaults = new Developer_Defaults()
                    //    {
                    //        SubVersion_Revision_Command = "svn info svn://swdev1/operations-sw/apps",
                    //        Remote_Service_Tool_Version_Source = @"C:\Code\Projects\sart\implementation\SART2012\RST\Properties\AssemblyInfo.cs",
                    //        SART_Internal_Version_Source = @"C:\Code\Projects\sart\implementation\SART2012\SART Internal\Properties\AssemblyInfo.cs"
                    //    };
                    //    Serialization.SerializeToFile<Developer_Defaults>(_Dev_Defaults, fi);
                    //}
                }

                return _Dev_Defaults;
            }
        }

        #endregion


        #region picDesc

        private String _picDesc = null;

        /// <summary>Property picDesc of type String</summary>
        public String picDesc
        {
            get { return _picDesc; }
            set
            {
                _picDesc = value;
                if ((_picDesc != null) && (_picDesc.Length > 300)) _picDesc = _picDesc.Substring(0, 300);
            }
        }

        #endregion


        #region Product_Components

        private List<Components> _Product_Components;
        /// <summary>Property Product_Components of type List&lt;Components&gt;</summary>
        public List<Components> Product_Components
        {
            get
            {
                if (_Product_Components == null)
                {
                    _Product_Components = BugZilla_Web_Service_Client.Select_Components_PRODUCT_ID(Token, (Int16)BugObject.Product_ID);
                }
                return _Product_Components;
            }
            set
            {
                _Product_Components = value;
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

        #region Issue_ID

        /// <summary>ViewModel property: Issue_ID of type: int points to BugObject.Bug_ID</summary>
        public int? Issue_ID
        {
            get
            {
                if (BugObject == null) return null;
                if (BugObject.ID == 0) return null;
                return BugObject.ID;
            }
            set
            {
                if (BugObject == null) return;
                if (value == null) BugObject.ID = 0;
                BugObject.ID = value.Value;
                OnPropertyChanged("Issue_ID");
            }
        }

        #endregion

        #region Work_Order

        /// <summary>Property Work_Order of type String</summary>
        public String Work_Order
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Work_Order;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Work_Order = value;
                OnPropertyChanged("Work_Order");
            }
        }

        #endregion

        #region Frequency

        /// <summary>Property Frequency of type String</summary>
        public String Frequency
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Frequency;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Frequency = value;
                OnPropertyChanged("Frequency");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Selected_Type

        /// <summary>Property Selected_Type of type String</summary>
        public String Selected_Type
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Bug_Type;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Bug_Type = value;
                OnPropertyChanged("Selected_Type");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Selected_Severity

        /// <summary>Property Selected_Severity of type String</summary>
        public String Selected_Severity
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Severity;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Severity = value;
                OnPropertyChanged("Selected_Severity");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Application_Name

        /// <summary>Property Application_Name of type String</summary>
        public String Application_Name
        {
            get
            {
                if (BugObject == null) return Application_Helper.GetConfigurationValue("ToolName");
                if (String.IsNullOrEmpty(BugObject.Application) == true) BugObject.Application = Application_Helper.GetConfigurationValue("ToolName");
                return BugObject.Application;
            }
            set
            {
                OnPropertyChanged("Application_Name");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Version_Number

        /// <summary>Property Version_Number of type String</summary>
        public String Version_Number
        {
            get
            {
                if (BugObject == null) return null;
                if (String.IsNullOrEmpty(BugObject.Reported_Version) == true) BugObject.Reported_Version = Application_Helper.Assembly_Version();
                return BugObject.Reported_Version;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Reported_Version = value;
                OnPropertyChanged("Version_Number");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Components

        private ObservableCollection<String> _Components;

        /// <summary>Property Components of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Components
        {
            get
            {
                if (_Components == null)
                {
                    _Components = new ObservableCollection<String>();
                }
                if (_Components.Count == 0)
                {
                    if (Token != null)
                    {
                        Load_Components();
                    }
                }
                return _Components;
            }
            set
            {
                _Components = value;
                OnPropertyChanged("Components");
            }
        }

        #endregion

        #region Selected_Component

        /// <summary>Property Selected_Component of type String</summary>
        public String Selected_Component
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Component;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Component = value;
                OnPropertyChanged("Selected_Component");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Summary

        /// <summary>Property Summary of type String</summary>
        public String Summary
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Description;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Description = value;
                OnPropertyChanged("Summary");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Occurrance_DateTime

        /// <summary>Property Occurrance_DateTime of type DateTime?</summary>
        public DateTime Occurrance_DateTime
        {
            get
            {
                if (BugObject == null) return DateTime.Now;
                if (BugObject.Occurrance_Timestamp.HasValue == false) BugObject.Occurrance_Timestamp = DateTime.Now;
                return BugObject.Occurrance_Timestamp.Value;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Occurrance_Timestamp = value;
                OnPropertyChanged("Occurrance_DateTime");
            }
        }

        #endregion

        #region UserName

        /// <summary>ViewModel property: UserName of type: String points to LoginContext.UserName</summary>
        public String UserName
        {
            get
            {
                if (LoginContext == null) return null;
                if (BugObject == null) return null;
                if (String.IsNullOrEmpty(BugObject.Reported_By) == true) BugObject.Reported_By = LoginContext.UserName;
                return BugObject.Reported_By;
            }
            set
            {
                if (LoginContext == null) return;
                if (BugObject == null) return;
                BugObject.Reported_By = value;
                OnPropertyChanged("UserName");
            }
        }

        #endregion

        #region Severity_List

        private ObservableCollection<String> _Severity_List;

        /// <summary>Property Severity_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Severity_List
        {
            get
            {
                if (_Severity_List == null)
                {
                    _Severity_List = new ObservableCollection<String>();
                }
                if (_Severity_List.Count == 0)
                {
                    if (Token != null)
                    {
                        Load_Severities();
                    }
                }
                return _Severity_List;
            }
            set
            {
                _Severity_List = value;
                OnPropertyChanged("Severity_List");
            }
        }


        #endregion

        #region Comment_Count

        private Int32 _Comment_Count;

        /// <summary>Property Comment_Count of type Int32</summary>
        public Int32 Comment_Count
        {
            get { return _Comment_Count; }
            set
            {
                _Comment_Count = value;
                OnPropertyChanged("Comment_Count");
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion


        #region Picture_List

        private ObservableCollection<Attachments> _Picture_List;

        /// <summary>Property Picture_List of type ObservableCollection&lt;Seg_SART_Pictures_Nodata&gt;</summary>
        public ObservableCollection<Attachments> Picture_List
        {
            get
            {
                if (_Picture_List == null) _Picture_List = new ObservableCollection<Attachments>();
                return _Picture_List;
            }
            set
            {
                _Picture_List = value;
                OnPropertyChanged("Picture_List");
            }
        }

        #endregion

        #region Selected_Picture

        private Attachments _Selected_Picture;

        /// <summary>Property Selected_Picture of type Seg_SART_Pictures_Nodata</summary>
        public Attachments Selected_Picture
        {
            get { return _Selected_Picture; }
            set
            {
                _Selected_Picture = value;
                OnPropertyChanged("Selected_Picture");
                PictureOpenCommand.RaiseCanExecuteChanged();
                PictureEditCommand.RaiseCanExecuteChanged();
                PictureAddClipCommand.RaiseCanExecuteChanged();
                PictureAddFileCommand.RaiseCanExecuteChanged();
                PictureDelCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion


        #region Bug_Type_List

        private ObservableCollection<String> _Bug_Type_List;

        /// <summary>Property Bug_Type_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Bug_Type_List
        {
            get
            {
                if (_Bug_Type_List == null) _Bug_Type_List = new ObservableCollection<String>(Bug_Types);
                return _Bug_Type_List;
            }
            set
            {
                _Bug_Type_List = value;
                OnPropertyChanged("Bug_Type_List");
            }
        }

        #endregion

        #region Reproducible_List

        private ObservableCollection<String> _Reproducible_List;

        /// <summary>Property Reproducible_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Reproducible_List
        {
            get
            {
                if (_Reproducible_List == null) _Reproducible_List = new ObservableCollection<String>(Reproduce_List);
                return _Reproducible_List;
            }
            set
            {
                _Reproducible_List = value;
                OnPropertyChanged("Reproducible_List");
            }
        }

        #endregion

        #region Priority_List

        private ObservableCollection<String> _Priority_List;

        /// <summary>Property Priority_List of type ObservableCollection&lt;String&gt;</summary>
        public ObservableCollection<String> Priority_List
        {
            get
            {
                if (_Priority_List == null)
                {
                    if (Priorities != null) _Priority_List = new ObservableCollection<String>(Priorities);
                }
                return _Priority_List;
            }
            set
            {
                _Priority_List = value;
                OnPropertyChanged("Priority_List");
            }
        }

        #endregion

        #region Selected_Priority

        /// <summary>ViewModel property: Selected_Priority of type: String points to BugObject.Priority</summary>
        public String Selected_Priority
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Priority;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Priority = value;
                OnPropertyChanged("Selected_Priority");
            }
        }

        #endregion

        #region Status

        /// <summary>ViewModel property: Status of type: String points to BugObject.Status</summary>
        public String Status
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Status;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Status = value;
                OnPropertyChanged("Status");
                BugStatusReOpenCommand.RaiseCanExecuteChanged();
                BugStatusAcceptCommand.RaiseCanExecuteChanged();
                BugStatusResolveCommand.RaiseCanExecuteChanged();
                BugStatusVerifyCommand.RaiseCanExecuteChanged();
                BugStatusCloseCommand.RaiseCanExecuteChanged();
                BugStatusStartCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Master_Visibility

        /// <summary>Property Master_Visibility of type Visibility</summary>
        public Visibility Master_Visibility
        {
            get
            {
                if (LoginContext == null) return Visibility.Collapsed;
                if (LoginContext.User_Level == UserLevels.Master)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
            set
            {
                OnPropertyChanged("Master_Visibility");
            }
        }

        #endregion

        #region Verify_Visibility

        /// <summary>Property Verify_Visibility of type Visibility</summary>
        public Visibility Verify_Visibility
        {
            get
            {
                if (BugObject == null) return Visibility.Collapsed;
                if (BugObject.Status == "RESOLVED") return Visibility.Visible;
                return Visibility.Collapsed;
            }
            set
            {
                OnPropertyChanged("Verify_Visibility");
            }
        }

        #endregion

        #region Work_Completed

        /// <summary>ViewModel property: Work_Completed of type: DateTime? points to BugObject.Work_Completed</summary>
        public DateTime? Work_Completed
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Work_Completed;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Work_Completed = value;
                OnPropertyChanged("Work_Completed");
            }
        }

        #endregion

        #region Work_Started

        /// <summary>ViewModel property: Work_Started of type: DateTime? points to BugObject.Work_Started</summary>
        public DateTime? Work_Started
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Work_Started;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Work_Started = value;
                OnPropertyChanged("Work_Started");
            }
        }

        #endregion

        #region Implemented_Version

        /// <summary>ViewModel property: Implemented_Version of type: String points to BugObject.Implemented_Version</summary>
        public String Implemented_Version
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Implemented_Version;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Implemented_Version = value;
                OnPropertyChanged("Implemented_Version");
            }
        }

        #endregion

        #region Revision

        /// <summary>ViewModel property: Revision of type: String points to BugObject.Revision</summary>
        public int? Revision
        {
            get
            {
                if (BugObject == null) return null;
                return BugObject.Revision;
            }
            set
            {
                if (BugObject == null) return;
                BugObject.Revision = value;
                OnPropertyChanged("Revision");
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers


        #region PictureAddFileCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PictureAddFileCommand</summary>
        public DelegateCommand PictureAddFileCommand { get; set; }
        private Boolean CanCommandPictureAddFile()
        {
            if (BugObject == null) return false;
            return BugObject.Bug_ID > 0;
        }

        private void CommandPictureAddFile()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);


                OpenFileDialog ofd = new OpenFileDialog();
                if (LoginContext.User_Level >= UserLevels.Expert) ofd.Multiselect = true;
                else ofd.Multiselect = false;
                ofd.CheckFileExists = true;
                if (ofd.ShowDialog() == true)
                {
                    Entry_Window ew = null;
                    picDesc = null;
                    while (String.IsNullOrEmpty(picDesc) == true)
                    {
                        ew = new Entry_Window();
                        ew.Width = 600;
                        ew.dp_LabelText = " Edit Description ";

                        Boolean? result = ew.ShowDialog();
                        if (result == true)
                        {
                            if (String.IsNullOrEmpty(ew.dp_TextBoxText) == false) break;
                        }
                        if (result == false)
                        {
                            return;
                        }
                    }

                    picDesc = ew.dp_TextBoxText;

                    eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(Brushes.Snow);

                    Thread back = new Thread(new ParameterizedThreadStart(Load_Selected_Pictures));
                    back.IsBackground = true;
                    back.Start(ofd.FileNames);

                    //int picCount = 0;
                    //foreach (String fname in ofd.FileNames)
                    //    picCount = Load_Selected_Pictures(ofd, ew, picCount, fname);
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = Brushes.Pink;
                PopupMessage = msg;
                PopupOpen = true;
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Pink);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error: Unable to upload picture(s)");
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void Load_Selected_Pictures(object files)
        {
            if (files == null) return;
            if (BugObject == null) return;

            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                List<String> fileList = new List<String>((String[])files);
                foreach (String fname in fileList)
                {
                    FileInfo fi = new FileInfo(fname);
                    FileInfo dstFI = null;
                    do
                    {
                        dstFI = Format_Cache_Filename(GUID_Helper.Encode(Guid.NewGuid()) + Path.GetExtension(fi.Name));
                    } while (dstFI.Exists == true);

                    FileStream fs = fi.OpenRead();
                    Byte[] picdata = new Byte[fi.Length];
                    int read = fs.Read(picdata, 0, (int)fi.Length);
                    fs.Close();
                    if (read == (int)fi.Length)
                    {
                        FileStream fout = dstFI.OpenWrite();
                        if (fout != null)
                        {
                            fout.Write(picdata, 0, read);
                            fout.Close();
                        }
                        dstFI.Refresh();

                        Attachments att = new Attachments()
                        {
                            Bug_ID = BugObject.ID,
                            Creation_TS = DateTime.Now,
                            Description = picDesc,
                            File_Name = dstFI.Name,
                            Mime_Type = String.Format("image/{0}", Format_Image_Type(fi.Name)),
                            Submitter_ID = User_Profile.User_ID,
                            IS_Obsolete = false,
                            IS_Private = false,
                            IS_Url = false,
                            IS_Patch = false
                        };


                        var attach = BugZilla_Web_Service_Client.Insert_Attachments_Key(Token, att);
                        if (attach == null) throw new Exception(String.Format("Could not Insert Picture: {0}", att.File_Name));

                        Add_To_Picture_List(attach);
                        Selected_Picture = attach;

                        Attach_Data ad = new Attach_Data();
                        ad.ID = attach.Attach_ID;
                        ad.The_Data = picdata; // Image_Helper.ImageFileToBuffer(fi.FullName);

                        BugZilla_Web_Service_Client.Upload_Attach_Data(Token, ad);

                        eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Uploaded picture: {0} of {1}", fileList.IndexOf(fname) + 1, fileList.Count));
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
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion



        #region PictureOpenCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PictureOpenCommand</summary>
        public DelegateCommand PictureOpenCommand { get; set; }
        private Boolean CanCommandPictureOpen()
        {
            if (BugObject == null) return false;
            if (BugObject.Bug_ID == 0) return false;
            return Selected_Picture != null;
        }

        private void CommandPictureOpen()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Thread back = new Thread(new ThreadStart(CommandPictureOpen_Back));
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


        #region PictureDelCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PictureDelCommand</summary>
        public DelegateCommand PictureDelCommand { get; set; }
        private Boolean CanCommandPictureDel()
        {
            if (BugObject == null) return false;
            if (BugObject.Bug_ID == 0) return false;
            return Selected_Picture != null;
        }
        private void CommandPictureDel()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Thread back = new Thread(new ThreadStart(CommandPictureDel_Back));
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



        #region PictureEditCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PictureEditCommand</summary>
        public DelegateCommand PictureEditCommand { get; set; }
        private Boolean CanCommandPictureEdit()
        {
            if (BugObject == null) return false;
            if (BugObject.Bug_ID == 0) return false;
            return Selected_Picture != null;
        }

        private void CommandPictureEdit()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Entry_Window ew = new Entry_Window();
                ew.dp_TextBoxText = Selected_Picture.Description;
                ew.dp_LabelText = " Edit Description ";
                ew.Width = 600;
                if (ew.ShowDialog() == true)
                {
                    var store = Selected_Picture;
                    Picture_List.Remove(Selected_Picture);
                    store.Description = ew.dp_TextBoxText;

                    Add_To_Picture_List(store);
                    Selected_Picture = store;

                    var attach = BugZilla_Web_Service_Client.Select_Attachments_Key(Token, store.Attach_ID);
                    if (attach == null) throw new Exception(String.Format("Could not find Picture: {0} ({1})", store.File_Name, store.Attach_ID));

                    attach.Update(store);
                    if (BugZilla_Web_Service_Client.Update_Attachments_Key(Token, attach) == false)
                    {
                        throw new Exception(String.Format("Could not update Picture: {0} ({1})", store.File_Name, store.Attach_ID));
                    }
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



        #region PictureAddClipCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: PictureAddClipCommand</summary>
        public DelegateCommand PictureAddClipCommand { get; set; }
        private Boolean CanCommandPictureAddClip()
        {
            if (BugObject == null) return false;
            if (BugObject.Bug_ID == 0) return false;
            return Clipboard.ContainsImage();
        }

        private void CommandPictureAddClip()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                FileInfo fi = Format_Cache_Filename(GUID_Helper.Encode(Guid.NewGuid()) + ".png");
                if (fi.Directory.Exists == false) fi.Directory.Create();
                else if (fi.Exists == true) throw new Exception("Duplicate Unique file name");

                Image_Helper.SaveClipboardImageToFile(fi.FullName);
                fi.Refresh();

                Entry_Window ew = new Entry_Window();
                ew.dp_LabelText = " Enter Description ";
                if (ew.ShowDialog() == true)
                {
                    Attachments att = new Attachments()
                    {
                        Bug_ID = BugObject.ID,
                        Creation_TS = DateTime.Now,
                        Description = ew.dp_TextBoxText,
                        File_Name = fi.Name,
                        Mime_Type = String.Format("image/{0}", Format_Image_Type(fi.Name)),
                        Submitter_ID = User_Profile.User_ID,
                        IS_Obsolete = false,
                        IS_Private = false,
                        IS_Url = false,
                        IS_Patch = false
                    };


                    var attach = BugZilla_Web_Service_Client.Insert_Attachments_Key(Token, att);
                    if (attach == null) throw new Exception(String.Format("Could not Insert Picture: {0}", att.File_Name));

                    Attach_Data ad = new Attach_Data();
                    ad.ID = attach.Attach_ID;
                    ad.The_Data = Image_Helper.ImageFileToBuffer(fi.FullName);

                    BugZilla_Web_Service_Client.Upload_Attach_Data(Token, ad);

                    Add_To_Picture_List(attach);
                    Selected_Picture = attach;
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

        private void Add_To_Picture_List(Attachments attach)
        {
            List<Attachments> attList = new List<Attachments>(Picture_List);
            attList.Add(attach);
            attList.Sort(new Attachments_Comparer());

            Application.Current.Dispatcher.Invoke((Action)delegate ()
            {
                Picture_List = new ObservableCollection<Attachments>(attList);
            });
        }

        /////////////////////////////////////////////
        #endregion



        #region UploadLogCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: UploadLogCommand</summary>
        public DelegateCommand UploadLogCommand { get; set; }
        private Boolean CanCommandUploadLog() { return true; }
        private void CommandUploadLog()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Thread back = new Thread(new ThreadStart(CommandUploadLog_Back));
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


        #region SubmitCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: SubmitCommand</summary>
        public DelegateCommand SubmitCommand { get; set; }
        private Boolean CanCommandSubmit()
        {
            if (BugObject == null) return false;
            if (BugObject.ID != 0) return true;
            if (Comment_Count == 0) return false;
            if (String.IsNullOrEmpty(Application_Name) == true) return false;
            if (String.IsNullOrEmpty(Selected_Component) == true) return false;
            if (String.IsNullOrEmpty(Version_Number) == true) return false;
            if (String.IsNullOrEmpty(Frequency) == true) return false;
            if (String.IsNullOrEmpty(Selected_Type) == true) return false;
            if (String.IsNullOrEmpty(Selected_Severity) == true) return false;
            if (String.IsNullOrEmpty(Summary) == true) return false;
            if (String.IsNullOrEmpty(Status) == true) return false;
            if (String.IsNullOrEmpty(Selected_Priority) == true) return false;
            if (BugObject.Bug_Type == "Bug")
            {
                if (BugObject.Log_Loaded.HasValue == false) return false;
                return BugObject.Log_Loaded.Value;
            }
            return true;
        }

        private void CommandSubmit()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Thread back = new Thread(new ThreadStart(CommandSubmit_Back));
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



        #region BugStatusStartCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BugStatusStartCommand</summary>
        public DelegateCommand BugStatusStartCommand { get; set; }
        private Boolean CanCommandBugStatusStart()
        {
            if (BugObject == null) return false;
            if (BugObject.Status == "ASSIGNED") return true;
            return false;
        }

        private void CommandBugStatusStart()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Thread back = new Thread(new ThreadStart(CommandBugStatusStart_Back));
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


        #region BugStatusAcceptCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BugStatusAcceptCommand</summary>
        public DelegateCommand BugStatusAcceptCommand { get; set; }
        private Boolean CanCommandBugStatusAccept()
        {
            if (BugObject == null) return false;
            if (Status != "NEW") return false;
            return true;
        }

        private void CommandBugStatusAccept()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Thread back = new Thread(new ThreadStart(CommandBugStatusAccept_Back));
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



        #region BugStatusResolveCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BugStatusResolveCommand</summary>
        public DelegateCommand BugStatusResolveCommand { get; set; }
        private Boolean CanCommandBugStatusResolve()
        {
            if (BugObject == null) return false;
            if (Status == "STARTED") return true;
            //if (Status == "REOPENED") return true;
            return false;
        }

        private void CommandBugStatusResolve()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Thread back = new Thread(new ThreadStart(CommandBugStatusResolve_Back));
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




        #region BugStatusReOpenCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BugStatusReOpenCommand</summary>
        public DelegateCommand BugStatusReOpenCommand { get; set; }
        private Boolean CanCommandBugStatusReOpen()
        {
            if (BugObject == null) return false;
            if (Status == "RESOLVED") return true;
            //if (Status == "VERIFIED") return true;
            return false;
        }

        private void CommandBugStatusReOpen()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Entry_Window ew = new Entry_Window();
                ew.dp_LabelText = "Please enter a reason";
                ew.Width = 1000;
                if (ew.ShowDialog() == true)
                {
                    Thread back = new Thread(new ParameterizedThreadStart(CommandBugStatusReOpen_Back));
                    back.IsBackground = true;
                    back.Start(ew.dp_TextBoxText);
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



        #region BugStatusVerifyCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BugStatusVerifyCommand</summary>
        public DelegateCommand BugStatusVerifyCommand { get; set; }
        private Boolean CanCommandBugStatusVerify()
        {
            if (BugObject == null) return false;
            if (Status == "RESOLVED") return true;
            return false;
        }
        private void CommandBugStatusVerify()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Thread back = new Thread(new ThreadStart(CommandBugStatusVerify_Back));
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



        #region BugStatusCloseCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BugStatusCloseCommand</summary>
        public DelegateCommand BugStatusCloseCommand { get; set; }
        private Boolean CanCommandBugStatusClose()
        {
            if (BugObject == null) return false;
            return Status == "VERIFIED";
        }

        private void CommandBugStatusClose()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Thread back = new Thread(new ThreadStart(CommandBugStatusClose_Back));
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



        #region BugStatusRejectCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: BugStatusRejectCommand</summary>
        public DelegateCommand BugStatusRejectCommand { get; set; }
        private Boolean CanCommandBugStatusReject()
        {
            if (BugObject == null) return false;
            if (Status == "NEW") return true;
            return false;
        }

        private void CommandBugStatusReject()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Entry_Window ew = new Entry_Window(Window_Add_Types.CustomList);
                var resList = BugZilla_Web_Service_Client.Select_Resolution_Criteria(Token, null);
                if (resList != null)
                {
                    ew.dp_ComboBoxList.Clear();
                    foreach (var res in resList)
                    {
                        ew.dp_ComboBoxList.Add(res.Value);
                    }
                }

                if (ew.ShowDialog() == true)
                {
                    Thread back = new Thread(new ParameterizedThreadStart(CommandBugStatusReject_Back));
                    back.IsBackground = true;
                    back.Start(ew.dp_Selected_Combo);
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
            _LoginContext = null;
            _Token = null;

            Thread back = new Thread(new ThreadStart(Load_Field_IDs));
            back.IsBackground = true;
            back.Start();


            Thread compback = new Thread(new ThreadStart(Load_Components));
            compback.IsBackground = true;
            compback.Start();


            Thread sevback = new Thread(new ThreadStart(Load_Severities));
            sevback.IsBackground = true;
            sevback.Start();

        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Bug_Open_Handler  -- Event: Bug_Open_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Bug_Open_Handler(Bugs_Tracker bug)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                BugObject = bug;

                Comment_List.Clear();


                Thread back = new Thread(new ThreadStart(Load_Product_Components));
                back.IsBackground = true;
                back.Start();

                if (bug.ID > 0)
                {
                    var origBug = BugZilla_Web_Service_Client.Select_Bugs_Key(Token, bug.Bug_ID);
                    if (bug != null) BugObject.Bug = origBug;

                    var origAdm = BugZilla_Web_Service_Client.Select_Bugs_Admin_Key(Token, bug.Bug_ID);
                    if (origAdm == null)
                    {
                        origAdm = new Bugs_Admin() { Bug_ID = bug.Bug_ID };
                        origAdm = BugZilla_Web_Service_Client.Insert_Bugs_Admin_Key(Token, origAdm);
                        if (origAdm == null) throw new Exception(String.Format("Unable to retrieve Bug: {0}", bug.Bug_ID));
                    }
                    BugObject.BugsAdmin = origAdm;

                    var descs = BugZilla_Web_Service_Client.Select_Longdescs_BUG_ID(Token, bug.ID);
                    if (descs != null)
                    {
                        foreach (var desc in descs)
                        {
                            Comment_Object co = new Comment_Object()
                            {
                                Comment = desc.The_Text,
                                ID = desc.Bug_ID,
                                Date_Time_Entered = desc.Bug_When,
                            };

                            co.User_Name = Get_User_Profile_Name(desc.Who);
                            Comment_List.Add(co);
                        }
                    }
                }

                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    Comment_Control cc = (Comment_Control)((Bug_Detail_Control)View).FindName("Bug_Conversations");
                    if (cc != null)
                    {
                        cc.Clear();
                        foreach (var comm in Comment_List)
                        {
                            eventAggregator.GetEvent<Comment_Create_Event>().Publish(comm);
                        }
                    }
                });


                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    Picture_List.Clear();
                });

                if (bug.ID > 0)
                {
                    var listPic = BugZilla_Web_Service_Client.Select_Attachments_BUG_ID(Token, bug.ID);
                    if (listPic != null)
                    {
                        listPic.Sort(new Attachments_Comparer());

                        Application.Current.Dispatcher.Invoke((Action)delegate ()
                        {
                            Picture_List = new ObservableCollection<Attachments>(listPic);
                        });
                    }
                }
                Selected_Picture = null;

                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("BD");
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

        /// <summary>Public Static Method - Get_User_Profile_Name</summary>
        /// <param name="submitter_ID">int</param>
        /// <returns>String</returns>
        public static String Get_User_Profile_Name(int submitter_ID)
        {
            if (Profile_List.ContainsKey(submitter_ID) == true) return Profile_List[submitter_ID].Login_Name;
            var prof = BugZilla_Web_Service_Client.Select_Profiles_Key(Token, submitter_ID);
            if (prof != null)
            {
                Profile_List[submitter_ID] = prof;
                return Profile_List[submitter_ID].Login_Name;
            }
            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Activated_Handler  -- Event: Application_Activated_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Activated_Handler(Boolean active)
        {
            if (active == true) PictureAddClipCommand.RaiseCanExecuteChanged();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Bug_Menu_Detail_Activate_Handler  -- Event: Bug_Menu_Detail_Activate_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Bug_Menu_Detail_Activate_Handler(Boolean activate)
        {
            for (int x = 0; x < 10; x++) if (BugObject != null) break; else Thread.Sleep(10);
            if (BugObject == null)
            {
                eventAggregator.GetEvent<ToolBar_Disable_Item_Event>().Publish("BD");
            }
            else
            {
                eventAggregator.GetEvent<ToolBar_Enable_Item_Event>().Publish("BD");
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
            //eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            //eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(false);
        }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);
            Refresh();
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods


        private void Load_Severities()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);


                List<String> items = BugZilla_Web_Service_Client.Select_BugZilla_Severities(Token);
                if ((items != null) && (items.Count > 0))
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        if (_Severity_List == null) _Severity_List = new ObservableCollection<String>();
                        else _Severity_List.Clear();
                        foreach (String item in items)
                        {
                            if (String.Compare(item, "enhancement", true) == 0) continue;
                            _Severity_List.Add(item);
                        }
                    });
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
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }

        }


        private void Load_Components()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);


                List<String> items = BugZilla_Web_Service_Client.Select_BugZilla_Components(Token, Application_Name);
                if ((items != null) && (items.Count > 0))
                {
                    items.Sort();

                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        if (_Components == null) _Components = new ObservableCollection<String>();
                        else _Components.Clear();
                        foreach (String item in items) _Components.Add(item);
                    });
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
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }

        }

        private void Load_Field_IDs()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                Field_Definitions = BugZilla_Web_Service_Client.Select_Fielddefs_Criteria(Token, null);
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


        private void CommandPictureAddFile_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                //throw new NotImplementedException();
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


        private void CommandSubmit_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                if (BugObject.Reporter == 0) BugObject.Reporter = User_Profile.User_ID;


                if ((BugObject.Product_ID.HasValue == false) || (BugObject.Product_ID == 0))
                {
                    var prod = BugZilla_Web_Service_Client.Select_Products_NAME(Token, BugObject.Application);
                    if (prod == null) throw new Exception(String.Format("Application: {0} was not recognized.", BugObject.Application));
                    BugObject.Product_ID = prod.ID;
                }

                //Components Component = null;
                foreach (var component in Product_Components)
                {
                    if (component.Name == BugObject.Component)
                    {
                        BugObject.Component_ID = component.ID;
                        //Component = component;
                        break;
                    }
                }

                if (BugObject.ID == 0)
                {
                    if (String.IsNullOrEmpty(BugObject.Status) == true) BugObject.Status = "NEW";
                    if (BugObject.Resolution == null) BugObject.Resolution = "";

                    Bug.Objects.Bugs obj = BugObject.Bug;

                    logger.Debug("Product: {0}, Version: {1}", obj.Product_ID, obj.Version);
                    if (BugZilla_Web_Service_Client.Select_Versions_PRODUCT_ID_VERSION(Token, obj.Product_ID, obj.Version) == false)
                    {
                        var versions = BugZilla_Web_Service_Client.Select_Versions_PRODUCT_ID(Token, obj.Product_ID);
                        if ((versions == null) || (versions.Count == 0))
                        {
                            Versions v = new Versions() { Value = obj.Version, Product_ID = obj.Product_ID };
                            BugZilla_Web_Service_Client.Insert_Versions_Key(Token, v);
                        }
                        else
                        {
                            Boolean found = false;
                            foreach (var ver in versions)
                            {
                                if (ver.Value == obj.Version)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found == false)
                            {
                                Versions v = new Versions() { Value = obj.Version, Product_ID = obj.Product_ID };
                                BugZilla_Web_Service_Client.Insert_Versions_Key(Token, v);
                            }
                        }
                    }

                    if (obj.Assigned_TO == 0)
                    {
                        if (obj.Component_ID != 0)
                        {
                            var comp = BugZilla_Web_Service_Client.Select_Components_Key(Token, obj.Component_ID);
                            if (comp != null)
                            {
                                obj.Assigned_TO = comp.Initial_Owner;
                                //Component = comp;
                            }
                        }
                    }

                    var newobj = BugZilla_Web_Service_Client.Insert_Bugs_Key(Token, obj);
                    if (newobj == null) throw new Exception(String.Format("Unable to Insert Bugs object: {0}", obj.Short_Desc));
                    logger.Debug("Successfully Insert new Bugs object: {0}", newobj.Bug_ID);

                    BugObject.Bug = newobj;

                    Bugs_Admin ba = BugObject.BugsAdmin;

                    var oldBA = BugZilla_Web_Service_Client.Select_Bugs_Admin_Key(Token, ba.Bug_ID);
                    if (oldBA == null)
                    {
                        oldBA = BugZilla_Web_Service_Client.Insert_Bugs_Admin_Key(Token, ba);
                        if (oldBA == null)
                        {
                            throw new Exception(String.Format("Unable to Insert Bugs_Admin object: {0}", ba.Bug_ID));
                        }
                    }
                    else
                    {
                        oldBA.Update(ba);
                        if (BugZilla_Web_Service_Client.Update_Bugs_Admin_Key(Token, oldBA) == false)
                        {
                            throw new Exception(String.Format("Unable to Update Bugs_Admin object: {0}", ba.Bug_ID));
                        }
                    }
                }
                else
                {
                    var bug = BugObject.Bug;


                    if (bug.Assigned_TO == 0)
                    {
                        if (bug.Component_ID != 0)
                        {
                            var comp = BugZilla_Web_Service_Client.Select_Components_Key(Token, bug.Component_ID);
                            if (comp != null)
                            {
                                bug.Assigned_TO = comp.Initial_Owner;
                                //Component = comp;
                            }
                        }
                    }

                    var oldBug = BugZilla_Web_Service_Client.Select_Bugs_Key(Token, bug.Bug_ID);
                    if (oldBug == null)
                    {
                        throw new Exception(String.Format("Unable to retrieve Bugs object: {0}", BugObject.ID));
                    }
                    oldBug.Update(bug);
                    if (BugZilla_Web_Service_Client.Update_Bugs_Key(Token, oldBug) == false)
                    {
                        throw new Exception(String.Format("Unable to Update Bugs object: {0}", oldBug.Bug_ID));
                    }

                    var ba = BugObject.BugsAdmin;
                    var oldba = BugZilla_Web_Service_Client.Select_Bugs_Admin_Key(Token, ba.Bug_ID);
                    if (oldba == null)
                    {
                        throw new Exception(String.Format("Unable to retrieve Bugs_Admin object: {0}", ba.Bug_ID));
                    }
                    oldba.Update(ba);
                    if (BugZilla_Web_Service_Client.Update_Bugs_Admin_Key(Token, oldba) == false)
                    {
                        throw new Exception(String.Format("Unable to Update Bugs_Admin object: {0}", ba.Bug_ID));
                    }
                }

                Save_Descriptions();

                foreach (var picture in Picture_List)
                {
                    SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                    criteria.Add(new FieldData("File_Name", picture.File_Name));
                    List<Attachments> attachs = BugZilla_Web_Service_Client.Select_Attachments_Criteria(Token, criteria);
                    if ((attachs != null) && (attachs.Count != 0)) continue;


                    var attach = BugZilla_Web_Service_Client.Insert_Attachments_Key(Token, picture);
                    if (attach == null) throw new Exception(String.Format("Could not Insert Picture: {0}", picture.File_Name));

                    Attach_Data ad = new Attach_Data();
                    ad.ID = attach.Attach_ID;
                    ad.The_Data = Image_Helper.ImageFileToBuffer(Format_Cache_Filename(picture.File_Name));

                    BugZilla_Web_Service_Client.Upload_Attach_Data(Token, ad);
                }

                if (BugObject.Status == "NEW")
                {
                    SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                    criteria.Add(new FieldData("Bug_ID", BugObject.Bug_ID));
                    criteria.Add(new FieldData("Added", "NEW"));
                    var acts = BugZilla_Web_Service_Client.Select_Bugs_Activity_Criteria(Token, criteria);
                    if ((acts == null) || (acts.Count == 0))
                    {
                        Bugs_Activity ba = new Bugs_Activity()
                        {
                            Bug_ID = BugObject.Bug_ID,
                            Added = "NEW",
                            Bug_When = BugObject.Bug.Creation_TS.HasValue ? BugObject.Bug.Creation_TS.Value : DateTime.Now,
                            Field_ID = 9,
                            Who = User_Profile.User_ID
                        };

                        BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, ba);
                        BugZilla_Web_Service_Client.Send_Notification(Token, BugObject.Bug_ID, User_Profile.User_ID);
                    }
                }


                eventAggregator.GetEvent<Bug_Refresh_List_Event>().Publish(true);
                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("BL");
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


        private String Format_Image_Type(String unique_Name)
        {
            if (String.IsNullOrEmpty(unique_Name)) throw new ArgumentNullException("Parameter unique_Name (String) can not be null or empty.");
            String ext = Path.GetExtension(unique_Name);
            if (ext.StartsWith(".") == true) ext = ext.Substring(1);
            if (String.Compare(ext, "png", true) == 0) return ext;
            if (String.Compare(ext, "jpg", true) == 0) return "jpeg";
            return ext;
        }

        private void CommandPictureOpen_Back()
        {
            eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
            eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
            //Application_Helper.DoEvents();

            try
            {
                logger.Trace("Entered");

                FileInfo fi = Format_Cache_Filename(Selected_Picture.File_Name);
                if (fi.Exists == false)
                {
                    logger.Debug("Picture: {0} does not exist in cache", fi.FullName);
                    if (Selected_Picture.Attach_ID != 0)
                    {
                        Attach_Data ad = BugZilla_Web_Service_Client.Download_Attach_Data(InfrastructureModule.Token, Selected_Picture.Attach_ID);
                        if (ad != null)
                        {
                            Byte[] picdata = ad.The_Data;
                            if (picdata != null)
                            {
                                logger.Debug("Writing picture to cache");
                                Write_Attachment_Data(picdata, fi);
                                fi.Refresh();
                            }
                        }
                    }
                    Attachments store = Selected_Picture;

                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        Picture_List.Remove(Selected_Picture);
                    });

                    Add_To_Picture_List(store);
                }

                if (fi.Exists == true) ProcessHelper.Run(fi.FullName, null, 0, true, false, false, false);
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Pink);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error: Unable to upload picture(s)");
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                //Application_Helper.DoEvents();
                logger.Trace("Leaving");
            }
        }

        private void Write_Attachment_Data(byte[] picdata, FileInfo fi)
        {
            fi.Refresh();
            if (fi.Directory.Exists == false) fi.Directory.Create();
            else if (fi.Exists == true) fi.Delete();
            using (FileStream fs = fi.OpenWrite())
            {
                fs.Write(picdata, 0, picdata.Length);
            }
        }

        private void CommandPictureDel_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Removing Picture: {0}", Selected_Picture.Description));

                BugZilla_Web_Service_Client.Delete_Attachments_Key(Token, Selected_Picture.Attach_ID);
                BugZilla_Web_Service_Client.Delete_Attach_Data_Key(Token, Selected_Picture.Attach_ID);

                FileInfo fi = Format_Cache_Filename(Selected_Picture);
                if (fi.Exists) fi.Delete();

                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    Picture_List.Remove(Selected_Picture);
                });
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = Brushes.Pink;
                PopupMessage = msg;
                PopupOpen = true;
                eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Pink);
                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error: Unable to delete picture(s)");
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private FileInfo Format_Cache_Filename(Attachments selected_Picture)
        {
            return Format_Cache_Filename(selected_Picture.File_Name);
        }

        private FileInfo Format_Cache_Filename(String fileName)
        {
            String pth = Path.Combine(Application_Helper.Application_Folder_Name(), "Cache", fileName);
            return new FileInfo(pth);
        }


        private void Refresh()
        {
            OnPropertyChanged("Application_Name");
            OnPropertyChanged("Comment_Count");
            OnPropertyChanged("Components");
            OnPropertyChanged("Frequency");
            OnPropertyChanged("Issue_ID");
            OnPropertyChanged("Occurrance_DateTime");
            OnPropertyChanged("Picture_List");
            OnPropertyChanged("Selected_Component");
            OnPropertyChanged("Selected_Picture");
            OnPropertyChanged("Selected_Severity");
            OnPropertyChanged("Selected_Type");
            OnPropertyChanged("Severity_List");
            OnPropertyChanged("Summary");
            OnPropertyChanged("UserName");
            OnPropertyChanged("Version_Number");
            OnPropertyChanged("Work_Order");
            OnPropertyChanged("Priority_List");
            OnPropertyChanged("Selected_Priority");
            OnPropertyChanged("Status");
            OnPropertyChanged("Bug_Statuses");
            OnPropertyChanged("Master_Visibility");
            OnPropertyChanged("Verify_Visibility");
            OnPropertyChanged("Implemented_Version");
            OnPropertyChanged("Revision");
            OnPropertyChanged("Work_Completed");
            OnPropertyChanged("Work_Started");

            BugStatusAcceptCommand.RaiseCanExecuteChanged();
            BugStatusCloseCommand.RaiseCanExecuteChanged();
            BugStatusRejectCommand.RaiseCanExecuteChanged();
            BugStatusReOpenCommand.RaiseCanExecuteChanged();
            BugStatusResolveCommand.RaiseCanExecuteChanged();
            BugStatusStartCommand.RaiseCanExecuteChanged();
            BugStatusVerifyCommand.RaiseCanExecuteChanged();
            PictureOpenCommand.RaiseCanExecuteChanged();
            PictureEditCommand.RaiseCanExecuteChanged();
            PictureAddClipCommand.RaiseCanExecuteChanged();
            PictureAddFileCommand.RaiseCanExecuteChanged();
            PictureDelCommand.RaiseCanExecuteChanged();
        }



        private void CommandPictureAddClip_Back(Object obj)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                FileInfo fi = (FileInfo)obj;
                if (fi.Exists == true)
                {
                    Byte[] buff = Image_Helper.ImageFileToBuffer(fi);
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
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void CommandUploadLog_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                String fname = Path.Combine(Application_Helper.Application_Folder_Name(), "Logs", String.Format("{0}.log", DateTime.Today.ToString("yyyy-MM-dd")));
                if (File.Exists(fname) == true)
                {
                    Runtime_Logs_Web_Service_Client.Upload_Logs(LoginContext.UserName, DisplayMessage, fname);
                    BugObject.Log_Loaded = true;
                    SubmitCommand.RaiseCanExecuteChanged();
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
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void DisplayMessage(String msg)
        {
            eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
        }



        private int Find_Field(String fieldname)
        {
            foreach (var field in Field_Definitions)
            {
                if (field.Name == fieldname) return field.Field_ID;
            }
            return 0;
        }



        private void CommandBugStatusAccept_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                Bugs_Activity baStatus = new Bugs_Activity()
                {
                    Removed = Status,
                    Added = "ASSIGNED",
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("bug_status")
                };

                Status = baStatus.Added;
                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baStatus);
                Thread.Sleep(1000);

                Bugs_Activity baAssign = new Bugs_Activity()
                {
                    Removed = BugObject.Bug.Assigned_TO.ToString(),
                    Added = User_Profile.User_ID.ToString(),
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("assigned_to")
                };

                BugObject.Bug.Assigned_TO = baAssign.Who;
                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baAssign);

                BugZilla_Web_Service_Client.Update_Bugs_Key(Token, BugObject.Bug);

                BugZilla_Web_Service_Client.Send_Notification(Token, BugObject.Bug_ID, User_Profile.User_ID);

                BugObject = null;

                eventAggregator.GetEvent<Bug_Refresh_List_Event>().Publish(true);
                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("BL");
                //Application.Current.Dispatcher.Invoke((Action)delegate ()
                //{
                //    regionManager.RequestNavigate(RegionNames.MainRegion, Bug_List_Control.Control_Name);
                //});

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


        private void CommandBugStatusResolve_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                Bugs_Activity baStatus = new Bugs_Activity()
                {
                    Removed = Status,
                    Added = "RESOLVED",
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("bug_status")
                };

                Status = baStatus.Added;
                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baStatus);
                Thread.Sleep(1000);


                Bugs_Activity baResolve = new Bugs_Activity()
                {
                    Removed = BugObject.Resolution,
                    Added = "FIXED",
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("resolution")
                };

                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baResolve);

                BugObject.Resolution = baResolve.Added;
                BugZilla_Web_Service_Client.Update_Bugs_Key(Token, BugObject.Bug);

                Work_Completed = DateTime.Now;
                Implemented_Version = Get_Application_Version(BugObject.Application);
                Revision = Get_Revision_Number();
                BugZilla_Web_Service_Client.Update_Bugs_Admin_Key(Token, BugObject.BugsAdmin);


                BugZilla_Web_Service_Client.Send_Notification(Token, BugObject.Bug_ID, User_Profile.User_ID);

                BugObject = null;

                eventAggregator.GetEvent<Bug_Refresh_List_Event>().Publish(true);
                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("BL");
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

        private String Get_Application_Version(String v)
        {
            if (Dev_Defaults == null) return "";

            String field = String.Format("{0}_Version_Source", v.Replace(" ", "_"));
            PropertyInfo pi = typeof(Developer_Defaults).GetProperty(field);
            if (pi != null)
            {
                Object obj = pi.GetValue(Dev_Defaults, null);
                if (obj != null)
                {
                    FileInfo fi = new FileInfo(obj.ToString());
                    if (fi.Exists == true)
                    {
                        using (TextReader tr = fi.OpenText())
                        {
                            while (true)
                            {
                                String line = tr.ReadLine();
                                if (line == null) break;
                                if (line.StartsWith("[assembly: AssemblyVersion(") == true)
                                {
                                    var parts = Strings.Split(line, "\"");
                                    if (parts.Length == 3)
                                    {
                                        logger.Debug("Version Number: {0}", parts[1]);
                                        return parts[1];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return "";
        }


        private int Get_Revision_Number()
        {
            try
            {
                if (Dev_Defaults != null)
                {
                    ProcessHelper.Run("svn", Dev_Defaults.SubVersion_Revision_Command.Substring(4), 20, true, true, false, false);
                    using (TextReader tr = new StringReader(ProcessHelper.StdOutput))
                    {
                        while (true)
                        {
                            String line = tr.ReadLine();
                            if (line == null) break;
                            if (line.StartsWith("Last Changed Rev:") == true)
                            {
                                String rev = line.Substring(17).Trim();
                                logger.Debug("Revision: {0}", rev);
                                return int.Parse(rev);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
            }
            return 0;
        }

        private void CommandBugStatusReOpen_Back(Object obj)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                String msg = obj.ToString();

                Comment_Object co = new Comment_Object()
                {
                    Comment = "Reason for Rejecting Resolve:" + msg,
                    Date_Time_Entered = DateTime.Now,
                    User_Name = LoginContext.UserName
                };

                Comment_Control cc = null;

                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    cc = (Comment_Control)((Bug_Detail_Control)View).FindName("Bug_Conversations");
                    cc.dp_Comment_List.Add(co);
                });

                Save_Descriptions();

                Bugs_Activity baStatus = new Bugs_Activity()
                {
                    Removed = Status,
                    Added = "REOPENED",
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("bug_status")
                };

                Status = baStatus.Added;
                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baStatus);
                Thread.Sleep(1000);


                Bugs_Activity baRes = new Bugs_Activity()
                {
                    Removed = BugObject.Resolution,
                    Added = null,
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("resolution")
                };

                BugObject.Resolution = null;
                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baRes);

                BugZilla_Web_Service_Client.Update_Bugs_Key(Token, BugObject.Bug);
                BugZilla_Web_Service_Client.Send_Notification(Token, BugObject.Bug_ID, User_Profile.User_ID);

                BugObject = null;

                eventAggregator.GetEvent<Bug_Refresh_List_Event>().Publish(true);
                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("BL");
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



        private void CommandBugStatusClose_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);


                Bugs_Activity baStatus = new Bugs_Activity()
                {
                    Removed = Status,
                    Added = "CLOSED",
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("bug_status")
                };

                Status = baStatus.Added;
                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baStatus);

                BugZilla_Web_Service_Client.Update_Bugs_Key(Token, BugObject.Bug);
                BugZilla_Web_Service_Client.Send_Notification(Token, BugObject.Bug_ID, User_Profile.User_ID);

                eventAggregator.GetEvent<Bug_Refresh_List_Event>().Publish(true);
                BugObject = null;
                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("BL");
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


        private void CommandBugStatusVerify_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                Bugs_Activity baStatus = new Bugs_Activity()
                {
                    Removed = Status,
                    Added = "VERIFIED",
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("bug_status")
                };

                Status = baStatus.Added;
                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baStatus);

                BugZilla_Web_Service_Client.Update_Bugs_Key(Token, BugObject.Bug);
                BugZilla_Web_Service_Client.Send_Notification(Token, BugObject.Bug_ID, User_Profile.User_ID);

                BugObject = null;

                eventAggregator.GetEvent<Bug_Refresh_List_Event>().Publish(true);
                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("BL");
                //Application.Current.Dispatcher.Invoke((Action)delegate ()
                //{
                //    regionManager.RequestNavigate(RegionNames.MainRegion, Bug_List_Control.Control_Name);
                //});
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

        private void Mouse_Cursor_Busy_On()
        {
            eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
        }

        private void Mouse_Cursor_Busy_Off()
        {
            eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
        }

        private void Load_Product_Components()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                int productID = 0;
                if ((BugObject.Product_ID.HasValue == false) || (BugObject.Product_ID.Value == 0))
                {
                    if (String.IsNullOrEmpty(BugObject.Application) == false)
                    {
                        var product = BugZilla_Web_Service_Client.Select_Products_NAME(Token, BugObject.Application);
                        productID = product.ID;
                    }
                }
                else productID = BugObject.Product_ID.Value;
                Product_Components = BugZilla_Web_Service_Client.Select_Components_PRODUCT_ID(Token, (Int16)productID);
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

        private void CommandBugStatusStart_Back()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);



                //Bugs_Activity baVersion = new Bugs_Activity()
                //{
                //    Removed = Version_Number,
                //    Added = Application_Helper.Version(),
                //    Bug_ID = BugObject.ID,
                //    Bug_When = DateTime.Now,
                //    Who = User_Profile.User_ID,
                //    Field_ID = Find_Field("version")
                //};

                //Version_Number = baVersion.Added;
                //BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baVersion);
                //Thread.Sleep(1000);




                Bugs_Activity baStatus = new Bugs_Activity()
                {
                    Removed = Status,
                    Added = "STARTED",
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("bug_status")
                };

                Status = baStatus.Added;
                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baStatus);
                Thread.Sleep(1000);

                Work_Started = DateTime.Now;

                BugZilla_Web_Service_Client.Update_Bugs_Key(Token, BugObject.Bug);
                BugZilla_Web_Service_Client.Update_Bugs_Admin_Key(Token, BugObject.BugsAdmin);
                BugZilla_Web_Service_Client.Send_Notification(Token, BugObject.Bug_ID, User_Profile.User_ID);

                BugObject = null;

                eventAggregator.GetEvent<Bug_Refresh_List_Event>().Publish(true);
                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("BL");
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


        private void Save_Descriptions()
        {
            Comment_Control cc = null;

            Application.Current.Dispatcher.Invoke((Action)delegate ()
            {
                cc = (Comment_Control)((Bug_Detail_Control)View).FindName("Bug_Conversations");
            });
            if (cc != null)
            {
                List<Comment_Object> comments = null;
                Application.Current.Dispatcher.Invoke((Action)delegate ()
                {
                    comments = new List<Comment_Object>(cc.dp_Comment_List);
                });

                logger.Debug("Inserting new conversations");
                Boolean inserted = false;
                foreach (var comment in comments)
                {
                    var b = BugZilla_Web_Service_Client.Select_Longdescs_Key(Token, comment.Date_Time_Entered.Value);
                    if (b != null)
                    {
                        if (b.Bug_ID == comment.ID)
                        {
                            continue;
                        }
                        logger.Debug("A conversation item from another bug already exists at the same Date/Time: {0}", comment.Date_Time_Entered);
                        while (true)
                        {
                            comment.Date_Time_Entered = DateTime.Now;
                            b = BugZilla_Web_Service_Client.Select_Longdescs_Key(Token, comment.Date_Time_Entered.Value);
                            if (b == null)
                            {
                                break;
                            }
                        }
                    }
                    Longdescs mcc = new Longdescs()
                    {
                        Bug_ID = comment.ID,
                        Bug_When = comment.Date_Time_Entered.Value,
                        Who = User_Profile.User_ID,
                        The_Text = comment.Comment
                    };

                    if (mcc.Bug_ID == 0) mcc.Bug_ID = BugObject.ID;
                    BugZilla_Web_Service_Client.Insert_Longdescs_Key(Token, mcc);
                    inserted = true;
                }


                if ((inserted == true) && ((BugObject.Status != "NEW") || (comments.Count > 1)))
                {
                    BugZilla_Web_Service_Client.Send_Discussion_Notification(Token, BugObject.ID, User_Profile.User_ID);
                }
            }
        }


        private void CommandBugStatusReject_Back(Object res)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);

                if ((res == null) || ((res is String) == false)) return;

                Bugs_Activity baStatus = new Bugs_Activity()
                {
                    Removed = Status,
                    Added = "CLOSED",
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("bug_status")
                };

                Status = baStatus.Added;
                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baStatus);
                Thread.Sleep(1000);

                Bugs_Activity baResolve = new Bugs_Activity()
                {
                    Removed = BugObject.Resolution,
                    Added = res.ToString(),
                    Bug_ID = BugObject.ID,
                    Bug_When = DateTime.Now,
                    Who = User_Profile.User_ID,
                    Field_ID = Find_Field("resolution")
                };

                BugObject.Resolution = res.ToString();
                BugZilla_Web_Service_Client.Insert_Bugs_Activity_Key(Token, baResolve);
                Thread.Sleep(1000);

                BugZilla_Web_Service_Client.Update_Bugs_Key(Token, BugObject.Bug);
                BugZilla_Web_Service_Client.Send_Notification(Token, BugObject.Bug_ID, User_Profile.User_ID);

                BugObject = null;

                eventAggregator.GetEvent<Bug_Refresh_List_Event>().Publish(true);
                eventAggregator.GetEvent<ToolBar_Activate_Menu_Event>().Publish("BL");
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
