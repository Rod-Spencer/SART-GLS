using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Login.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.ListBox_Item_Def;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Service.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Segway.Service.SART
{
    /// <summary>Public Class - BlackBox_ViewModel</summary>
    public class BlackBox_ViewModel : ViewModelBase, BlackBox_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">BlackBox_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public BlackBox_ViewModel(BlackBox_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<Application_Logout_Event>().Subscribe(Application_Logout_Handler, true);
            eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, true);

            #endregion

            #region Command Delegates

            #endregion

            WindowList = new ObservableCollection<List_Box_Item_Definition>();
            WindowList.Add(new List_Box_Item_Definition("Extract BlackBox", Image_Helper.ImageFromEmbedded("Images.BBExtract.png"), (IViewModel)container.Resolve<BlackBox_Extraction_ViewModel_Interface>(), IsWorkOrderOpen));
            WindowList.Add(new List_Box_Item_Definition("Open BlackBox", Image_Helper.ImageFromEmbedded("Images.BBopen.png"), (IViewModel)container.Resolve<BlackBox_Open_ViewModel_Interface>(), true));
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

        #region Popup Controls

        #region PopupMessage

        private String _PopupMessage;

        /// <summary>ViewModel Property: PopupMessage of type: String</summary>
        public String PopupMessage
        {
            get { return _PopupMessage; }
            set
            {
                _PopupMessage = value;
                OnPropertyChanged("PopupMessage");
            }
        }

        #endregion

        #region PopupOpen

        private Boolean _PopupOpen;

        /// <summary>ViewModel Property: PopupOpen of type: Boolean</summary>
        public Boolean PopupOpen
        {
            get { return _PopupOpen; }
            set
            {
                _PopupOpen = value;
                OnPropertyChanged("PopupOpen");
            }
        }

        #endregion

        #region PopupColor

        private Brush _PopupColor;

        /// <summary>ViewModel Property: PopupColor of type: Brush</summary>
        public Brush PopupColor
        {
            get { return _PopupColor; }
            set
            {
                _PopupColor = value;
                OnPropertyChanged("PopupColor");
            }
        }

        #endregion

        #endregion


        #region WindowList

        private ObservableCollection<List_Box_Item_Definition> _WindowList;

        /// <summary>Property WindowList of type List&lt;List_Box_Item_Definition&gt;</summary>
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
                eventAggregator.GetEvent<BlackBox_Open_Extract_Panel_Event>().Publish(WindowList[_SelectedPanel].ViewModel);
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

        private void Application_Logout_Handler(Boolean logout)
        {
            if (logout == true) SelectedPanel = 1;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Close_Handler  -- Event: SART_WorkOrder_Close_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Close_Handler(Boolean closed)
        {
            Application_Logout_Handler(closed);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Cancel_Handler  -- Event: SART_WorkOrder_Cancel_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Cancel_Handler(Boolean canceled)
        {
            Application_Logout_Handler(canceled);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        /// <summary>Public Method - IsNavigationTarget</summary>
        /// <param name="navigationContext">NavigationContext</param>
        /// <returns>bool</returns>
        public bool IsNavigationTarget(NavigationContext navigationContext) { return true; }

        /// <summary>Public Method - OnNavigatedFrom</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            eventAggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>(BlackBox_Module.ToolBar_Name, BlackBox_Control.Control_Name));
        }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            WindowList[0].IsDefinitionEnabled = IsWorkOrderOpen;
            if (IsWorkOrderOpen == false) { SelectedPanel = 1; }

            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Selection_Event>().Publish(BlackBox_Module.ToolBar_Name);
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods
        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
