using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using NLog;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ShellControls;
using Segway.Service.Common.LoggerHelp;
using Segway.Service.Helper;
using System;

namespace Segway.Service.Bugs
{
    /// <summary>Public Class - Bug_Tracking_Module</summary>
    [Module(ModuleName = "Bug_Tracking_Module")]
    public class Bug_Tracking_Module : IModule
    {
        static Logger logger = LoggerHelper.GetCurrentLogger();

        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private readonly IEventAggregator aggregator;

        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Bug_Tracking_Module(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            logger.Debug("Entering - Bug_Tracking_Module");
            this.container = container;
            this.regionManager = regionManager;
            this.aggregator = eventAggregator;
        }

        /// <summary>Public Method - Initialize</summary>
        public void Initialize()
        {
            logger.Debug("Entering - Bug_Tracking_Module.Initialize");
            Register_Types();

            var tbi_Bug_Tracking_Module = new ToolBar_List("BTL", "Bug Tracking", "Bug Tracking Management", null, Image_Helper.ImageFromEmbedded("Images.Bug.png"), null, true);
            container.RegisterInstance<ToolBar_Interface>("tbi_Bug_Tracking_Module", tbi_Bug_Tracking_Module);


            ///////////////////////////////////////////////////////////////////////////////////////////////////////
            // Bug List
            var tbi_Bug_List_Module = new ToolBar_Item("BL", "Bug List", "Bug List", Image_Helper.ImageFromEmbedded("Images.Bug.png"), Bug_List_Control.Control_Name, null, "BTL");
            container.RegisterInstance<ToolBar_Interface>("tbi_Bug_List_Module", tbi_Bug_List_Module);


            logger.Debug("Resolving Bug_List ViewModel");
            var bug_listVM = this.container.Resolve<Bug_List_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(bug_listVM.View, Bug_List_Control.Control_Name);
            // Bug List
            ///////////////////////////////////////////////////////////////////////////////////////////////////////



            ///////////////////////////////////////////////////////////////////////////////////////////////////////
            // Bug Detail
            var tbi_Bug_Detail_Module = new ToolBar_Item("BD", "Bug Detail", "Bug Detail", Image_Helper.ImageFromEmbedded("Images.Bug.png"), Bug_Detail_Control.Control_Name, null, "BTL");
            container.RegisterInstance<ToolBar_Interface>("tbi_Bug_Detail_Module", tbi_Bug_Detail_Module);

            logger.Debug("Resolving Bug_Detail ViewModel");
            var bug_detailVM = this.container.Resolve<Bug_Detail_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(bug_detailVM.View, Bug_Detail_Control.Control_Name);
            // Bug Detail
            ///////////////////////////////////////////////////////////////////////////////////////////////////////


            var tbiRet = new ToolBar_Return("RET", "Return", "BTL", Image_Helper.ImageFromEmbedded("Return.png"));
            container.RegisterInstance<ToolBar_Interface>("tbiRet", tbiRet);


            logger.Debug("Exiting - Bug_Tracking_Module.Initialize");
        }

        private void Register_Types()
        {
            container.RegisterType<Bug_List_ViewModel_Interface, Bug_List_ViewModel>();
            container.RegisterType<Bug_List_Control_Interface, Bug_List_Control>();
            container.RegisterType<Object, Bug_List_Control>(Bug_List_Control.Control_Name);

            container.RegisterType<Bug_Detail_ViewModel_Interface, Bug_Detail_ViewModel>();
            container.RegisterType<Bug_Detail_Control_Interface, Bug_Detail_Control>();
            container.RegisterType<Object, Bug_Detail_Control>(Bug_Detail_Control.Control_Name);
        }
    }
}
