using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ShellControls;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Tools.ModuleBase;
using System;

namespace Segway.Modules.SART.CodeLoad
{
    /// <summary>Public Class - CU_Code_Load_Module</summary>
    /// <summary>Public Class - CU_Code_Load_Module</summary>
    [Module(ModuleName = "CU_Code_Load_Module")]
    public class CU_Code_Load_Module : Module_Base, IModule
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;

        /// <summary>Public Member - ToolBar_Name</summary>
        public static readonly String ToolBar_Name = "Load Code";
        /// <summary>Contructor</summary>
        /// <param name="container">IUnityContainer</param>
        /// <param name="regionManager">IRegionManager</param>
        public CU_Code_Load_Module(IUnityContainer container, IRegionManager regionManager)
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

                var cu_codeVM = this.container.Resolve<CU_Code_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(cu_codeVM.View);

                // create a ToolBar_Item to represent the CU Log View
                var CULogToolBarItem = new ToolBar_Item(ToolBar_Name, "Display the Load Code form", Image_Helper.ImageFromEmbedded("Images.loadcode.png"), "CU_Code_Control");
                container.RegisterInstance<ToolBar_Interface>("CU_Code_LogToolBarItem", CULogToolBarItem);
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
            container.RegisterType<CU_Code_ViewModel_Interface, CU_Code_ViewModel>();
            container.RegisterType<CU_Code_Control_Interface, CU_Code_Control>();
            container.RegisterType<Object, CU_Code_Control>("CU_Code_Control");

        }
    }
}
