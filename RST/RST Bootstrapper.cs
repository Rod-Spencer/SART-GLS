using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.UnityExtensions;
using Microsoft.Practices.Unity;
using NLog;
using Segway.Modules.Administration;
using Segway.Modules.Controls.Comment;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.CU_Log_Module;
using Segway.Modules.Diagnostic;
using Segway.Modules.Diagnostic.All;
using Segway.Modules.Diagnostic.MotorTests;
using Segway.Modules.Diagnostic.PT_BSA;
using Segway.Modules.Diagnostic.PT_LED;
using Segway.Modules.Diagnostic.RiderDetect;
using Segway.Modules.Login;
using Segway.Modules.SART.CodeLoad;
using Segway.Modules.SART.Repair;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder;
using Segway.RT.Logs.Client;
using Segway.SART.Reports;
using Segway.Service.AppSettings.Helper;
using Segway.Service.Authentication.Client.REST;
using Segway.Service.Authentication.Objects;
using Segway.Service.BugTracking;
using Segway.Service.BugZilla.Client.REST;
using Segway.Service.Common;
using Segway.Service.Controls.ListBoxVerticalToolBar;
using Segway.Service.Controls.StatusBars;
using Segway.Service.Controls.TitleBars;
using Segway.Service.Disclaimer;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.Modules.AddWindow;
using Segway.Service.SART;
using Segway.Service.SART.Client.REST;
using Segway.Service.SART.RideTest;
using Segway.Service.SART_2012.Battery;
using Segway.Service.Updater.Client.REST;
using Segway.Syteline.Client.REST;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Windows;

namespace Segway.RST
{
    // Deriving from UnityBootStrapper pulls in the Unity Dependency Injection framework
    public class RST_Bootstrapper : UnityBootstrapper
    {
        private static Logger logger = Logger_Helper.GetCurrentLogger();

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<Shell>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();
            App.Current.MainWindow = (Window)this.Shell;
            App.Current.MainWindow.Show();

            logger.Debug("Testing for Culture name");
            String cultureName = ConfigurationManager.AppSettings["Culture"];
            if (String.IsNullOrEmpty(cultureName) == true)
            {
                logger.Warn("Culture name was not found");
                Configuration_Helper.SetConfigurationValue("Culture", CultureInfo.CurrentCulture.Name);
                ConfigurationManager.AppSettings["Culture"] = CultureInfo.CurrentCulture.Name;
            }
            logger.Info("Culture: {0}", ConfigurationManager.AppSettings["Culture"]);

            logger.Info(Application_Helper.Name_And_Version());
            Logger_Helper.RemoveLogFiles();
            RunMode.SetOverRide(Environment.GetCommandLineArgs());

            String appName = App_Settings_Helper.GetConfigurationValue("ToolName");
            if (String.IsNullOrEmpty(appName) == false)
            {
                Container.RegisterInstance<String>(TitleBar_View_Model.TitleBarDisplayName, appName, new ContainerControlledLifetimeManager());
            }

            try
            {
                Authentication_Web_Service_Client_REST.HeartBeat();
                BugZilla_Web_Service_Client_REST.HeartBeat();
                Manufacturing_Web_Service_Client_REST.HeartBeat();
                Runtime_Logs_Web_Service_Client_REST.HeartBeat();
                SART_Web_Service_Client_REST.HeartBeat();
                Syteline_Web_Service_Client_REST.HeartBeat();
                Updater_Web_Service_Client_REST.HeartBeat();
            }
            catch
            {
                String msg = "Unable to connect to web services";
                logger.Error(msg);
                MessageBox.Show(msg);
                Application.Current.Shutdown();
                return;
            }

            Updater_Web_Service_Client_REST.Initialize(null, Update_Check_Result_Handler, Updater_Shutdown_Handler);
            String tool = Configuration_Helper.GetAppSettingString("ToolAlias", Application_Helper.Application_Name());
            Updater_Update_Web_Service_Client_REST.UpdateSegwayTool(tool, Application_Helper.Version());
        }


