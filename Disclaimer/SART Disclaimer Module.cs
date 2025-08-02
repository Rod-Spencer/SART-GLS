using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.ShellControls;
using Segway.Service.ExceptionHelper;
using Segway.Service.Tools.ModuleBase;
using System;


namespace Segway.Service.Disclaimer
{
    /// <summary>Public Class - SART_Disclaimer_Module</summary>
    [Module(ModuleName = "SART_Disclaimer_Module")]
    [ModuleDependency("LoginModule")]
    [ModuleDependency("WorkOrderModule")]
    public class SART_Disclaimer_Module : Module_Base, IModule
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private readonly IEventAggregator aggregator;

        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public SART_Disclaimer_Module(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            this.container = container;
            this.regionManager = regionManager;
            this.aggregator = eventAggregator;
        }

        /// <summary>Public Method - Initialize</summary>
        /// <returns>void</returns>
        public void Initialize()
        {
            try
            {
                logger.Debug("Entered");
                Register_Types();

                var disclaimerVM = this.container.Resolve<Disclaimer_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(disclaimerVM.View);
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
        }

        private void Register_Types()
        {
            container.RegisterType<Disclaimer_ViewModel_Interface, Disclaimer_ViewModel>();
            container.RegisterType<Disclaimer_Control_Interface, Disclaimer_Control>();
            container.RegisterType<Object, Disclaimer_Control>("Disclaimer_Control");
        }
    }
}
