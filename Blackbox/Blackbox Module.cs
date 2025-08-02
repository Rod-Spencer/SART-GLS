using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ShellControls;
using Segway.Service.Helper;
using Segway.Service.Tools.ModuleBase;
using System;

namespace Segway.Service.SART
{
    /// <summary>Public Class - BlackBox_Module</summary>
    [Module(ModuleName = "BlackBox_Module")]
    public class BlackBox_Module : Module_Base, IModule
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private readonly IEventAggregator aggregator;

        /// <summary>Public Member - ToolBar_Name</summary>
        public static readonly String ToolBar_Name = "Black Box";

        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public BlackBox_Module(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            logger.Debug("Entering - BlackBox_Module");
            this.container = container;
            this.regionManager = regionManager;
            this.aggregator = eventAggregator;
        }

        /// <summary>Public Method - Initialize</summary>
        public void Initialize()
        {
            logger.Debug("Entering - BlackBox_Module.Initialize");
            Register_Types();

            // create a ToolBar_Item to represent the CU Log View
            var tbi_BlackBox_Module = new ToolBar_Item(ToolBar_Name, "Extract BSA BlackBox Data", Image_Helper.ImageFromEmbedded("Images.BlackBox.png"), BlackBox_Control.Control_Name);
            container.RegisterInstance<ToolBar_Interface>("tbi_BlackBox_Module", tbi_BlackBox_Module);

            logger.Debug("Resolving BlackBox ViewModel");
            var blackboxVM = this.container.Resolve<BlackBox_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(blackboxVM.View, BlackBox_Control.Control_Name);


            logger.Debug("Resolving BlackBox_Open ViewModel");
            var blackbox_openVM = this.container.Resolve<BlackBox_Open_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(blackbox_openVM.View, BlackBox_Open_Control.Control_Name);

            logger.Debug("Resolving BlackBox_Extraction ViewModel");
            var blackbox_extractionVM = this.container.Resolve<BlackBox_Extraction_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(blackbox_extractionVM.View, BlackBox_Extraction_Control.Control_Name);




            logger.Debug("Exiting - BlackBox_Module.Initialize");
        }

        private void Register_Types()
        {
            container.RegisterType<BlackBox_ViewModel_Interface, BlackBox_ViewModel>();
            container.RegisterType<BlackBox_Control_Interface, BlackBox_Control>();
            container.RegisterType<Object, BlackBox_Control>(BlackBox_Control.Control_Name);

            container.RegisterType<BlackBox_Open_ViewModel_Interface, BlackBox_Open_ViewModel>();
            container.RegisterType<BlackBox_Open_Control_Interface, BlackBox_Open_Control>();
            container.RegisterType<Object, BlackBox_Open_Control>(BlackBox_Open_Control.Control_Name);

            container.RegisterType<BlackBox_Extraction_ViewModel_Interface, BlackBox_Extraction_ViewModel>();
            container.RegisterType<BlackBox_Extraction_Control_Interface, BlackBox_Extraction_Control>();
            container.RegisterType<Object, BlackBox_Extraction_Control>(BlackBox_Extraction_Control.Control_Name);

            container.RegisterType<Object, BlackBox_Open_Settings_Control>(BlackBox_Open_Settings_Control.Control_Name);
        }
    }
}
