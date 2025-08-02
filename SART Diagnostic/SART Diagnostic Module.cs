using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ShellControls;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Tools.ModuleBase;
using System;

namespace Segway.Modules.Diagnostic
{
    /// <summary>Public Class - SART_Diagnostic_Module</summary>
    [Module(ModuleName = "SART_Diagnostic_Module")]
    public class SART_Diagnostic_Module : Module_Base, IModule
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;

        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        public SART_Diagnostic_Module(IUnityContainer container, IRegionManager regionManager)
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

                var diagnosticVM = this.container.Resolve<Diagnostic_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(diagnosticVM.View);

                // create a ToolBar_Item to represent the CU Log View
                var DiagnosticsToolBarItem = new ToolBar_Item("Diagnostics", "Display the Diagnostics form", Image_Helper.ImageFromEmbedded("Images.diagnostics.png"), "Diagnostic_Control");
                container.RegisterInstance<ToolBar_Interface>("Diagnostic_ToolBarItem", DiagnosticsToolBarItem);
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
            container.RegisterType<Diagnostic_ViewModel_Interface, Diagnostic_ViewModel>();
            container.RegisterType<Diagnostic_Control_Interface, Diagnostic_Control>();
            container.RegisterType<Object, Diagnostic_Control>("Diagnostic_Control");
        }
    }
}
