using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Unity;
using NLog;
using SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.RT.Logs.Client.REST;
using Segway.Service.AppSettings.Helper;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.Modules.AddWindow;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace Segway.SART2012
{
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class Shell : Window
    {
        private static Logger logger = Logger_Helper.GetCurrentLogger();

        private IUnityContainer _container;
        private IEventAggregator eventAggregator;

        Boolean IsRightControlKeyPressed = false;
        Boolean IsRightShiftKeyPressed = false;
        private Boolean UploadingLogs = false;


        public Shell(IUnityContainer container, IEventAggregator aggregator)
        {
            InitializeComponent();
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                _container = container;
                eventAggregator = aggregator;
                logger.Debug("SART application starting up");
                //this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;



                #region Event Subscriptions

                eventAggregator.GetEvent<Shell_Minimize_Event>().Subscribe(Minimize, true);
                eventAggregator.GetEvent<Shell_Maximize_Event>().Subscribe(Maximize, true);
                eventAggregator.GetEvent<Shell_Restore_Event>().Subscribe(Restore, true);
                eventAggregator.GetEvent<Shell_Close_Event>().Subscribe(Close, true);
                eventAggregator.GetEvent<Shell_Drag_Event>().Subscribe(Drag, true);
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Subscribe(BusyMouseCursorEventHandler, ThreadOption.UIThread);

                #endregion

                #region Command Delegates

                #endregion

                WindowState ws = WindowState.Normal;
                container.RegisterInstance<WindowState>("ApplicationWindowState", ws, new ContainerControlledLifetimeManager());


                String appWindowLoc = Configuration_Helper.GetConfigurationValue("ApplicationWindowStartLoc", "CenterScreen");
                WindowStartupLocation loc = WindowStartupLocation.CenterScreen;
                if (Enum.TryParse<WindowStartupLocation>(appWindowLoc, out loc) == true)
                {
                    mainWindow.WindowStartupLocation = loc;
                }

                String appState = Configuration_Helper.GetConfigurationValue("ApplicationState", "Normal");

                //if (appState == "Normal")
                //{
                String appWidth = Configuration_Helper.GetConfigurationValue("ApplicationWidth", "1500");
                Double width = 0.0;
                if (Double.TryParse(appWidth, out width) == true)
                {
                    mainWindow.Width = width;
                }
                String appHeight = Configuration_Helper.GetConfigurationValue("ApplicationHeight", "850");
                Double height = 0.0;
                if (Double.TryParse(appHeight, out height) == true)
                {
                    mainWindow.Height = height;
                }

                String appTop = Configuration_Helper.GetConfigurationValue("ApplicationTop", "-1");
                Double top = 0.0;
                if (Double.TryParse(appTop, out top) == true)
                {
                    if (top >= 0.0)
                    {
                        mainWindow.Top = top;
                        mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                    }
                }

                String appLeft = Configuration_Helper.GetConfigurationValue("ApplicationLeft", "-1");
                Double left = 0.0;
                if (Double.TryParse(appLeft, out left) == true)
                {
                    if (left >= 0.0)
                    {
                        mainWindow.Left = left;
                        mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                    }
                }

                aggregator.GetEvent<Shell_Restore_Event>().Publish("Normal state from constructor");


                if (appState == "Maximized")
                {
                    aggregator.GetEvent<Shell_Maximize_Event>().Publish("Maximizing from constructor");
                }
                else if (appState == "Minimized")
                {
                    aggregator.GetEvent<Shell_Minimize_Event>().Publish("Minimizing from constructor");
                }
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

        private void BusyMouseCursorEventHandler(Boolean bBusy)
        {
            if (bBusy)
            {
                Mouse.OverrideCursor = Cursors.Wait;
            }
            else
            {
                Mouse.OverrideCursor = null;
            }
        }

        public Window ThisWindow
        {
            get
            {
                return this;
            }
        }

        private Boolean _IsWindowActive = true;

        public Boolean IsWindowActive
        {
            get
            {
                return _IsWindowActive;
            }
            set
            {
                _IsWindowActive = value;
                //OnPropertyChanged("IsWindowActive");
            }
        }

        #region LoginContext

        private Login_Context _LoginContext;

        /// <summary>Property LoginContext of type Login_Context</summary>
        public Login_Context LoginContext
        {
            get
            {
                if (_container.IsRegistered<AuthenticationToken_Interface>(AuthenticationToken.ApplicationGlobalInstanceName) == true)
                {
                    AuthenticationToken at = _container.Resolve<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName);
                    _LoginContext = at.LoginContext;
                }
                return _LoginContext;
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                logger.Debug("SART application shutting down");
                eventAggregator.GetEvent<Shell_Close_Event>().Publish("Closing Work Order");
                base.OnClosed(e);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
            }
            finally
            {
                //Application.Current.Shutdown(0);
            }
        }

        private void OnShellLoaded(object sender, RoutedEventArgs e)
        {
            //ClearValue(SizeToContentProperty);
            //root.ClearValue(WidthProperty);
            //root.ClearValue(HeightProperty);
            //CenterWindowOnScreen();
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        private void Minimize(String str)
        {
            logger.Debug(str);
            this.WindowState = WindowState.Minimized;
            _container.RegisterInstance<WindowState>("ApplicationWindowState", this.WindowState, new ContainerControlledLifetimeManager());
        }

        private void Maximize(String str)
        {
            logger.Debug(str);
            this.WindowState = WindowState.Maximized;
            _container.RegisterInstance<WindowState>("ApplicationWindowState", this.WindowState, new ContainerControlledLifetimeManager());
        }

        private void Restore(String str)
        {
            logger.Debug(str);
            this.WindowState = WindowState.Normal;
            _container.RegisterInstance<WindowState>("ApplicationWindowState", this.WindowState, new ContainerControlledLifetimeManager());
        }

        private void Close(String str)
        {
            logger.Debug(str);
            Application.Current.Shutdown();
        }

        private void Drag(String str)
        {
            logger.Debug(str);
            this.DragMove();
        }


        private void WindowBase_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.RightCtrl)
            {
                IsRightControlKeyPressed = false;
            }
            else if (e.Key == Key.RightShift)
            {
                IsRightShiftKeyPressed = false;
            }
            else if ((IsRightControlKeyPressed == true) && (IsRightShiftKeyPressed == true))
            {
                if (e.Key == Key.F11)
                {
                    Upload_Logs();
                }
                else if (e.Key == Key.F12)
                {
                    if (UploadingLogs == false)
                    {
                        UploadingLogs = true;
                        Entry_Window ew = new Entry_Window(Window_Add_Types.Date);
                        ew.dp_End_Date = DateTime.Today;
                        String logFileName = Logger_Helper.Get_First_LogFile();
                        if (String.IsNullOrEmpty(logFileName) == false)
                        {
                            String logFName = Path.GetFileNameWithoutExtension(logFileName);
                            try { ew.dp_Start_Date = DateTime.Parse(logFName); }
                            catch { }
                        }

                        FileInfo fi = new FileInfo(logFileName);
                        for (DateTime dt = ew.dp_Start_Date.Value; dt <= DateTime.Today; dt = dt.AddDays(1))
                        {
                            FileInfo black = new FileInfo(Path.Combine(fi.Directory.FullName, dt.ToString("yyyy-MM-dd") + ".log"));
                            if (black.Exists == false)
                            {
                                ew.Add_Blackout_Date(dt);
                            }
                        }

                        if (ew.ShowDialog() == true)
                        {
                            Thread uploadThread = new Thread(new ParameterizedThreadStart(UploadBackground));
                            uploadThread.IsBackground = true;
                            uploadThread.Start(ew.dp_SelectedDate.Value);
                        }
                    }
                }
                else if (e.Key == Key.T)
                {
                    Logger_Helper.ChangeLoggingLevel(LogLevel.Trace);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Logging level has been set to Trace");
                }
                else if (e.Key == Key.D)
                {
                    Logger_Helper.ChangeLoggingLevel(LogLevel.Debug);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Logging level has been set to Debug");
                }
                else if (e.Key == Key.I)
                {
                    Logger_Helper.ChangeLoggingLevel(LogLevel.Info);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Logging level has been set to Info");
                }
                else if (e.Key == Key.W)
                {
                    Logger_Helper.ChangeLoggingLevel(LogLevel.Warn);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Logging level has been set to Warn");
                }
                else if (e.Key == Key.E)
                {
                    Logger_Helper.ChangeLoggingLevel(LogLevel.Error);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Logging level has been set to Error");
                }
                else if (e.Key == Key.F)
                {
                    Logger_Helper.ChangeLoggingLevel(LogLevel.Fatal);
                    eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Logging level has been set to Fatal");
                }
                else if (e.Key == Key.P)
                {
                    //Rectangle bounds = this.Bounds;
                    //using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                    //{
                    //    using (Graphics g = Graphics.FromImage(bitmap))
                    //    {
                    //        g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                    //    }
                    //    bitmap.Save("C://test.jpg", ImageFormat.Jpeg);
                    //}
                }
            }
        }


        private void WindowBase_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.RightCtrl)
            {
                IsRightControlKeyPressed = true;
            }
            else if (e.Key == Key.RightShift)
            {
                IsRightShiftKeyPressed = true;
            }
        }


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        private void mainWindow_Activated(object sender, EventArgs e)
        {
            IsWindowActive = true;
            eventAggregator.GetEvent<StatusBar_ParentWindow_Active_Event>().Publish(true);
            eventAggregator.GetEvent<Application_Activated_Event>().Publish(true);
        }

        private void mainWindow_Deactivated(object sender, EventArgs e)
        {
            IsWindowActive = false;
            eventAggregator.GetEvent<StatusBar_ParentWindow_Active_Event>().Publish(false);
            eventAggregator.GetEvent<Application_Activated_Event>().Publish(false);
        }

        //// INotifyPropertyChanged members
        //public event PropertyChangedEventHandler PropertyChanged;
        ////

        //protected virtual void OnPropertyChanged(String name)
        //{
        //    if (PropertyChanged != null)
        //    {
        //        PropertyChanged(this, new PropertyChangedEventArgs(name));
        //    }
        //}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods


        private void Upload_Logs()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (UploadingLogs == false)
                {
                    UploadingLogs = true;
                    Thread uploadThread = new Thread(new ParameterizedThreadStart(UploadBackground));
                    uploadThread.IsBackground = true;
                    uploadThread.Start(DateTime.Today);
                }
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


        private void UploadBackground(Object date)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                DateTime dt = (DateTime)date;
                String fname = Path.Combine(Application_Helper.Application_Folder_Name(), "Logs", dt.ToString("yyyy-MM-dd") + ".log");
                String user = null;
                if ((LoginContext != null) && (String.IsNullOrEmpty(LoginContext.UserName) == false))
                {
                    user = LoginContext.UserName;
                }

                Runtime_Logs_Logs_Web_Service_Client_REST.Upload_Logs(user, DisplayMessage, fname);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                DisplayError(ex.Message);
            }
            finally
            {
                UploadingLogs = false;
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private void DisplayMessage(String msg)
        {
            eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Snow);
            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
        }


        private void DisplayError(String msg)
        {
            eventAggregator.GetEvent<StatusBar_Region1_Color_Event>().Publish(System.Windows.Media.Brushes.Pink);
            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(msg);
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mainWindow.WindowState != WindowState.Maximized)
            {
                Configuration_Helper.SetConfigurationValue("ApplicationWidth", mainWindow.Width.ToString());
                Configuration_Helper.SetConfigurationValue("ApplicationHeight", mainWindow.Height.ToString());
            }
            Configuration_Helper.SetConfigurationValue("ApplicationState", mainWindow.WindowState.ToString());
            Configuration_Helper.SetConfigurationValue("ApplicationTop", mainWindow.Top.ToString());
            Configuration_Helper.SetConfigurationValue("ApplicationLeft", mainWindow.Left.ToString());
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
