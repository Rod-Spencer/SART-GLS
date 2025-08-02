//using System.Linq;
//using System.Text;

using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ShellControls;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Tools.ModuleBase;
using System;
//using Segway.Modules.Progression_Log_Viewer;
//using Segway.SART.Objects;


namespace Segway.Modules.CU_Log_Module
{
    [Module(ModuleName = "CU_Log_Module")]
    [ModuleDependency("WorkOrderModule")]
    public class CU_Log_Module : Module_Base, IModule
    {
        public static IUnityContainer CU_Log_Module_Container;

        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;

        public CU_Log_Module(IUnityContainer container, IRegionManager regionManager)
        {
            CU_Log_Module.CU_Log_Module_Container = this.container = container;
            this.regionManager = regionManager;
        }

        public void Initialize()
        {
            try
            {
                logger.Debug("Entered");
                Register_Types();

                // resolve the view model and the view, and add them to the Main Region's view collection
                //  so we can navigate to it later when needed.  This could also be done in code elsewhere, 
                //  right before requesting navigation, but cleaner to do it here.

                // create a ToolBar_Item to represent the CU Log View
                var CULogToolBarItem = new ToolBar_Item("Event Logs", "Display the CU Event Log form", Image_Helper.ImageFromEmbedded("Images.eventlogs.png"), "CU_Log_Control");
                container.RegisterInstance<ToolBar_Interface>("CULogToolBarItem", CULogToolBarItem);

                var cu_logVM = this.container.Resolve<CU_Log_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(cu_logVM.View);
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
            // Then register the type of the view against a name. Note: when using Unity for DI container, 
            //  the first parameter must be Object. The container resolves a view as an Object type, then assigns the given name
            //  as the type mapping name.  AND THE NAME MUST BE THE SAME AS THE TYPE NAME!

            container.RegisterType<Object, CU_Log_Open_Control>(CU_Log_Open_Control.Control_Name);
            container.RegisterType<Object, CU_Log_Extraction>(CU_Log_Extraction.Control_Name);

            container.RegisterType<CU_Log_ViewModel_Interface, CU_Log_ViewModel>();
            container.RegisterType<CU_Log_Open_ViewModel_Interface, CU_Log_Open_ViewModel>();
            container.RegisterType<CU_Log_Open_Control_Interface, CU_Log_Open_Control>(new ContainerControlledLifetimeManager());
            container.RegisterType<CU_Log_Extraction_View_Model_Interface, CU_Log_Extraction_View_Model>();
            container.RegisterType<CU_Log_Extraction_Interface, CU_Log_Extraction>(new ContainerControlledLifetimeManager());

            container.RegisterType<Object, CU_Log_Control>(CU_Log_Control.Control_Name);
            container.RegisterType<CU_Log_Control_Interface, CU_Log_Control>();
        }
    }
}
