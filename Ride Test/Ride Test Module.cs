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

namespace Segway.Service.SART.RideTest
{
    /// <summary>	public Class - Ride_Test_Module</summary>
    [Module(ModuleName = "Ride_Test_Module")]
    public class Ride_Test_Module : IModule
    {
        static Logger logger = Logger_Helper.GetCurrentLogger();

        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private readonly IEventAggregator aggregator;

        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Ride_Test_Module(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            logger.Debug("Entering - Ride_Test_Module");
            this.container = container;
            this.regionManager = regionManager;
            this.aggregator = eventAggregator;
        }

        /// <summary>Public Method - Initialize</summary>
        public void Initialize()
        {
            logger.Debug("Entering - Ride_Test_Module.Initialize");
            Register_Types();

            // create a ToolBar_Item to represent the CU Log View
            var tbi_Ride_Test_Module = new ToolBar_Item("Ride Test", "Ride Test Checklist", Image_Helper.ImageFromEmbedded("Images.Ride Test.png"), Ride_Test_Control.Control_Name);
            container.RegisterInstance<ToolBar_Interface>("tbi_Ride_Test_Module", tbi_Ride_Test_Module);

            logger.Debug("Resolving Ride_Test ViewModel");
            var ride_testVM = this.container.Resolve<Ride_Test_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(ride_testVM.View, Ride_Test_Control.Control_Name);

            logger.Debug("Exiting - Ride_Test_Module.Initialize");
        }

        private void Register_Types()
        {

            container.RegisterType<Ride_Test_ViewModel_Interface, Ride_Test_ViewModel>();
            container.RegisterType<Ride_Test_Control_Interface, Ride_Test_Control>();
            container.RegisterType<Object, Ride_Test_Control>(Ride_Test_Control.Control_Name);
        }
    }
}
