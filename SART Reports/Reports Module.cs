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

namespace Segway.SART.Reports
{
    /// <summary>Public Class - Reports_Module</summary>
    [Module(ModuleName = "Reports_Module")]
    public class Reports_Module : IModule
    {
        static Logger logger = Logger_Helper.GetCurrentLogger();

        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private readonly IEventAggregator aggregator;

        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Reports_Module(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            logger.Debug("Entering - Reports_Module");
            this.container = container;
            this.regionManager = regionManager;
            this.aggregator = eventAggregator;
        }

        /// <summary>Public Method - Initialize</summary>
        /// <returns>void</returns>
        public void Initialize()
        {
            logger.Debug("Entering - Reports_Module.Initialize");
            Register_Types();

            // create a ToolBar_Item to represent the CU Log View
            var tbi_Reports_Module = new ToolBar_Item("Reports", "Reports for SART", Image_Helper.ImageFromEmbedded("Images.Report.png"), Report_Control.Control_Name);
            container.RegisterInstance<ToolBar_Interface>("tbi_Reports_Module", tbi_Reports_Module);

            logger.Debug("Resolving Report ViewModel");
            var reportVM = this.container.Resolve<Report_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(reportVM.View, Report_Control.Control_Name);

            logger.Debug("Exiting - Reports_Module.Initialize");
        }

        private void Register_Types()
        {

            container.RegisterType<Report_ViewModel_Interface, Report_ViewModel>();
            container.RegisterType<Report_Control_Interface, Report_Control>();
            container.RegisterType<Object, Report_Control>(Report_Control.Control_Name);
        }
    }
}
