using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.SART.Client.REST;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Segway.Service.SART.RideTest
{
    /// <summary>Public Class - Ride_Test_ViewModel</summary>
    public class Ride_Test_ViewModel : ViewModelBase, Ride_Test_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">Ride_Test_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Ride_Test_ViewModel(Ride_Test_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<WorkOrder_Opened_Event>().Subscribe(WorkOrder_Opened_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Delegates

            RequestToCloseCommand = new DelegateCommand(CommandRequestToClose, CanCommandRequestToClose);

            #endregion
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        #region Token

        private AuthenticationToken _Token;

        /// <summary>Property Token of type AuthenticationToken</summary>
        public AuthenticationToken Token
        {
            get
            {
                if (_Token == null)
                {
                    if (container.IsRegistered<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName) == true)
                    {
                        _Token = container.Resolve<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName);
                    }
                }
                return _Token;
            }
        }

        #endregion

        #region LoginContext

        private Login_Context _LoginContext = null;
        /// <summary>Property LoginContext of type Login_Context</summary>
        public Login_Context LoginContext
        {
            get
            {
                if (_LoginContext == null)
                {
                    if (Token != null) _LoginContext = Token.LoginContext;
                }
                return _LoginContext;
            }
        }
        #endregion

        #region RideTest

        private SART_Ride_Test _RideTest;

        /// <summary>Property RideTest of type String</summary>
        public SART_Ride_Test RideTest
        {
            get
            {
                if (_RideTest == null)
                {
                    if (InfrastructureModule.Current_Work_Order == null) return new SART_Ride_Test();
                    if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Work_Order_ID) == true) return new SART_Ride_Test();
                    _RideTest = new SART_Ride_Test();
                    _RideTest.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                    _RideTest.PT_Serial = InfrastructureModule.Current_Work_Order.PT_Serial;
                }
                return _RideTest;
            }
            set
            {
                _RideTest = value;
                OnPropertyChanged("RideTest");
            }
        }

        #endregion


        #region Ride_Test_Success

        /// <summary>Property Ride_Test_Success of type Boolean</summary>
        public Boolean Ride_Test_Success
        {
            get
            {
                return PowerOn_Checked && PowerOff_Checked && BalanceOff_Checked && BalanceOn_Checked && Stationary_Checked &&
                    AxisLeft_Checked && AxisRight_Checked && Accelerate_Checked && Decelerate_Checked;
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        #region ImagePath

        private BitmapImage _ImagePath = null;

        /// <summary>Property ImagePath of type BitmapImage</summary>
        public BitmapImage ImagePath
        {
            get
            {
                if (_ImagePath == null)
                {
                    _ImagePath = Image_Helper.ImageFromEmbedded("Images.Ride Test.png");
                }
                return _ImagePath;
            }
            set
            {
                _ImagePath = value;
                OnPropertyChanged("ImagePath");
            }
        }

        #endregion

        #region Work Order Info Bar Data

        #region Work_Order_Num

        /// <summary>Property Work_Order_Num of type String</summary>
        public String Work_Order_Num
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Work_Order_ID;
            }
            set { OnPropertyChanged("Work_Order_Num"); }
        }

        #endregion

        #region PTSerial

        /// <summary>Property PTSerial of type String</summary>
        public String PTSerial
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.PT_Serial;
            }
            set { OnPropertyChanged("PTSerial"); }
        }

        #endregion

        #region WorkOrderColor

        /// <summary>Property WorkOrderColor of type Brush</summary>
        public Brush WorkOrderColor
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null)
                {
                    return new BrushConverter().ConvertFromString("#FF67A1DC") as System.Windows.Media.Brush;
                }
                else if (InfrastructureModule.Current_Work_Order.Priority == "Incident")
                {
                    return System.Windows.Media.Brushes.Red;
                }
                else
                {
                    return new BrushConverter().ConvertFromString("#FF67A1DC") as System.Windows.Media.Brush;
                }
            }
            set { OnPropertyChanged("WorkOrderColor"); }
        }

        #endregion

        #endregion

        #region PowerOn_Checked

        /// <summary>Property PowerOn_Checked of type Boolean</summary>
        public Boolean PowerOn_Checked
        {
            get { return RideTest.Power_On; }
            set
            {
                RideTest.Power_On = value;
                OnPropertyChanged("PowerOn_Checked");
                eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Publish(Ride_Test_Success);
                RequestToCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region BalanceOn_Checked

        /// <summary>Property BalanceOn_Checked of type Boolean</summary>
        public Boolean BalanceOn_Checked
        {
            get { return RideTest.Balance_On; }
            set
            {
                RideTest.Balance_On = value;
                OnPropertyChanged("BalanceOn_Checked");
                eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Publish(Ride_Test_Success);
                RequestToCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Stationary_Checked

        /// <summary>Property Stationary_Checked of type Boolean</summary>
        public Boolean Stationary_Checked
        {
            get { return RideTest.Stationary; }
            set
            {
                RideTest.Stationary = value;
                OnPropertyChanged("Stationary_Checked");
                eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Publish(Ride_Test_Success);
                RequestToCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region AxisRight_Checked

        /// <summary>Property AxisRight_Checked of type Boolean</summary>
        public Boolean AxisRight_Checked
        {
            get { return RideTest.Axis_Right; }
            set
            {
                RideTest.Axis_Right = value;
                OnPropertyChanged("AxisRight_Checked");
                eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Publish(Ride_Test_Success);
                RequestToCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region AxisLeft_Checked

        /// <summary>Property AxisLeft_Checked of type Boolean</summary>
        public Boolean AxisLeft_Checked
        {
            get { return RideTest.Axis_Left; }
            set
            {
                RideTest.Axis_Left = value;
                OnPropertyChanged("AxisLeft_Checked");
                eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Publish(Ride_Test_Success);
                RequestToCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Accelerate_Checked

        /// <summary>Property Accelerate_Checked of type Boolean</summary>
        public Boolean Accelerate_Checked
        {
            get { return RideTest.Accelerate; }
            set
            {
                RideTest.Accelerate = value;
                OnPropertyChanged("Accelerate_Checked");
                eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Publish(Ride_Test_Success);
                RequestToCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Decelerate_Checked

        /// <summary>Property Decelerate_Checked of type Boolean</summary>
        public Boolean Decelerate_Checked
        {
            get { return RideTest.Decelerate; }
            set
            {
                RideTest.Decelerate = value;
                OnPropertyChanged("Decelerate_Checked");
                eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Publish(Ride_Test_Success);
                RequestToCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region BalanceOff_Checked

        /// <summary>Property BalanceOff_Checked of type Boolean</summary>
        public Boolean BalanceOff_Checked
        {
            get { return RideTest.Balance_Off; }
            set
            {
                RideTest.Balance_Off = value;
                OnPropertyChanged("BalanceOff_Checked");
                eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Publish(Ride_Test_Success);
                RequestToCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region PowerOff_Checked

        /// <summary>Property PowerOff_Checked of type Boolean</summary>
        public Boolean PowerOff_Checked
        {
            get { return RideTest.Power_Off; }
            set
            {
                RideTest.Power_Off = value;
                OnPropertyChanged("PowerOff_Checked");
                eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Publish(Ride_Test_Success);
                RequestToCloseCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

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

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region RequestToCloseCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: RequestToCloseCommand</summary>
        public DelegateCommand RequestToCloseCommand { get; set; }
        private Boolean CanCommandRequestToClose() { return Ride_Test_Success; }
        private void CommandRequestToClose()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (String.IsNullOrEmpty(RideTest.Work_Order) == false)
                {
                    if (InfrastructureModule.Current_Work_Order == null) throw new ApplicationException("No Work Order Found");
                    if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Work_Order_ID) == true) throw new ApplicationException("No Work Order ID Found");
                    RideTest.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                }

                if (SART_Ride_Web_Service_Client_REST.Request_To_Close(InfrastructureModule.Token, RideTest) == false)
                {
                    PopupColor = Brushes.LightGoldenrodYellow;
                    PopupMessage = String.Format("An error occurred while trying to submit the request to close Work Order: {0}.  Please try again or contact Segway Technical Support for further assistance.", InfrastructureModule.Current_Work_Order.Work_Order_ID);
                }
                else
                {
                    PopupColor = Brushes.LightGreen;
                    PopupMessage = String.Format("Successfully submited request to close Work Order: {0}", InfrastructureModule.Current_Work_Order.Work_Order_ID);
                    WorkOrder_Opened_Handler(false);
                }
                PopupOpen = true;
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = Brushes.Pink;
                PopupMessage = msg;
                PopupOpen = true;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        /////////////////////////////////////////////
        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Opened_Handler  -- Event: WorkOrder_Opened_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Opened_Handler(Boolean opentype)
        {
            RideTest = null;
            if (InfrastructureModule.Current_Work_Order == null) return;
            if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Work_Order_ID) == true) return;
            var rt = SART_Ride_Web_Service_Client_REST.Select_SART_Ride_Test_WORK_ORDER_Curr(Token, InfrastructureModule.Current_Work_Order.Work_Order_ID);
            if (rt != null)
            {
                RideTest = rt;
                eventAggregator.GetEvent<WorkOrder_RideTest_Event>().Publish(Ride_Test_Success);
            }
            if (String.IsNullOrEmpty(RideTest.Work_Order) == true) RideTest.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
            if (String.IsNullOrEmpty(RideTest.PT_Serial) == true) RideTest.PT_Serial = InfrastructureModule.Current_Work_Order.PT_Serial;
            Display_All();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        /// <summary></summary>
        /// <param name="navigationContext"></param>
        /// <returns></returns>
        public bool IsNavigationTarget(NavigationContext navigationContext) { return true; }

        /// <summary></summary>
        /// <param name="navigationContext"></param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            eventAggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Ride Test", Ride_Test_Control.Control_Name));
        }

        /// <summary></summary>
        /// <param name="navigationContext"></param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Selection_Event>().Publish("Ride Test");
            Display_All();
            RequestToCloseCommand.RaiseCanExecuteChanged();
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private void Display_All()
        {
            OnPropertyChanged("PowerOn_Checked");
            OnPropertyChanged("BalanceOn_Checked");
            OnPropertyChanged("Stationary_Checked");
            OnPropertyChanged("AxisRight_Checked");
            OnPropertyChanged("AxisLeft_Checked");
            OnPropertyChanged("Accelerate_Checked");
            OnPropertyChanged("Decelerate_Checked");
            OnPropertyChanged("BalanceOff_Checked");
            OnPropertyChanged("PowerOff_Checked");
            OnPropertyChanged("Work_Order_Num");
            OnPropertyChanged("PTSerial");
            OnPropertyChanged("WorkOrderColor");
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
