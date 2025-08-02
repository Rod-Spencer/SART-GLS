using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ShellControls;
using Segway.Service.Common;
using Segway.Service.Helper;
using Segway.Service.Tools.ModuleBase;
using System;

namespace Segway.Modules.Administration
{
    /// <summary>Public Class</summary>
    [Module(ModuleName = "Administration_Module")]
    public class Administration_Module : Module_Base, IModule
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;

        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        public Administration_Module(IUnityContainer container, IRegionManager regionManager)
        {
            this.container = container;
            this.regionManager = regionManager;
        }

        /// <summary>Public Method - Initialize</summary>
        /// <returns>void</returns>
        public void Initialize()
        {
            try
            {
                logger.Debug("Entered");
                Register_Types();

                // create a ToolBar_Item to represent the CU Log View
                //var adminToolBarItem = new ToolBar_Item("Administration", "To change the accessibility of a user", Image_Helper.ImageFromEmbedded("Images.Administrator.png"), "Main_Control");
                //container.RegisterInstance<ToolBar_Interface>("adminToolBarItem", adminToolBarItem);

                ///////////////////////////////////////////////////////////////////////////////////////
                // Administration
                var Admin_BTList = new ToolBar_List("Administration", "Administration", "To change the accessibility of a user", null, Image_Helper.ImageFromEmbedded("Images.Administrator.png"));
                container.RegisterInstance<ToolBar_Interface>("Admin_BTList", Admin_BTList);
                // Administration
                ///////////////////////////////////////////////////////////////////////////////////////



                //var mainVM = this.container.Resolve<Main_ViewModel_Interface>();
                //regionManager.Regions[RegionNames.MainRegion].Add(mainVM.View);



                ///////////////////////////////////////////////////////////////////////////////////////
                // Override_Management

                var Override_Management_TBItem = new ToolBar_Item("Override_Management", "Override Management", "Override configuration settings and close setting", Image_Helper.ImageFromEmbedded("Images.Override.png"), Override_Management_Control.Control_Name, null, "Administration");
                container.RegisterInstance<ToolBar_Interface>("Override_Management_TBItem", Override_Management_TBItem);

                logger.Debug("Resolving Override_Management ViewModel");
                var override_managementVM = this.container.Resolve<Override_Management_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(override_managementVM.View, Override_Management_Control.Control_Name);

                // Override_Management
                ///////////////////////////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////////////////////////
                // User_Management

                var User_Management_TBItem = new ToolBar_Item("User_Management", "User Management", "Manage user configuration settings, access level, JTag visibility, and regional settings", Image_Helper.ImageFromEmbedded("Images.User.png"), User_Management_Control.Control_Name, null, "Administration");
                container.RegisterInstance<ToolBar_Interface>("User_Management_TBItem", User_Management_TBItem);

                logger.Debug("Resolving User_Management ViewModel");
                var user_managementVM = this.container.Resolve<User_Management_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(user_managementVM.View, User_Management_Control.Control_Name);

                // User_Management
                ///////////////////////////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////////////////////////
                // Settings

                var Settings_TBItem = new ToolBar_Item("Settings", "Settings", "Mangement of configuration settings", Image_Helper.ImageFromEmbedded("Images.Settings.png"), Settings_Control.Control_Name, null, "Administration");
                container.RegisterInstance<ToolBar_Interface>("Settings_TBItem", Settings_TBItem);

                logger.Debug("Resolving Settings ViewModel");
                var settingsVM = this.container.Resolve<Settings_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(settingsVM.View, Settings_Control.Control_Name);

                // Settings
                ///////////////////////////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////////////////////////
                // Parts_Management

                var Parts_Management_TBItem = new ToolBar_Item("Parts_Management", "Parts Management", "Manage part number cross referencing", Image_Helper.ImageFromEmbedded("Images.Parts_Management.png"), Parts_Management_Control.Control_Name, null, "Administration");
                container.RegisterInstance<ToolBar_Interface>("Parts_Management_TBItem", Parts_Management_TBItem);

                logger.Debug("Resolving Parts_Management ViewModel");
                var parts_managementVM = this.container.Resolve<Parts_Management_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(parts_managementVM.View, Parts_Management_Control.Control_Name);

                // Parts_Management
                ///////////////////////////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////////////////////////
                // AdminReturn
                var AdminReturn_TBRet = new ToolBar_Return("AdminReturn_RET", "Return to Main Menu", "Administration", Image_Helper.ImageFromEmbedded("Images.Return.png"));
                container.RegisterInstance<ToolBar_Interface>("AdminReturn_TBRet", AdminReturn_TBRet);
                // AdminReturn
                ///////////////////////////////////////////////////////////////////////////////////////



            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
            finally
            {
                logger.Debug("Leaving");
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Override
            var Override_TBItem = new ToolBar_Item("Override", "Override", "Override", Image_Helper.ImageFromEmbedded("Images.Override.png"), Override_Control.Control_Name, null, "Administration");
            container.RegisterInstance<ToolBar_Interface>("Override_TBItem", Override_TBItem);
            logger.Debug("Resolving Override ViewModel");
            var overrideVM = this.container.Resolve<Override_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(overrideVM.View, Override_Control.Control_Name);
            // Override
            ///////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////////
            // Parts
            var Parts_TBItem = new ToolBar_Item("Parts", "Parts", "Parts", Image_Helper.ImageFromEmbedded("Images.Parts.png"), Parts_Control.Control_Name, null, "Administration");
            container.RegisterInstance<ToolBar_Interface>("Parts_TBItem", Parts_TBItem);
            logger.Debug("Resolving Parts ViewModel");
            var partsVM = this.container.Resolve<Parts_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(partsVM.View, Parts_Control.Control_Name);
            // Parts
            ///////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////////
            // Users
            var Users_TBItem = new ToolBar_Item("Users", "Users", "Users", Image_Helper.ImageFromEmbedded("Images.Users.png"), Users_Control.Control_Name, null, "Administration");
            container.RegisterInstance<ToolBar_Interface>("Users_TBItem", Users_TBItem);
            logger.Debug("Resolving Users ViewModel");
            var usersVM = this.container.Resolve<Users_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(usersVM.View, Users_Control.Control_Name);
            // Users
            ///////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////////
            // Return
            var Return_TBRet = new ToolBar_Return("Return_TBRet_RET", "Return To Main Menu", "Administration", Image_Helper.ImageFromEmbedded("Return.png"));
            container.RegisterInstance<ToolBar_Interface>("Return_TBRet", Return_TBRet);
            logger.Debug("Resolving Return ViewModel");
            var returnVM = this.container.Resolve<Return_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(returnVM.View, Return_Control.Control_Name);
            // Return
            ///////////////////////////////////////////////////////////////////////////////////////

            /////////////////////////////////////////////////////////////////////////////////////////
            //// Return
            //var Return_TBRet_TBRet = new ToolBar_Return("Return_TBRet_RET", "Return To Main Menu", "Administration", Image_Helper.ImageFromEmbedded("Return.png"));
            //container.RegisterInstance<ToolBar_Interface>("Return_TBRet", Return_TBRet);
            //logger.Debug("Resolving Return ViewModel");
            //var returnVM = this.container.Resolve<Return_ViewModel_Interface>();
            //regionManager.Regions[RegionNames.MainRegion].Add(returnVM.View, Return_Control.Control_Name);
            //// Return
            /////////////////////////////////////////////////////////////////////////////////////////
        }

        private void Register_Types()
        {

            //container.RegisterType<Main_ViewModel_Interface, Main_ViewModel>();
            //container.RegisterType<Main_Control_Interface, Main_Control>();
            //container.RegisterType<Object, Main_Control>("Main_Control");

            container.RegisterType<Override_Management_ViewModel_Interface, Override_Management_ViewModel>();
            container.RegisterType<Override_Management_Control_Interface, Override_Management_Control>();
            container.RegisterType<Object, Override_Management_Control>(Override_Management_Control.Control_Name);

            container.RegisterType<User_Management_ViewModel_Interface, User_Management_ViewModel>();
            container.RegisterType<User_Management_Control_Interface, User_Management_Control>();
            container.RegisterType<Object, User_Management_Control>(User_Management_Control.Control_Name);

            container.RegisterType<Settings_ViewModel_Interface, Settings_ViewModel>();
            container.RegisterType<Settings_Control_Interface, Settings_Control>();
            container.RegisterType<Object, Settings_Control>(Settings_Control.Control_Name);

            container.RegisterType<Parts_Management_ViewModel_Interface, Parts_Management_ViewModel>();
            container.RegisterType<Parts_Management_Control_Interface, Parts_Management_Control>();
            container.RegisterType<Object, Parts_Management_Control>(Parts_Management_Control.Control_Name);

            container.RegisterType<Override_ViewModel_Interface, Override_ViewModel>();
            container.RegisterType<Override_Control_Interface, Override_Control>();
            container.RegisterType<Object, Override_Control>(Override_Control.Control_Name);

            container.RegisterType<Parts_ViewModel_Interface, Parts_ViewModel>();
            container.RegisterType<Parts_Control_Interface, Parts_Control>();
            container.RegisterType<Object, Parts_Control>(Parts_Control.Control_Name);

            container.RegisterType<Users_ViewModel_Interface, Users_ViewModel>();
            container.RegisterType<Users_Control_Interface, Users_Control>();
            container.RegisterType<Object, Users_Control>(Users_Control.Control_Name);

            container.RegisterType<Return_ViewModel_Interface, Return_ViewModel>();
            container.RegisterType<Return_Control_Interface, Return_Control>();
            container.RegisterType<Object, Return_Control>(Return_Control.Control_Name);
        }
    }
}
