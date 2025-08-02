using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using NLog;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ShellControls;
using Segway.Service.Helper;
using Segway.Service.LoggerHelper;
using System;

namespace Segway.Modules.Administration
{
    /// <summary>Public Class - Administration_Module</summary>
    [Module(ModuleName = "Administration_Module")]
    public class Administration_Module : IModule
    {
        static Logger logger = Logger_Helper.GetCurrentLogger();

        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private readonly IEventAggregator aggregator;

        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Administration_Module(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            logger.Debug("Entering - Administration_Module");
            this.container = container;
            this.regionManager = regionManager;
            this.aggregator = eventAggregator;
        }

        /// <summary>Public Method - Initialize</summary>
        public void Initialize()
        {
            logger.Debug("Entering - Administration_Module.Initialize");
            Register_Types();

            ///////////////////////////////////////////////////////////////////////////////////////
            // Administration
            var Administration_BTList = new ToolBar_List("Administration", "Administration", "Management of Work Order Overrides, Parts, Users, and Application Settings", null, Image_Helper.ImageFromEmbedded("Images.Administration.png"));
            container.RegisterInstance<ToolBar_Interface>("Administration_BTList", Administration_BTList);
            // Administration
            ///////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////////
            // Overrides
            var Overrides_TBItem = new ToolBar_Item("Override", "Work Order Overrides", "Management of Work Order Overrides and Virtual Closing of an Application", Image_Helper.ImageFromEmbedded("Images.Override.png"), Overrides_Control.Control_Name, null, "Administration");
            container.RegisterInstance<ToolBar_Interface>("Overrides_TBItem", Overrides_TBItem);

            logger.Debug("Resolving Overrides ViewModel");
            var overridesVM = this.container.Resolve<Overrides_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(overridesVM.View, Overrides_Control.Control_Name);
            // Overrides
            ///////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////////
            // Parts
            var Parts_TBItem = new ToolBar_Item("Parts", "Parts Management", "Management of Part Numbers", Image_Helper.ImageFromEmbedded("Images.Parts.png"), Parts_Control.Control_Name, null, "Administration");
            container.RegisterInstance<ToolBar_Interface>("Parts_TBItem", Parts_TBItem);
            logger.Debug("Resolving Parts ViewModel");
            var partsVM = this.container.Resolve<Parts_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(partsVM.View, Parts_Control.Control_Name);
            // Parts
            ///////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////////
            // Settings
            var Settings_TBItem = new ToolBar_Item("Settings", "Application Settings", "Management of Application Settings", Image_Helper.ImageFromEmbedded("Images.Settings.png"), Settings_Control.Control_Name, null, "Administration");
            container.RegisterInstance<ToolBar_Interface>("Settings_TBItem", Settings_TBItem);
            logger.Debug("Resolving Settings ViewModel");
            var settingsVM = this.container.Resolve<Settings_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(settingsVM.View, Settings_Control.Control_Name);
            // Settings
            ///////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////////
            // Users
            var Users_TBItem = new ToolBar_Item("Users", "User Settings Management", "Management of User Access, JTag, Application, and Regional Settings", Image_Helper.ImageFromEmbedded("Images.Users.png"), Users_Control.Control_Name, null, "Administration");
            container.RegisterInstance<ToolBar_Interface>("Users_TBItem", Users_TBItem);
            logger.Debug("Resolving Users ViewModel");
            var usersVM = this.container.Resolve<Users_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(usersVM.View, Users_Control.Control_Name);
            // Users
            ///////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////////
            // Model
            var Model_TBItem = new ToolBar_Item("Model", "Model Management", "Convert a PT model", Image_Helper.ImageFromEmbedded("Images.Model.png"), Model_Control.Control_Name, null, "Administration");
            container.RegisterInstance<ToolBar_Interface>("Model_TBItem", Model_TBItem);
            logger.Debug("Resolving Model ViewModel");
            var modelVM = this.container.Resolve<Model_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(modelVM.View, Model_Control.Control_Name);
            // Model
            ///////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////////
            // Model
            var SRO_TBItem = new ToolBar_Item("SRO", "Create SRO", "Create an Work Order for a PT", Image_Helper.ImageFromEmbedded("Images.SRO.png"), Create_SRO_Control.Control_Name, null, "Administration");
            container.RegisterInstance<ToolBar_Interface>("SRO_TBItem", SRO_TBItem);
            logger.Debug("Resolving Create SRO ViewModel");
            var SROVM = this.container.Resolve<Create_SRO_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(SROVM.View, Create_SRO_Control.Control_Name);
            // Model
            ///////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////////
            // Return
            var Return_TBRet = new ToolBar_Return("Administration_TBRet", "Return To Main Menu", "Administration", Image_Helper.ImageFromEmbedded("Return.png"));
            container.RegisterInstance<ToolBar_Interface>("Return_TBRet", Return_TBRet);
            // Return
            ///////////////////////////////////////////////////////////////////////////////////////


            logger.Debug("Exiting - Administration_Module.Initialize");
        }

        private void Register_Types()
        {
            container.RegisterType<Overrides_ViewModel_Interface, Overrides_ViewModel>();
            container.RegisterType<Overrides_Control_Interface, Overrides_Control>();
            container.RegisterType<Object, Overrides_Control>(Overrides_Control.Control_Name);

            container.RegisterType<Parts_ViewModel_Interface, Parts_ViewModel>();
            container.RegisterType<Parts_Control_Interface, Parts_Control>();
            container.RegisterType<Object, Parts_Control>(Parts_Control.Control_Name);

            container.RegisterType<Settings_ViewModel_Interface, Settings_ViewModel>();
            container.RegisterType<Settings_Control_Interface, Settings_Control>();
            container.RegisterType<Object, Settings_Control>(Settings_Control.Control_Name);

            container.RegisterType<Users_ViewModel_Interface, Users_ViewModel>();
            container.RegisterType<Users_Control_Interface, Users_Control>();
            container.RegisterType<Object, Users_Control>(Users_Control.Control_Name);

            container.RegisterType<Model_ViewModel_Interface, Model_ViewModel>();
            container.RegisterType<Model_Control_Interface, Model_Control>();
            container.RegisterType<Object, Model_Control>(Model_Control.Control_Name);

            container.RegisterType<Create_SRO_ViewModel_Interface, Create_SRO_ViewModel>();
            container.RegisterType<Create_SRO_Control_Interface, Create_SRO_Control>();
            container.RegisterType<Object, Create_SRO_Control>(Create_SRO_Control.Control_Name);
        }
    }
}
