using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using NLog;
using Segway.Database.Objects;
using Segway.Login.Objects;
using Segway.Manufacturing.Objects;
using Segway.Modules.ShellControls;
using Segway.SART.Objects;
using Segway.Service.AppSettings.Helper;
using Segway.Service.Authentication.Client.REST;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using Segway.Syteline.Client.REST;
using Segway.Syteline.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Segway.Modules.SART_Infrastructure
{
    [Module(ModuleName = "InfrastructureModule")]
    [ModuleDependency("ShellControlsModule")]
    public class InfrastructureModule : IModule
    {
        static Logger logger = Logger_Helper.GetCurrentLogger();
        public const String Regional_Settings_Path = "Regional Settings.xml";

        public static IRegionManager RegionManager;
        public static IUnityContainer Container;
        public static IEventAggregator Aggregator;

        private static LifetimeManager LTMgr = null;
        private static Object Lock_Load_Customer_Addresses = new Object();


        public InfrastructureModule(IUnityContainer container, IRegionManager rm, IEventAggregator eventAggregator)
        {
            Container = container;
            RegionManager = rm;
            Aggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Login_Event>().Subscribe(Login_Event, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<Application_Logout_Event>().Subscribe(Logout_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<Navigate_To_Login_Event>().Subscribe(Navigated_To_Login, true);
            eventAggregator.GetEvent<Shell_Close_Event>().Subscribe(Application_Close, true);
            eventAggregator.GetEvent<Authentication_Failure_Event>().Subscribe(AuthenticationFailure_Handler, true);
            eventAggregator.GetEvent<SART_Load_Regional_Data_Event>().Subscribe(SART_Load_Regional_Data_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<SART_Save_Regional_Data_Event>().Subscribe(SART_Save_Regional_Data_Handler, ThreadOption.PublisherThread, true);
            eventAggregator.GetEvent<SART_Load_Dealers_Event>().Subscribe(SART_Load_Dealers_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<SART_Dealers_Loaded_Event>().Subscribe(SART_Dealers_Loaded_Handler, ThreadOption.PublisherThread, true);
            eventAggregator.GetEvent<SART_Settings_Changed_Event>().Subscribe(SART_Settings_Changed_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<SART_UserSettings_Changed_Event>().Subscribe(SART_UserSettings_Changed_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates

            #endregion

        }

        public void Initialize()
        {
            try
            {
                logger.Debug("Entered");
                Register_Types();

                logger.Debug("Testing for Systec Adapter");
                int SystecSN = new CAN2_Systec().Get_Serial_Number();
                if (SystecSN == 0)
                {
                    logger.Warn("Systec adapter was not found");
                }
                else
                {
                    logger.Debug("Systec adapter serial number: {0}", SystecSN);
                }

                /*String tool = Application_Helper.Application_Name();
                String version = Application_Helper.Version();
                PT_Registration_Services_Client_SART2012.Initialize();

                if (Updater_Web_Service_Client.UpdateSegwayExternalTool(tool, version)) return;
                logger.Debug("No update found");*/

                Dealer_Info DealerInfo = Container.Resolve<Dealer_Info>(Dealer_Info.Name);
                DealerInfo.LoadDealer();
                Aggregator.GetEvent<SART_Dealers_Loaded_Event>().Publish(true);

            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                Aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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

        private void Register_Types()
        {
            if (Container.IsRegistered<AuthenticationToken_Interface>(AuthenticationToken.ApplicationGlobalInstanceName) == false)
            {
                Container.RegisterType<AuthenticationToken_Interface, AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName, new ContainerControlledLifetimeManager());
            }
            Container.RegisterType<Dealer_Info_Interface, Dealer_Info>(new ContainerControlledLifetimeManager());
            Container.RegisterType<Regional_Settings_Interface, Regional_Settings>(new ContainerControlledLifetimeManager());

            Container.RegisterInstance<Dealer_Info>(Dealer_Info.Name, new Dealer_Info(), new ContainerControlledLifetimeManager());
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        #region Current_Work_Order

        //private static SART_Work_Order _Current_Work_Order;

        /// <summary>Property Current_Work_Order of type SART_Work_Order</summary>
        public static SART_Work_Order Current_Work_Order { get; set; }
        //{
        //    get
        //    {
        //        if (Container.IsRegistered<SART_Work_Order>("Current_WorkOrder") == true)
        //        {
        //            return Container.Resolve<SART_Work_Order>("Current_WorkOrder");
        //        }
        //        return null;
        //    }
        //    set
        //    {
        //        if (value != null)
        //        {
        //            if (LTMgr == null) LTMgr = new ContainerControlledLifetimeManager();
        //            Container.RegisterInstance<SART_Work_Order>("Current_WorkOrder", value, LTMgr);
        //        }
        //        else
        //        {
        //            if (LTMgr != null)
        //            {
        //                LTMgr.RemoveValue();
        //                LTMgr = null;
        //            }

        //            if (Container.IsRegistered<SART_Work_Order>("Current_WorkOrder") == true)
        //            {
        //                logger.Warn("Current_WorkOrder is still Registered");
        //            }

        //            //foreach (var reg in Container.Registrations)
        //            //{
        //            //    if (reg.Name == "Current_WorkOrder")
        //            //    {
        //            //        reg.LifetimeManager.RemoveValue();
        //            //        break;
        //            //    }
        //            //}
        //        }
        //    }
        //}

        #endregion


        #region WorkOrder_OpenMode

        private static Open_Mode _WorkOrder_OpenMode;

        /// <summary>Property WorkOrder_OpenMode of type Open_Mode</summary>
        public static Open_Mode WorkOrder_OpenMode
        {
            get { return _WorkOrder_OpenMode; }
            set { _WorkOrder_OpenMode = value; }
        }

        #endregion


        #region Original_Work_Order

        private static SART_Work_Order _Original_Work_Order;

        /// <summary>Property Original_Work_Order of type static SART_Work_Order</summary>
        public static SART_Work_Order Original_Work_Order
        {
            get { return _Original_Work_Order; }
            set { _Original_Work_Order = value; }
        }

        #endregion


        #region RegionSettings

        private static Regional_Settings _RegionSettings;

        /// <summary>Property RegionSettings of type Regional_Settings</summary>
        public static Regional_Settings RegionSettings
        {
            get
            {
                if (_RegionSettings == null)
                {
                    Load_Regional_Settings();
                }
                return _RegionSettings;
            }
            set { _RegionSettings = value; }
        }

        #endregion


        #region Token

        private static AuthenticationToken _Token;

        /// <summary>Property Token of type AuthenticationToken</summary>
        public static AuthenticationToken Token
        {
            get
            {
                if (_Token == null)
                {
                    if (Container.IsRegistered<AuthenticationToken_Interface>(AuthenticationToken.ApplicationGlobalInstanceName) == true)
                    {
                        AuthenticationToken t = (AuthenticationToken)Container.Resolve<AuthenticationToken_Interface>(AuthenticationToken.ApplicationGlobalInstanceName);
                        if ((t.LoginContext == null) || (String.IsNullOrEmpty(t.Token) == true)) return null;
                        _Token = t;
                    }
                }
                return _Token;
            }
            set { _Token = value; }
        }

        #endregion


        #region LoginContext

        private Login_Context _LoginContext;

        /// <summary>Property LoginContext of type Login_Context</summary>
        public Login_Context LoginContext
        {
            get
            {
                if (Token != null)
                {
                    _LoginContext = Token.LoginContext;
                }
                return _LoginContext;
            }
        }

        #endregion


        #region User_Settings

        /// <summary>Property User_Settings of type SART_User_Settings</summary>
        public static SART_User_Settings User_Settings { get; set; }

        #endregion


        #region Part_Numbers_Models

        private static Dictionary<String, Segway_Part_Number_Information> _Part_Numbers_Models;

        /// <summary>Property Production_Part_Numbers_XRef of type Dictionary<String, String></summary>
        public static Dictionary<String, Segway_Part_Number_Information> Part_Numbers_Models
        {
            get
            {
                if (_Part_Numbers_Models == null)
                {
                    var pns = Manufacturing_SPNI_Web_Service_Client_REST.Select_Segway_Part_Number_Information_All(Token);
                    if (pns != null)
                    {
                        _Part_Numbers_Models = new Dictionary<String, Segway_Part_Number_Information>();
                        foreach (var pn in pns)
                        {
                            _Part_Numbers_Models[pn.Part_Number] = pn;
                        }
                    }
                }
                return _Part_Numbers_Models;
            }
            set
            {
                _Part_Numbers_Models = value;
            }
        }

        #endregion


        #region Service_Part_Numbers_XRef

        private static Dictionary<String, String> _Service_Part_Numbers_XRef;

        /// <summary>Property Service_Part_Numbers_XRef of type Dictionary<String, String></summary>
        public static Dictionary<String, String> Service_Part_Numbers_XRef
        {
            get
            {
                if (_Service_Part_Numbers_XRef == null)
                {
                    var pns = Manufacturing_SPNPX_Web_Service_Client_REST.Select_Segway_Part_Number_Production_Xref_All(Token);
                    if (pns != null)
                    {
                        _Service_Part_Numbers_XRef = new Dictionary<String, String>();
                        foreach (var pn in pns)
                        {
                            _Service_Part_Numbers_XRef[pn.Part_Number] = pn.Production_Part_Number;
                        }
                    }
                }
                return _Service_Part_Numbers_XRef;
            }
            set
            {
                _Service_Part_Numbers_XRef = value;
            }
        }

        #endregion


        #region Assembly_Table_Parts

        private static Dictionary<String, Segway_Part_Type_Xref> _Assembly_Table_Parts;

        /// <summary>Property Assembly_Table_Parts of type List<Segway_Part_Type_Xref></summary>
        public static Dictionary<String, Segway_Part_Type_Xref> Assembly_Table_Parts
        {
            get
            {
                if (_Assembly_Table_Parts == null)
                {
                    var xrefList = Manufacturing_SPTX_Web_Service_Client_REST.Select_Segway_Part_Type_Xref_All(InfrastructureModule.Token);
                    if (xrefList != null)
                    {
                        _Assembly_Table_Parts = new Dictionary<string, Segway_Part_Type_Xref>();
                        foreach (var xref in xrefList)
                        {
                            _Assembly_Table_Parts[xref.Service_Part_Number] = xref;
                        }
                        if (_Assembly_Table_Parts.Count == 0) return null;
                    }
                    // SART_2012_Web_Service_Client.Select_Segway_Part_Type_Xref_Criteria(InfrastructureModule.Token, null);
                    //if ((_Assembly_Table_Parts != null) && (_Assembly_Table_Parts.Count == 0)) return null;
                }
                return _Assembly_Table_Parts;
            }
            set { _Assembly_Table_Parts = value; }
        }

        #endregion


        #region Settings

        /// <summary>Property Settings of type SART_Settings</summary>
        public static SART_Settings Settings
        {
            get { return CAN2_Commands.Settings; }
            set { CAN2_Commands.Settings = value; }
        }

        #endregion

        #region Customer_Address_List

        private static List<Address_Information> _Customer_Address_List = null;

        /// <summary>Property Customer_Address_List of type List<Address_Information></summary>
        public static List<Address_Information> Customer_Address_List
        {
            get
            {
                if (_Customer_Address_List == null)
                {
                    Load_Customer_Addresses();
                }
                return _Customer_Address_List;
            }
            set { _Customer_Address_List = value; }
        }

        #endregion



        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        /////////////////////////////////////////////
        #region Application_Close  -- Close_Shell_Event Event Handler
        /////////////////////////////////////////////

        private void Application_Close(String msg)
        {
            FileInfo fi = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", Regional_Settings_Path));
            if (fi.Directory.Exists == false) fi.Directory.Create();
            if (fi.Exists) fi.Delete();
            Serialization.SerializeToFile<Regional_Settings>(RegionSettings, fi.FullName);
            SART_Common.Stop_Watch_Thread();
        }

        /////////////////////////////////////////////
        #endregion
        /////////////////////////////////////////////


        /////////////////////////////////////////////
        #region Navigated_To_Login  -- NavigateTo_Login_Event Event Handler
        /////////////////////////////////////////////

        private void Navigated_To_Login(String msg)
        {
            logger.Info("Application Info - Name: {0}", Application_Helper.Application_Name());
            logger.Info("Application Info - Version: {0}", Application_Helper.Version());
            logger.Info("Application Info - Folder: {0}", Application_Helper.Application_Folder_Name());
            logger.Info("Application Info - Metric?: {0}", Application_Helper.IsMetric);
            Aggregator.GetEvent<StatusBar_Region6_Event>().Publish(String.Format("v{0}", Application_Helper.Version()));
            Aggregator.GetEvent<StatusBar_Region7_Event>().Publish(String.Format("  |  {0}", RunMode.Mode.ToString()[0]));

            Aggregator.GetEvent<StatusBar_Region1_Width_Event>().Publish(-1);
            Aggregator.GetEvent<StatusBar_Region2_Width_Event>().Publish(0);
            Aggregator.GetEvent<StatusBar_Region3_Width_Event>().Publish(0);
            Aggregator.GetEvent<StatusBar_Region4_Width_Event>().Publish(0);
            Aggregator.GetEvent<StatusBar_Region5_Width_Event>().Publish(0);
            Aggregator.GetEvent<StatusBar_Region6_Width_Event>().Publish(0);
            Aggregator.GetEvent<StatusBar_Region7_Width_Event>().Publish(0);

            Aggregator.GetEvent<StatusBar_Region1_FontSize_Event>().Publish(12);
            Aggregator.GetEvent<StatusBar_Region5_FontSize_Event>().Publish(12);
            Aggregator.GetEvent<StatusBar_Region6_FontSize_Event>().Publish(12);
            Aggregator.GetEvent<StatusBar_Region7_FontSize_Event>().Publish(12);
        }

        /////////////////////////////////////////////
        #endregion
        /////////////////////////////////////////////


        /////////////////////////////////////////////
        #region Login_Event  -- Application_Login_Event Handler
        /////////////////////////////////////////////

        private void Login_Event(String userName)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                _LoginContext = null;
                _Token = null;
                Aggregator.GetEvent<StatusBar_Region5_Event>().Publish(String.Format("User:  {0}   |  ", userName));
                if (LoginContext != null)
                {
                    Aggregator.GetEvent<SART_Load_Regional_Data_Event>().Publish(true);
                    Aggregator.GetEvent<SART_Load_Dealers_Event>().Publish(true);

                    Update_User_Settings();
                }

                Task back = new Task(() =>
                    { Load_Customer_Addresses(); });
                back.ContinueWith((o) =>
                {
                    String m = Exception_Helper.FormatExceptionString(o.Exception);
                    logger.Error(m);
                }, TaskContinuationOptions.OnlyOnFaulted);
                back.ContinueWith((o) =>
                {
                    logger.Debug("Successfully loaded Dealers");
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
                back.Start();
                logger.Debug("Started background process");


            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                Aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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

        /////////////////////////////////////////////
        #endregion
        /////////////////////////////////////////////


        /////////////////////////////////////////////
        #region Logout_Handler  -- Application_Logout_Event Event Handler
        /////////////////////////////////////////////

        private void Logout_Handler(Boolean logout)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (logout == true)
                {
                    if (LoginContext == null)
                    {
                        logger.Warn("LoginContext is null");
                        return;
                    }
                    if (String.IsNullOrEmpty(LoginContext.UserName) == true)
                    {
                        logger.Warn("UserName of LoginContext is null or empty");
                        return;
                    }
                    if (LoginContext.User_Level < UserLevels.Expert)
                    {
                        Service_Users su = Authentication_User_Web_Service_Client_REST.Select_Service_Users_USERNAME(InfrastructureModule.Token, LoginContext.UserName);
                        if (su != null)
                        {
                            String tool = Get_Tool_Name();
                            logger.Debug("Searching for tool: {0}", tool);
                            Service_User_Access sua = su.AccessInfo(tool);
                            if (sua != null)
                            {
                                logger.Debug("Default User Level: {0}", sua.User_Default_Level);
                                sua.Access_Level = sua.User_Default_Level;
                                if (String.IsNullOrEmpty(sua.Access_Level) == true)
                                {
                                    sua.Access_Level = UserLevels.Basic.ToString();
                                    logger.Debug("Default User Level set to: {0}", sua.Access_Level);
                                }
                                if (Authentication_Access_Web_Service_Client_REST.Update_Service_User_Access_Object(InfrastructureModule.Token, sua))
                                {
                                    logger.Debug("Successfully reset Non-Expert user to {0}", sua.Access_Level);
                                }
                                else
                                {
                                    logger.Warn("Update user's access record failed");
                                }
                            }
                            else
                            {
                                logger.Warn("Could not find tool: {0} for user: {1}", tool, LoginContext.UserName);
                            }
                        }
                    }
                }
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                Aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
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

        public static String Get_Tool_Name()
        {
            String tool = App_Settings_Helper.GetConfigurationValue("ToolName");
            if (String.IsNullOrEmpty(tool) == false)
            {
                if (String.Compare("Remote Service Tool", tool, true) == 0)
                {
                    tool = App_Settings_Helper.GetConfigurationValue(tool, "RST");
                }
            }

            return tool;
        }

        /////////////////////////////////////////////
        #endregion
        /////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region AuthenticationFailure_Handler  -- Event: AuthenticationFailure_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void AuthenticationFailure_Handler(Boolean failed)
        {
            Current_Work_Order = null;
            Token = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_Load_Regional_Data_Handler  -- Event: SART_Load_Regional_Data_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_Load_Regional_Data_Handler(Boolean load)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Load_Regional_Settings();
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_Save_Regional_Data_Handler  -- Event: SART_Save_Regional_Data_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_Save_Regional_Data_Handler(Boolean save)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Save_Regional_Settings();
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_Load_Dealers_Handler  -- Event: SART_Load_Dealers_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_Load_Dealers_Handler(Boolean load)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Dealer_Info di = Container.Resolve<Dealer_Info>(Dealer_Info.Name);
                di.LoadDealer();
                Aggregator.GetEvent<SART_Dealers_Loaded_Event>().Publish(true);
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_Dealers_Loaded_Handler  -- Event: SART_Dealers_Loaded_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_Dealers_Loaded_Handler(Boolean loaded)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
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

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_Settings_Changed_Handler  -- Event: SART_Settings_Changed_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_Settings_Changed_Handler(Boolean changed)
        {
            if (changed == true)
            {
                Settings = SART_Settings_Web_Service_Client_REST.Select_SART_Settings(InfrastructureModule.Token);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_UserSettings_Changed_Handler  -- Event: SART_UserSettings_Changed_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_UserSettings_Changed_Handler(SART_User_Settings changed)
        {
            if (changed != null)
            {
                User_Settings.Update(changed);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        //public static void Clear_DealerList()
        //{
        //    try
        //    {
        //        logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
        //        Dealer_Info DealerInfo = Container.Resolve<Dealer_Info>(Dealer_Info.Name);
        //        DealerInfo.Dealer_List = null;
        //        DealerInfo.Accounts = null;
        //        DealerInfo.Dealers = null;
        //        Aggregator.GetEvent<SART_Load_Dealers_Event>().Publish(true);
        //    }
        //    catch (Authentication_Exception ae)
        //    {
        //        logger.Warn(Exception_Helper.FormatExceptionString(ae));
        //        Aggregator.GetEvent<AuthenticationFailure_Event>().Publish(true);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(Exception_Helper.FormatExceptionString(ex));
        //        throw;
        //    }
        //    finally
        //    {
        //        logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
        //    }
        //}

        public static String Format_CU_OperatingTime(int? seconds)
        {
            if (seconds == null) return null;
            TimeSpan ts = new TimeSpan(0, 0, seconds.Value * 15);
            return Format_Short_Time(ts);
        }

        public static String Format_Short_Time(TimeSpan ts)
        {
            List<String> parts = new List<String>(Strings.Split(ts.ToString(), ':'));
            parts[0] = String.Format("{0}", (int)ts.TotalHours);
            return Strings.MergeName(parts, ':');
        }

        public static String Format_Timestamp(DateTime? ts)
        {
            if (ts.HasValue == false) return "";
            return String.Format("{0} {1}", ts.Value.ToShortDateString(), ts.Value.ToShortTimeString());
        }

        public static String Format_CU_Odometer(int? meters)
        {
            if (meters.HasValue == false) return null;
            Conversion.Meters = (Double)meters.Value;
            if (Application_Helper.IsMetric)
            {
                return String.Format("{0:F2}  km", Conversion.Kilometers);
            }
            else
            {
                return String.Format("{0:F2}  mi", Conversion.Miles);
            }
        }


        /// <summary>Pulic Static Method - Loads the Regional Data setting</summary>
        public static void Load_Regional_Settings()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Regional_Settings rs = null;
                FileInfo fi = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", Regional_Settings_Path));
                try
                {
                    if (fi.Directory.Exists == false) fi.Directory.Create();
                    rs = Serialization.DeserializeFromFile<Regional_Settings>(fi.FullName);
                }
                catch
                {

                }
                if (rs == null)
                {
                    rs = new Regional_Settings();
                    rs.IsUSA = true;
                    rs.IsAPAC = true;
                    rs.IsEMEA = true;
                    rs.IsCANA = true;
                    rs.IsLTAM = true;
                    fi.Refresh();
                    if (fi.Exists) fi.Delete();
                    Serialization.SerializeToFile<Regional_Settings>(rs, fi.FullName);
                }

                _RegionSettings = Container.Resolve<Regional_Settings>();
                _RegionSettings.Copy(rs);
                Aggregator.GetEvent<SART_Loaded_Regional_Data_Event>().Publish(true);
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


        /// <summary>Pulic Static Method - Saves the Regional Data setting</summary>
        public static void Save_Regional_Settings()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                FileInfo fi = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "App Data", Regional_Settings_Path));
                try
                {
                    if (fi.Directory.Exists == false) fi.Directory.Create();
                    Serialization.SerializeToFile<Regional_Settings>(_RegionSettings, fi.FullName);
                    Aggregator.GetEvent<SART_Saved_Regional_Data_Event>().Publish(true);
                }
                catch
                {

                }
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

        private void Update_User_Settings()
        {
            User_Settings = SART_Users_Web_Service_Client_REST.Select_SART_User_Settings_USER_NAME(InfrastructureModule.Token, LoginContext.UserName);
            if (User_Settings == null)
            {
                User_Settings = new SART_User_Settings(User_Settings);
                User_Settings.User_Name = LoginContext.UserName;
                User_Settings.Date_Time_Entered = User_Settings.Date_Time_Updated = DateTime.Now;
                User_Settings = SART_Users_Web_Service_Client_REST.Insert_SART_User_Settings_Key(InfrastructureModule.Token, User_Settings);
            }

            Update_SART_Settings();
        }

        private static void Update_SART_Settings()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (String.IsNullOrEmpty(User_Settings.SART_Settings_Name) == false)
                {
                    Settings = SART_Settings_Web_Service_Client_REST.Select_SART_Settings_NAME(InfrastructureModule.Token, User_Settings.SART_Settings_Name);
                    if (Settings != null) return;
                }

                try
                {
                    Settings = SART_Settings_Web_Service_Client_REST.Select_SART_Settings_NAME(InfrastructureModule.Token, "Default");
                    if (Settings != null)
                    {
                        return;
                    }


                    var Settings_List = SART_Settings_Web_Service_Client_REST.Select_SART_Settings_Criteria(InfrastructureModule.Token, null);
                    if ((Settings_List != null) && (Settings_List.Count > 0))
                    {
                        Settings_List.Sort(new SART_Settings_Date_Comparer());
                        Settings = Settings_List[0];
                        if (Settings != null)
                        {
                            return;
                        }
                    }

                    Settings = SART_Settings_Web_Service_Client_REST.Select_SART_Settings(InfrastructureModule.Token);
                    if (Settings == null)
                    {
                        Settings = new SART_Settings();
                        Settings.Delay_Full_Stop = 5000;
                        Settings.Delay_LEDTest_Wakeup = 6000;
                        Settings.Delay_Wake_Start_Wireless = 5000;
                        Settings.Delay_Wake_Start = 2000;
                        Settings.Delay_Diagnostic_Wakeup = 5000;
                        Settings.Timeout_Start_Applet_Response = 2000;
                        Settings.Timeout_Start_Applet = 20;
                        Settings.Timeout_Heartbeat = 2000;
                        Settings.Timeout_BSA_SPI_Enter_Boot = 5000;
                        Settings.Timeout_Enter_Diagnostic_Mode = 5000;
                        Settings.Timeout_Ramp = 2500;
                        Settings.Name = "Default";
                        Settings = SART_Settings_Web_Service_Client_REST.Insert_SART_Settings_Key(InfrastructureModule.Token, Settings);
                    }
                }
                finally
                {
                    if (User_Settings != null)
                    {
                        User_Settings.SART_Settings_Name = Settings.Name;
                        SART_Users_Web_Service_Client_REST.Update_SART_User_Settings_Key(InfrastructureModule.Token, User_Settings);
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
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        public static Boolean Perform_Reset_Channels()
        {
            try
            {
                var processString = App_Settings_Helper.GetConfigurationValue("Process Resetting Channels", Boolean.FalseString);
                return Boolean.Parse(processString);
            }
            catch
            {
                return false;
            }
        }

        private static void Load_Customer_Addresses()
        {
            lock (Lock_Load_Customer_Addresses)
            {
                SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                criteria.Add(new FieldData("Cust_Seq", 0));
                _Customer_Address_List = Syteline_AdrInfo_Web_Service_Client_REST.Get_Address_Information_From_Syteline_Customer(Token, criteria);
            }
        }

        public static Security Load_Data_To_Security(List<Manufacturing_Component_Data> dataList)
        {
            Security sec = new Security();
            foreach (Manufacturing_Component_Data data in dataList)
            {
                MCD_Types mcdType = MCD_Types.NOT_DEFINED;
                data.Data_Type = data.Data_Type.Replace(" ", "_");
                if (Enum.TryParse<MCD_Types>(data.Data_Type, out mcdType) == true)
                {
                    switch (mcdType)
                    {
                        case MCD_Types.BSA_JTAG:
                            sec.BSA_JTag_Lock = data.Data;
                            break;
                        case MCD_Types.CUA_JTAG:
                            sec.CU_A_JTag_Lock = data.Data;
                            break;
                        case MCD_Types.CUB_JTAG:
                            sec.CU_B_JTag_Lock = data.Data;
                            break;
                        case MCD_Types.CHANNEL:
                            sec.Radio_Channel_Configuration = data.Data;
                            break;
                        case MCD_Types.ENCRYPT_KEY:
                            sec.Radio_Encryption_Key = data.Data;
                            break;
                        case MCD_Types.FOB_SID:
                            sec.FOB_Radio_SID = data.Data;
                            break;
                        case MCD_Types.FOB_WID:
                            sec.FOB_Radio_WID = data.Data;
                            break;
                        case MCD_Types.SERVICE_KEY:
                            sec.Service_Key_Code = data.Data;
                            break;
                        case MCD_Types.USER_KEY:
                            sec.User_Key_Code = data.Data;
                            break;
                        case MCD_Types.UIC_SID:
                            sec.Console_Radio_SID = data.Data;
                            break;
                        case MCD_Types.UIC_WID:
                            sec.Console_Radio_WID = data.Data;
                            break;
                    }

                    if (sec.Date_Time_Entered > data.Date_Time_Created)
                    {
                        sec.Date_Time_Entered = data.Date_Time_Created;
                    }
                    if (String.IsNullOrEmpty(sec.Unit_ID_Serial_Number) == true)
                    {
                        sec.Unit_ID_Serial_Number = InfrastructureModule.Current_Work_Order.PT_Serial;
                    }
                }
            }
            return sec;
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
