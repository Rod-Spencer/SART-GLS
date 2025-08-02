using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ShellControls;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Tools.ModuleBase;
using System;

namespace Segway.Modules.WorkOrder
{
    [Module(ModuleName = "WorkOrderModule")]
    [ModuleDependency("LoginModule")]
    public class WorkOrderModule : Module_Base, IModule
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;


        public static readonly String StatusCodesName = "Status_Codes";

        public WorkOrderModule(IUnityContainer container, IRegionManager regionManager)
        {
            this.container = container;
            this.regionManager = regionManager;
        }

        public void Initialize()
        {
            try
            {
                logger.Debug("Entered");
                Register_Types();

                //var wo = this.container.Resolve<Work_Order_Interface>();

                //// resolve the view model and the view, and add them to the Main Region's view collection
                ////  so we can navigate to it later when needed.  This could also be done in code elsewhere, 
                ////  right before requesting navigation, but cleaner to do it here.
                //var vm = this.container.Resolve<IWorkOrderViewModel>();
                //var vm2 = this.container.Resolve<IWorkOrderSummaryViewModel>();

                ////regionManager.Regions[RegionNames.MainRegion].Add(vm.View, "New_WorkOrder");
                //regionManager.Regions[RegionNames.MainRegion].Add(vm.View);
                //regionManager.Regions[RegionNames.MainRegion].Add(vm2.View);

                // create a ToolBar_Item to represent the Summary Work Order View
                var WorkOrderToolBarItem = new ToolBar_Item("Work Order", "Display the Work Order Summary View", Image_Helper.ImageFromEmbedded("Images.workorder.png"), Work_Order_Open_Control.Control_Name);
                container.RegisterInstance<ToolBar_Interface>("WorkOrderToolBarItem", WorkOrderToolBarItem);

                logger.Debug("Resolving WorkOrder_Update ViewModel");
                var workorder_updateVM = this.container.Resolve<Work_Order_Update_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(workorder_updateVM.View, Work_Order_Update_Control.Control_Name);

                logger.Debug("Resolving Work_Order_Open ViewModel");
                var work_order_openVM = this.container.Resolve<Work_Order_Open_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(work_order_openVM.View, Work_Order_Open_Control.Control_Name);

                logger.Debug("Resolving Work_Order_Summary ViewModel");
                var work_order_summaryVM = this.container.Resolve<Work_Order_Summary_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(work_order_summaryVM.View, Work_Order_Summary_Control.Control_Name);

                var work_order_configurationVM = this.container.Resolve<Work_Order_Configuration_ViewModel_Interface>();
                regionManager.Regions[RegionNames.MainRegion].Add(work_order_configurationVM.View);
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
            //container.RegisterType<Object, Dictionary<String, String>>(WorkOrderModule.StatusCodesName, new ContainerControlledLifetimeManager());

            container.RegisterType<Work_Order_Configuration_ViewModel_Interface, Work_Order_Configuration_ViewModel>(new ContainerControlledLifetimeManager());
            container.RegisterType<Work_Order_Configuration_Control_Interface, Work_Order_Configuration_Control>();
            container.RegisterType<Object, Work_Order_Configuration_Control>(Work_Order_Configuration_Control.Control_Name);

            container.RegisterType<Work_Order_Update_ViewModel_Interface, Work_Order_Update_ViewModel>();
            container.RegisterType<Work_Order_Update_Control_Interface, Work_Order_Update_Control>();
            container.RegisterType<Object, Work_Order_Update_Control>(Work_Order_Update_Control.Control_Name);

            container.RegisterType<Work_Order_Open_ViewModel_Interface, Work_Order_Open_ViewModel>();
            container.RegisterType<Work_Order_Open_Control_Interface, Work_Order_Open_Control>();
            container.RegisterType<Object, Work_Order_Open_Control>(Work_Order_Open_Control.Control_Name);

            container.RegisterType<Work_Order_Summary_ViewModel_Interface, Work_Order_Summary_ViewModel>();
            container.RegisterType<Work_Order_Summary_Control_Interface, Work_Order_Summary_Control>();
            container.RegisterType<Object, Work_Order_Summary_Control>(Work_Order_Summary_Control.Control_Name);
        }

    }
}
