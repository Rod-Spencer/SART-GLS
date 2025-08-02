using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Segway.Database.Objects;
using Segway.Login.Objects;
using Segway.Manufacturing.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.Modules.WorkOrder.Services;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.Controls.StatusBars;
using Segway.Service.Disclaimer;
using Segway.Service.ExceptionHelper;
using Segway.Service.Helper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.Modules.AddWindow;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using Segway.Syteline.Client.REST;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Segway.Modules.WorkOrder
{
    public class Work_Order_Update_ViewModel : ViewModelBase, Work_Order_Update_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        public Work_Order_Update_ViewModel(Work_Order_Update_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<Application_Logout_Event>().Subscribe(Application_Logout_Handler, ThreadOption.BackgroundThread, true);

            #endregion

            #region Command Setups

            UpdateWorkOrderCommand = new DelegateCommand(CommandUpdateWorkOrder, CanCommandUpdateWorkOrder);
            CancelUpdateWorkOrderCommand = new DelegateCommand(CommandCancelUpdateWorkOrder, CanCommandCancelUpdateWorkOrder);

            #endregion

        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        private const String ComplaintDefault = "";


        #region Flight_Code_Model

        private Dictionary<String, String> _Flight_Code_Model;

        /// <summary>Property Flight_Code_Model of type Dictionary<String,String></summary>
        public Dictionary<String, String> Flight_Code_Model
        {
            get
            {
                if ((_Flight_Code_Model == null) || (_Flight_Code_Model.Count == 0))
                {
                    try
                    {
                        _Flight_Code_Model = Syteline_Items_Web_Service_Client_REST.Get_Gen2_Flight_Code_Models(InfrastructureModule.Token);
                        if (_Flight_Code_Model == null) _Flight_Code_Model = new Dictionary<String, String>();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(Exception_Helper.FormatExceptionString(ex));
                        _Flight_Code_Model = new Dictionary<String, String>(); ;
                    }
                }
                return _Flight_Code_Model;
            }
            set
            {
                _Flight_Code_Model = value;
                OnPropertyChanged("Flight_Code_Model");
            }
        }

        #endregion

        #region InfoKey_Codes

        private Dictionary<String, InfoKey_Error_Codes> _InfoKey_Codes;

        /// <summary>Property InfoKey_Codes of type Dictionary<String, InfoKey_Error_Codes></summary>
        public Dictionary<String, InfoKey_Error_Codes> InfoKey_Codes
        {
            get
            {
                if (_InfoKey_Codes == null)
                {
                    if (InfrastructureModule.Token != null)
                    {
                        try
                        {
                            List<InfoKey_Error_Codes> codes = SART_IKErrCod_Web_Service_Client_REST.Select_InfoKey_Error_Codes_Criteria(InfrastructureModule.Token, null);
                            if (codes != null)
                            {
                                codes.Sort(new InfoKey_Error_Codes_Comparer());
                                _InfoKey_Codes = new Dictionary<String, InfoKey_Error_Codes>();
                                foreach (InfoKey_Error_Codes code in codes)
                                {
                                    _InfoKey_Codes.Add(code.Code, code);
                                }
                                InfoKey_Error_Codes = new ObservableCollection<String>(_InfoKey_Codes.Keys);
                            }
                        }
                        catch (AuthenticationNull_Exception ane)
                        {
                            logger.Warn(Exception_Helper.FormatExceptionString(ane));
                            throw;
                        }
                        catch (Authentication_Exception ae)
                        {
                            logger.Warn(Exception_Helper.FormatExceptionString(ae));
                            eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(Exception_Helper.FormatExceptionString(ex));
                            throw;
                        }
                    }
                }
                return _InfoKey_Codes;
            }
            set { _InfoKey_Codes = value; }
        }

        #endregion


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

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        ////////////////////////////////////////////////
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
        ////////////////////////////////////////////////

        #region Header_Image

        private BitmapImage _Header_Image;

        /// <summary>Property Header_Image of type BitmapImage</summary>
        public BitmapImage Header_Image
        {
            get
            {
                if (_Header_Image == null)
                {
                    _Header_Image = Image_Helper.ImageFromEmbedded(".Images.new.png");
                }
                return _Header_Image;
            }
            set
            {
                OnPropertyChanged("Header_Image");
            }
        }

        #endregion

        ////////////////////////////////////////////////
        #region Work_Order_Info Controls

        #region PT_Serial_Number

        /// <summary>Property PT_Serial_Number of type String</summary>
        public String PT_Serial_Number
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.PT_Serial;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;
                InfrastructureModule.Current_Work_Order.PT_Serial = value;
                OnPropertyChanged("PT_Serial_Number");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Work_Order_Number

        //private String _Work_Order_Number;

        /// <summary>Property Work_Order_Number of type String</summary>
        public String Work_Order_Number
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Work_Order_ID;
            }
            set
            {
                OnPropertyChanged("Work_Order_Number");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
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
        ////////////////////////////////////////////////

        ////////////////////////////////////////////////
        #region Model Controls

        #region Is_I2

        /// <summary>Property Is_I2 of type Boolean</summary>
        public Boolean Is_I2
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return false;
                return InfrastructureModule.Current_Work_Order.PT_Model == "i2";
            }
            set
            {
                if (value == true)
                {
                    if (InfrastructureModule.Current_Work_Order == null) return;

                    InfrastructureModule.Current_Work_Order.PT_Model = "";
                    String pn = Strings.AlphaDigits(InfrastructureModule.Current_Work_Order.PT_Part_Number);
                    if (Flight_Code_Model.ContainsKey(pn) == false)
                    {
                        logger.Debug("Searching Stage1_Partnum for PT: {0}", PT_Serial_Number);
                        List<Stage1_Partnum> partList = Manufacturing_S1PN_Web_Service_Client_REST.Select_Stage1_Partnum_UNIT_ID_SERIAL_NUMBER(InfrastructureModule.Token, PT_Serial_Number);
                        if ((partList == null) || (partList.Count == 0))
                        {
                            logger.Warn("Could not find a record entry in Stage1_Partnum for PT: {0}", PT_Serial_Number);
                            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Could not find model type for PT: {0}", PT_Serial_Number));
                        }
                        else
                        {
                            Stage1_Partnum stage1rec = partList[partList.Count - 1];
                            logger.Debug("Found Stage1_Partnum record(s) for PT: {0} ({1})", PT_Serial_Number, stage1rec.Unit_ID_Partnumber);
                            if (stage1rec.Unit_ID_Partnumber.ToUpper() != "I2")
                            {
                                logger.Warn("Model type: I2 did not match Segway database (Stage1_Partnum) for PT: {0} ({1})", PT_Serial_Number, stage1rec.Unit_ID_Partnumber);
                                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Model type: I2 did not match Segway database");
                            }
                            else
                            {
                                InfrastructureModule.Current_Work_Order.PT_Model = "i2";
                                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Ready");
                            }
                        }
                    }
                    else
                    {
                        String model = Flight_Code_Model[pn].ToUpper();
                        if (model != "I2")
                        {
                            SART_Work_Order wo = InfrastructureModule.Current_Work_Order;
                            logger.Warn("Model type: I2 did not match Segway flight code model for PT: {0} ({1})", wo.PT_Serial, model);
                            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Model type: I2 did not match Segway database");
                        }
                        else
                        {
                            InfrastructureModule.Current_Work_Order.PT_Model = "i2";
                            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Ready");
                        }
                    }
                }
                OnPropertyChanged("Is_I2");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Is_X2

        /// <summary>Property Is_X2 of type Boolean</summary>
        public Boolean Is_X2
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return false;
                return InfrastructureModule.Current_Work_Order.PT_Model == "x2";
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;

                if (value == true)
                {
                    InfrastructureModule.Current_Work_Order.PT_Model = "";
                    String pn = Strings.AlphaDigits(InfrastructureModule.Current_Work_Order.PT_Part_Number);
                    if (Flight_Code_Model.ContainsKey(pn) == false)
                    {
                        logger.Debug("Searching Stage1_Partnum for PT: {0}", PT_Serial_Number);
                        List<Stage1_Partnum> partList = Manufacturing_S1PN_Web_Service_Client_REST.Select_Stage1_Partnum_UNIT_ID_SERIAL_NUMBER(InfrastructureModule.Token, PT_Serial_Number);
                        if ((partList == null) || (partList.Count == 0))
                        {
                            logger.Warn("Could not find a record entry in Stage1_Partnum for PT: {0}", PT_Serial_Number);
                            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish(String.Format("Could not find model type for PT: {0}", PT_Serial_Number));
                        }
                        else
                        {
                            Stage1_Partnum stage1rec = partList[partList.Count - 1];
                            logger.Debug("Found Stage1_Partnum record(s) for PT: {0} ({1})", PT_Serial_Number, stage1rec.Unit_ID_Partnumber);
                            if (stage1rec.Unit_ID_Partnumber.ToUpper() != "X2")
                            {
                                logger.Warn("Model type: X2 did not match Segway database (Stage1_Partnum) for PT: {0} ({1})", PT_Serial_Number, stage1rec.Unit_ID_Partnumber);
                                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Model type: X2 did not match Segway database");
                            }
                            else
                            {
                                InfrastructureModule.Current_Work_Order.PT_Model = "x2";
                                eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Ready");
                            }
                        }
                    }
                    else
                    {
                        String model = Flight_Code_Model[pn].ToUpper();
                        if (model != "X2")
                        {
                            SART_Work_Order wo = InfrastructureModule.Current_Work_Order;
                            logger.Warn("Model type: X2 did not match Segway flight code model for PT: {0} ({1})", wo.PT_Serial, model);
                            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Model type: X2 did not match Segway database");
                        }
                        else
                        {
                            InfrastructureModule.Current_Work_Order.PT_Model = "x2";
                            eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Ready");
                        }
                    }
                }
                OnPropertyChanged("Is_X2");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////

        ////////////////////////////////////////////////
        #region Error Code Controls

        #region PTCannotStart

        /// <summary>Property PTCannotStart of type Boolean</summary>
        public Boolean PTCannotStart
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return false;
                if (InfrastructureModule.Current_Work_Order.Error_Code_NO_Start.HasValue == false) return false;
                return InfrastructureModule.Current_Work_Order.Error_Code_NO_Start.Value;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;

                InfrastructureModule.Current_Work_Order.Error_Code_NO_Start = value;
                OnPropertyChanged("PTCannotStart");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
                if (value == true) Selected_Code = null;
            }
        }

        #endregion


        #region HasErrorCode

        private Boolean _HasErrorCode;

        /// <summary>Property HasErrorCode of type Boolean</summary>
        public Boolean HasErrorCode
        {
            get { return _HasErrorCode; }
            set
            {
                _HasErrorCode = value;
                OnPropertyChanged("HasErrorCode");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion


        #region HasNoErrorCode

        /// <summary>Property HasNoErrorCode of type Boolean</summary>
        public Boolean HasNoErrorCode
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return false;
                if (InfrastructureModule.Current_Work_Order.Error_Code_None.HasValue == false) return false;
                return InfrastructureModule.Current_Work_Order.Error_Code_None.Value;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;

                InfrastructureModule.Current_Work_Order.Error_Code_None = value;
                OnPropertyChanged("HasNoErrorCode");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
                if (value == true) Selected_Code = null;
            }
        }

        #endregion


        #region InfoKey_Error_Codes

        private ObservableCollection<String> _InfoKey_Error_Codes;

        /// <summary>Property InfoKey_Error_Codes of type List<String></summary>
        public ObservableCollection<String> InfoKey_Error_Codes
        {
            get
            {
                if ((_InfoKey_Error_Codes == null) || (_InfoKey_Error_Codes.Count == 0))
                {
                    if (InfrastructureModule.Token != null)
                    {
                        try
                        {
                            List<InfoKey_Error_Codes> codes = SART_IKErrCod_Web_Service_Client_REST.Select_InfoKey_Error_Codes_Criteria(InfrastructureModule.Token, null);
                            if (codes != null)
                            {
                                codes.Sort(new InfoKey_Error_Codes_Comparer());
                                InfoKey_Codes = new Dictionary<String, InfoKey_Error_Codes>();
                                foreach (InfoKey_Error_Codes code in codes)
                                {
                                    InfoKey_Codes[code.Code] = code;
                                }
                                _InfoKey_Error_Codes = new ObservableCollection<String>(InfoKey_Codes.Keys);
                            }
                        }
                        catch (AuthenticationNull_Exception ane)
                        {
                            logger.Warn(Exception_Helper.FormatExceptionString(ane));
                            throw;
                        }
                        catch (Authentication_Exception ae)
                        {
                            logger.Warn(Exception_Helper.FormatExceptionString(ae));
                            eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(Exception_Helper.FormatExceptionString(ex));
                            throw;
                        }
                    }
                }
                return _InfoKey_Error_Codes;
            }
            set
            {
                _InfoKey_Error_Codes = value;
                OnPropertyChanged("InfoKey_Error_Codes");
            }
        }

        #endregion


        #region Selected_Code

        /// <summary>Property Selected_Code of type String</summary>
        public String Selected_Code
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Error_Code;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;

                InfrastructureModule.Current_Work_Order.Error_Code = value;
                if (String.IsNullOrWhiteSpace(InfrastructureModule.Current_Work_Order.Error_Code)) Error_Code_Description = "";
                else if (InfoKey_Codes.ContainsKey(InfrastructureModule.Current_Work_Order.Error_Code) == false) Error_Code_Description = "";
                else Error_Code_Description = InfoKey_Codes[InfrastructureModule.Current_Work_Order.Error_Code].Display;
                OnPropertyChanged("Selected_Code");
                OnPropertyChanged("HasErrorCode");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion


        #region Error_Code_Description

        private String _Error_Code_Description; // = "Error code description goes here";

        /// <summary>Property Error_Code_Description of type String</summary>
        public String Error_Code_Description
        {
            get { return _Error_Code_Description; }
            set
            {
                _Error_Code_Description = value;
                OnPropertyChanged("Error_Code_Description");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////

        #region Customer_Complaint

        /// <summary>Property Customer_Complaint of type String</summary>
        public String Customer_Complaint
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Customer_Complaint;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;
                InfrastructureModule.Current_Work_Order.Customer_Complaint = value;
                OnPropertyChanged("Customer_Complaint");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Unit_Condition

        private const String ConditionDefault = "";

        /// <summary>Property Unit_Condition of type String</summary>
        public String Unit_Condition
        {
            get
            {
                if (InfrastructureModule.Current_Work_Order == null) return null;
                return InfrastructureModule.Current_Work_Order.Unit_Condition;
            }
            set
            {
                if (InfrastructureModule.Current_Work_Order == null) return;
                InfrastructureModule.Current_Work_Order.Unit_Condition = value;
                OnPropertyChanged("Unit_Condition");
                UpdateWorkOrderCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region UpdateWorkOrderCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: UpdateWorkOrderCommand</summary>
        public DelegateCommand UpdateWorkOrderCommand { get; set; }
        private Boolean CanCommandUpdateWorkOrder()
        {
            if (String.IsNullOrEmpty(Customer_Complaint) == true) return false;
            if (String.IsNullOrEmpty(Unit_Condition) == true) return false;
            if ((PTCannotStart ^ HasErrorCode ^ HasNoErrorCode) == false) return false;
            if ((Is_I2 ^ Is_X2) == false) return false;
            if ((HasErrorCode == true) && (String.IsNullOrEmpty(Selected_Code) == true)) return false;
            return true;
        }

        private void CommandUpdateWorkOrder()
        {
            eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
            //Application_Helper.DoEvents();

            try
            {
                logger.Trace("Entered");
                if (InfrastructureModule.Current_Work_Order == null)
                {
                    String m = "InfrastructureModule.Current_Work_Order is NULL";
                    logger.Error(m);
                    throw new Exception(m);
                }

                InfrastructureModule.Current_Work_Order.Date_Time_Updated = DateTime.Now;
                if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Technician_Name) == true)
                {
                    InfrastructureModule.Current_Work_Order.Technician_Name = LoginContext.UserName;
                }

                SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                criteria.Add(new FieldData("Work_Order_ID", InfrastructureModule.Current_Work_Order.Work_Order_ID));
                criteria.Add(new FieldData("User_Name", LoginContext.UserName));
                criteria.Add(new FieldData("Status", (int)Disclaimer_Statuses.Accepted));
                List<SART_Disclaimer> sdList = SART_DISCLAIMER_Web_Service_Client_REST.Select_SART_Disclaimer_Criteria(InfrastructureModule.Token, criteria);
                if ((sdList == null) || (sdList.Count == 0))
                {
                    if ((LoginContext.User_Level >= UserLevels.Expert) && (InfrastructureModule.User_Settings != null) && (InfrastructureModule.User_Settings.Disclaimer == Disclaimer_Statuses.Accepted))
                    {
                        SART_Disclaimer sd = new SART_Disclaimer(LoginContext.UserName, InfrastructureModule.Current_Work_Order.Work_Order_ID);
                        if (SART_DISCLAIMER_Web_Service_Client_REST.Insert_SART_Disclaimer_Key(InfrastructureModule.Token, sd) != null)
                        {
                            logger.Debug("Assigned accepted disclaimer - user: {0}, work order: {1}", LoginContext.UserName, InfrastructureModule.Current_Work_Order.Work_Order_ID);
                            // Open work order and nNavigate to Work Order Summary
                            Work_Order_Events.Open_WorkOrder(true);
                            return;
                        }
                    }

                    Work_Order_Events.Open_WorkOrder(false);

                    eventAggregator.GetEvent<SART_Disclaimer_Accept_Navigate_Event>().Publish(Work_Order_Summary_Control.Control_Name);
                    eventAggregator.GetEvent<SART_Disclaimer_Reject_Navigate_Event>().Publish(Work_Order_Open_Control.Control_Name);
                    regionManager.RequestNavigate(RegionNames.MainRegion, Disclaimer_Control.Control_Name);
                    return;
                }
                logger.Debug("Found accepted disclaimer: {0}", criteria);
                Work_Order_Events.Open_WorkOrder(true);
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                //Application_Helper.DoEvents();
                logger.Trace("Leaving");
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region CancelUpdateWorkOrderCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: CancelUpdateWorkOrderCommand</summary>
        public DelegateCommand CancelUpdateWorkOrderCommand { get; set; }
        private Boolean CanCommandCancelUpdateWorkOrder() { return true; }
        private void CommandCancelUpdateWorkOrder()
        {
            Work_Order_Events.Cancel_Current_Work_Order(eventAggregator, regionManager, true);
            regionManager.RequestNavigate(RegionNames.MainRegion, Work_Order_Open_Control.Control_Name);
        }

        /////////////////////////////////////////////
        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Close_Handler  -- Event: SART_WorkOrder_Close_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Close_Handler(Boolean close)
        {
            Reset();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SART_WorkOrder_Cancel_Handler  -- Event: SART_WorkOrder_Cancel_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SART_WorkOrder_Cancel_Handler(Boolean cancel)
        {
            Reset();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Application_Logout_Handler  -- Event: Application_Logout_Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Application_Logout_Handler(Boolean logout)
        {
            if (Token != null)
            {
                Token.LoginContext = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region IsNavigationAware Handlers

        public bool IsNavigationTarget(NavigationContext navigationContext) { return true; }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(false);
            eventAggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Work Order", Work_Order_Update_Control.Control_Name));
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Enabled_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Selection_Event>().Publish("Work Order");

            OnPropertyChanged("Work_Order_Number");
            OnPropertyChanged("PT_Serial_Number");
            OnPropertyChanged("WorkOrderColor");
            OnPropertyChanged("InfoKey_Error_Codes");

            if (InfrastructureModule.Current_Work_Order == null)
            {
                logger.Error("The Work Order has not been assigned to InfrastructureModule.Current_Work_Order");
                regionManager.RequestNavigate(RegionNames.MainRegion, Work_Order_Open_Control.Control_Name);
                return;
            }


            ////////////////////////////////////////////////////////////////////////////////
            // Checking for Model
            try
            {
                logger.Debug("Checking for Model type");
                if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.PT_Model) == false)
                {
                    String ptModel = InfrastructureModule.Current_Work_Order.PT_Model;
                    if ((ptModel.StartsWith("G2_") == true) || (ptModel.StartsWith("SE_") == true))
                    {
                        ptModel = ptModel.Substring(3, 2);
                    }
                    Is_I2 = ptModel.ToUpper() == "I2" ? true : false;
                    Is_X2 = ptModel.ToUpper() == "X2" ? true : false;
                }
                else if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.PT_Part_Number) == false)
                {
                    String pn = InfrastructureModule.Current_Work_Order.PT_Part_Number.Replace("-", "");
                    if (Flight_Code_Model.ContainsKey(pn) == true)
                    {
                        InfrastructureModule.Current_Work_Order.PT_Model = Flight_Code_Model[pn];
                    }
                    else
                    {
                        String msg = String.Format("There is no flight code model information available for part number: {0}.", pn);
                        String msg1 = String.Format("{0}  Please CANCEL out of this work order and contact Segway Technical Support to have this information entered.  It is found under the \"User Defined\" tab of the Item form in Syteline.", msg);
                        Message_Window.Error(msg1, height: Window_Sizes.MediumSmall).ShowDialog();
                        logger.Warn(msg);
                        //return;
                    }
                }
                else
                {
                    String msg = String.Format("There is no PT Model or PT Part Number associated to work order: {0} (PT: {1})", InfrastructureModule.Current_Work_Order.PT_Serial, InfrastructureModule.Current_Work_Order.PT_Part_Number);
                    String msg1 = String.Format("{0}  Please CANCEL out of this work order and have this information entered before trying to continue.  ", msg);
                    Message_Window.Error(msg1, height: Window_Sizes.MediumSmall).ShowDialog();
                    logger.Warn(msg);
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                String msg = Exception_Helper.FormatExceptionString("An exception has occurred.  Please upload your log (<Right-Shft>-<F11>) and contact Segway Technical Support for further assistance.", ex);
                Message_Window.Error(msg).ShowDialog();
                logger.Error(msg);
            }
            finally
            {
                logger.Debug("Leaving");
            }
            // Checking for Model
            ////////////////////////////////////////////////////////////////////////////////


            Boolean IsWarranty = Convert.ToBoolean(InfrastructureModule.Current_Work_Order.Warranty);
            Customer_Complaint = InfrastructureModule.Current_Work_Order.Customer_Complaint;
            Unit_Condition = InfrastructureModule.Current_Work_Order.Unit_Condition;



            ////////////////////////////////////////////////////////////////////////////////
            // Checking for Error Code
            PTCannotStart = false;
            HasNoErrorCode = false;
            HasErrorCode = false;
            if (String.IsNullOrEmpty(InfrastructureModule.Current_Work_Order.Error_Code) == false)
            {
                if (InfoKey_Error_Codes.Contains(InfrastructureModule.Current_Work_Order.Error_Code))
                {
                    Selected_Code = InfrastructureModule.Current_Work_Order.Error_Code;
                }
                HasErrorCode = true;
            }
            //HasNoErrorCode = InfrastructureModule.Current_Work_Order.Error_Code_None;
            //PTCannotStart = InfrastructureModule.Current_Work_Order.Error_Code_NO_Start;

            if (InfrastructureModule.Current_Work_Order.Error_Code_None.HasValue)
            {
                if (InfrastructureModule.Current_Work_Order.Error_Code_None.Value == true)
                {
                    HasNoErrorCode = true;
                }
            }
            if (InfrastructureModule.Current_Work_Order.Error_Code_NO_Start.HasValue == true)
            {
                if (InfrastructureModule.Current_Work_Order.Error_Code_NO_Start.Value == true)
                {
                    PTCannotStart = true;
                }
            }


            // Checking for Error Code
            ////////////////////////////////////////////////////////////////////////////////

            if (CanCommandUpdateWorkOrder() == true)
            {
                Work_Order_Events.Open_WorkOrder(true);
                //regionManager.RequestNavigate(RegionNames.MainRegion, Work_Order_Summary_Control.Control_Name);
            }

        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private void Reset()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                _HasErrorCode = false;
                _Error_Code_Description = null;
                OnPropertyChanged("HasErrorCode");
                OnPropertyChanged("Error_Code_Description");
                OnPropertyChanged("PT_Serial_Number");
                OnPropertyChanged("Work_Order_Number");
                OnPropertyChanged("WorkOrderColor");
                OnPropertyChanged("Is_I2");
                OnPropertyChanged("Is_X2");
                OnPropertyChanged("PTCannotStart");
                OnPropertyChanged("HasNoErrorCode");
                OnPropertyChanged("Selected_Code");
                OnPropertyChanged("Customer_Complaint");
                OnPropertyChanged("Unit_Condition");
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }
        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
