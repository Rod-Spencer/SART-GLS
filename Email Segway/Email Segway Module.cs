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

namespace Segway.Service.SART.Email
{
    /// <summary>Public Class - Email_Segway_Module</summary>
    [Module(ModuleName = "Email_Segway_Module")]
    public class Email_Segway_Module : IModule
    {
        static Logger logger = Logger_Helper.GetCurrentLogger();

        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private readonly IEventAggregator aggregator;

        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Email_Segway_Module(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            logger.Debug("Entering - Email_Segway_Module");
            this.container = container;
            this.regionManager = regionManager;
            this.aggregator = eventAggregator;
        }

        /// <summary>Public Method - Initialize</summary>
        /// <returns>void</returns>
        public void Initialize()
        {
            logger.Debug("Entering - Email_Segway_Module.Initialize");
            Register_Types();

            // create a ToolBar_Item to represent the CU Log View
            var tbi_Email_Segway_Module = new ToolBar_Item("Email Segway", "Send an email to Segway Service to schedule an appointment", Image_Helper.ImageFromEmbedded("Images.Email Segway.png"), EmailSegway_Control.Control_Name);
            container.RegisterInstance<ToolBar_Interface>("tbi_Email_Segway_Module", tbi_Email_Segway_Module);

            logger.Debug("Resolving Email_Segway ViewModel");
            var email_segwayVM = this.container.Resolve<Email_Segway_ViewModel_Interface>();
            regionManager.Regions[RegionNames.MainRegion].Add(email_segwayVM.View, EmailSegway_Control.Control_Name);

            logger.Debug("Exiting - Email_Segway_Module.Initialize");
        }

        private void Register_Types()
        {

            container.RegisterType<Email_Segway_ViewModel_Interface, Email_Segway_ViewModel>();
            container.RegisterType<EmailSegway_Control_Interface, EmailSegway_Control>();
            container.RegisterType<Object, EmailSegway_Control>(EmailSegway_Control.Control_Name);
        }
    }
}
