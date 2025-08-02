using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Login.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ListBox_Item_Def;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder;
using Segway.Service.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Segway.Modules.CU_Log_Module
{
    public class CU_Log_ViewModel : ViewModelBase, CU_Log_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator aggregator;


        public CU_Log_ViewModel(CU_Log_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.aggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<Application_Logout_Event>().Subscribe(Application_Logout_Handler, ThreadOption.BackgroundThread, true);
            eventAggregator.GetEvent<WorkOrder_Opened_Event>().Subscribe(WorkOrder_Opened_Handler, ThreadOption.BackgroundThread, true);
            //eventAggregator.GetEvent<SelectLogPanelEvent>().Subscribe(SelectLogPanel_Handler, true);

            #endregion

            #region Command Setups

            #endregion

            WindowList = new ObservableCollection<List_Box_Item_Definition>();
            WindowList.Add(new List_Box_Item_Definition("Extract Event Log", Image_Helper.ImageFromEmbedded("Images.new.png"), (IViewModel)container.Resolve<CU_Log_Extraction_View_Model_Interface>(), IsWorkOrderOpen));
            WindowList.Add(new List_Box_Item_Definition("Open Event Log", Image_Helper.ImageFromEmbedded("Images.open.png"), (IViewModel)container.Resolve<CU_Log_Open_ViewModel_Interface>(), true));
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        #region IsWorkOrderOpen

        /// <summary>Property IsWorkOrderOpen of type SART_Work_Order</summary>
        public Boolean IsWorkOrderOpen
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return false;
                return String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Opened_By) == false;
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        #region WindowList

        private ObservableCollection<List_Box_Item_Definition> _WindowList;

        /// <summary>Property WindowList of type List<List_Box_Item_Definition></summary>
        public ObservableCollection<List_Box_Item_Definition> WindowList
        {
            get { return _WindowList; }
            set
            {
                _WindowList = value;
                OnPropertyChanged("WindowList");
            }
        }

        #endregion

        #region SelectedPanel

        private int _SelectedPanel = 1;

        /// <summary>Property SelectedPanel of type int</summary>
        public int SelectedPanel
        {
            get { return _SelectedPanel; }
            set
            {
                _SelectedPanel = value;

                if (_SelectedPanel == 0)
                {
                    this.aggregator.GetEvent<OpenExtractPanelEvent>().Publish(true);
                }
                OnPropertyChanged("SelectedPanel");
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers
        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Logout_Handler  -- Event: Application_Logout_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Logout_Handler(Boolean IsLogout)
        {
            if (IsLogout) SelectedPanel = 1;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Close_Handler  -- Event: SART_WorkOrder_Close_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Close_Handler(Boolean closed)
        {
            SelectedPanel = 1;
            WindowList[0].IsDefinitionEnabled = false;
            OnPropertyChanged("IsDefinitionEnabled");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Cancel_Handler  -- Event: SART_WorkOrder_Cancel_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Cancel_Handler(Boolean cancelled)
        {
            SelectedPanel = 1;
            WindowList[0].IsDefinitionEnabled = false;
            OnPropertyChanged("IsDefinitionEnabled");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //#region SelectLogPanel_Handler  -- Event: SelectLogPanel_Event Handler
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //private void SelectLogPanel_Handler(int nPanel)
        //{
        //    SelectedPanel = nPanel;
        //}

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //#endregion
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Opened_Handler  -- Event: WorkOrder_Opened_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Opened_Handler(Boolean obj)
        {
            OnPropertyChanged("IsDefinitionEnabled");
            //Application.Current.Dispatcher.Invoke((Action)delegate ()
            //{
            //    List<List_Box_Item_Definition> lbiList = new List<List_Box_Item_Definition>(WindowList);
            //    WindowList = new ObservableCollection<List_Box_Item_Definition>(lbiList);
            //});
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //aggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            aggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Event Logs", "CU_Log_Control"));
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            WindowList[0].IsDefinitionEnabled = IsWorkOrderOpen;
            if (IsWorkOrderOpen == false) { SelectedPanel = 1; }
            aggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            aggregator.GetEvent<ToolBar_Selection_Event>().Publish("Event Logs");

            for (int x = 0; x < WindowList.Count; x++)
            {
                var vm = WindowList[x].ViewModel;
                if (vm == null) continue;
                Type t = vm.GetType();
                if (t == null) continue;
                MethodInfo mi = t.GetMethod("OnSelected");
                if (mi == null) continue;
                mi.Invoke(vm, null);
            }
            OnPropertyChanged("IsDefinitionEnabled");
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods
        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
