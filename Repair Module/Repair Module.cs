using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ShellControls;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Tools.ModuleBase;
using System;

namespace Segway.Modules.SART.Repair
{
    /// <summary>Public Class - Repair_Module</summary>
    /// <summary>Public Class - Repair_Module</summary>
    [Module(ModuleName = "Repair_Module")]
    public class Repair_Module : Module_Base, IModule
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;


        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        public Repair_Module(IUnityContainer container, IRegionManager regionManager)
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


                var repairVM = this.container.Resolve<Repair_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(repairVM.View);

                var repairToolBarItem = new ToolBar_Item("Repairs", "Entry screen to record replaced components", Image_Helper.ImageFromEmbedded("Images.Repair.png"), "Repair_Control");
                container.RegisterInstance<ToolBar_Interface>("repairToolBarItem", repairToolBarItem);
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
            container.RegisterType<Repair_ViewModel_Interface, Repair_ViewModel>();
            container.RegisterType<Repair_Control_Interface, Repair_Control>();
            container.RegisterType<Object, Repair_Control>("Repair_Control");
        }
    }
}