        protected override IModuleCatalog CreateModuleCatalog()
        {
            FileInfo pFI = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "ToolBar Permissions.bf"));
            if (pFI.Exists == false)
            {
                pFI = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", "ToolBar Permissions.xml"));
                if (pFI.Exists == false)
                {
                    try
                    {
                        if (pFI.Directory.Exists == false) pFI.Directory.Create();
                        TextWriter tw = new StreamWriter(pFI.FullName);
                        if (tw == null) throw new Exception("Missing ToolBar Permissions and unable to create permissions file");
                        tw.Write(Embedded_Helper.GetEmbeddedContentString("ToolBar Permissions.xml"));
                        tw.Close();
                    }
                    catch (Exception ex)
                    {
                        logger.Fatal(Exception_Helper.FormatExceptionString(ex));
                        Application.Current.Shutdown();
                        return null;
                    }
                }
            }

            ModuleCatalog catalog = new ModuleCatalog();
            catalog.AddModule(typeof(TitleBarModule));
            catalog.AddModule(typeof(StatusBar_Module));
            catalog.AddModule(typeof(InfrastructureModule));
            catalog.AddModule(typeof(ShellControlsModule));
            catalog.AddModule(typeof(Comment_Module));

            catalog.AddModule(typeof(LoginModule));
            catalog.AddModule(typeof(WorkOrderModule));
            catalog.AddModule(typeof(CU_Log_Module));
            catalog.AddModule(typeof(CU_Code_Load_Module));
            catalog.AddModule(typeof(SART_Diagnostic_Module));
            catalog.AddModule(typeof(Repair_Module));
            catalog.AddModule(typeof(Ride_Test_Module));
            catalog.AddModule(typeof(Reports_Module));

#if false
            catalog.AddModule(typeof(Email_Segway_Module));
#endif
            catalog.AddModule(typeof(Bug_Tracking_Module));
            catalog.AddModule(typeof(Administration_Module));
            catalog.AddModule(typeof(MultiLevel_Tool_Bar_Module));

            catalog.AddModule(typeof(MotorTestsModule));
            catalog.AddModule(typeof(RiderDetectModule));
            catalog.AddModule(typeof(PT_BSAModule));
            catalog.AddModule(typeof(PT_LED_Module));
            catalog.AddModule(typeof(Battery_Info_Module));
            catalog.AddModule(typeof(All_Diagnostics_Module));
            catalog.AddModule(typeof(ListBoxVerticalToolBarModule));
            catalog.AddModule(typeof(SART_Disclaimer_Module));

            catalog.AddModule(typeof(BlackBox_Module));
            return catalog;
        }


        private AuthenticationWebsiteDescription SetServiceUrls(Boolean ext)
        {
            AuthenticationWebsiteDescription authWS = new AuthenticationWebsiteDescription();
            authWS.ApplicationName = Application_Helper.Application_Name();


            String key = String.Format("{0} Server {1}", Application_Helper.Application_Name(), RunTimeMode.Production);
            String server = App_Settings_Helper.GetConfigurationValue(key, "service");
            if (ext == true) server += ".segway.com";
            key = String.Format("{0} {1}", Application_Helper.Application_Name(), RunTimeMode.Production);
            String service = App_Settings_Helper.GetConfigurationValue(key, "RST 46 Web Services");
            String site = String.Format("http://{0}/{1}", server, service);
            logger.Debug("Production - {0}", site);
            authWS.SetModalUrl(RunTimeMode.Production, site);


            key = String.Format("{0} Server {1}", Application_Helper.Application_Name(), RunTimeMode.Test);
            server = App_Settings_Helper.GetConfigurationValue(key, "service-test");
            if (ext == true) server += ".segway.com";
            key = String.Format("{0} {1}", Application_Helper.Application_Name(), RunTimeMode.Test);
            service = App_Settings_Helper.GetConfigurationValue(key, "RST Web Services");
            site = String.Format("http://{0}/{1}", server, service);
            logger.Debug("Test - {0}", site);
            authWS.SetModalUrl(RunTimeMode.Test, site);


            key = String.Format("{0} Server {1}", Application_Helper.Application_Name(), RunTimeMode.Local);
            server = App_Settings_Helper.GetConfigurationValue(key, "localhost:16725");
            key = String.Format("{0} {1}", Application_Helper.Application_Name(), RunTimeMode.Local);
            service = App_Settings_Helper.GetConfigurationValue(key, "RST Web Services");
            site = String.Format("http://{0}/{1}", server, service);
            logger.Debug("Local - {0}", site);
            authWS.SetModalUrl(RunTimeMode.Local, site);





            Container.RegisterInstance<AuthenticationWebsiteDescription>(AuthenticationWebsiteDescription.ApplicationGlobalInstanceName, authWS);
            return authWS;
        }


        public Boolean Update_Check_Result_Handler(String msg, String lbl)
        {
            Message_Window mw = null;
            Boolean? result = false;
            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate ()
                    {
                        mw = Message_Window.Information(msg, lbl, buttons: MessageButtons.YesNo);
                        result = mw.ShowDialog();
                    });
                }
            }

            if (result.HasValue == false) return false;
            return mw.dp_DialogResult == MessageButtons.Yes;
        }

        public void Updater_Shutdown_Handler()
        {
            Application.Current.Shutdown();
        }

    }
}
