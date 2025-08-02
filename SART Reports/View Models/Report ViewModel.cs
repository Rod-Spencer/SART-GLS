using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using PdfSharp.Drawing;
using Segway.Database.Objects;
using Segway.Login.Objects;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Modules.SART_Infrastructure;
using Segway.Modules.ShellControls;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.CAN;
using Segway.Service.Common;
using Segway.Service.ExceptionHelper;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.PDFHelper;
using Segway.Syteline.Client.REST;
using Segway.Syteline.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


namespace Segway.SART.Reports
{
    /// <summary>Public Class - Report_ViewModel</summary>
    public class Report_ViewModel : ViewModelBase, Report_ViewModel_Interface, INavigationAware
    {
        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;
        private IEventAggregator eventAggregator;


        /// <summary>Contructor</summary>
        /// <param name="view">Report_Control_Interface</param>
        /// <param name="regionManager">IRegionManager</param>
        /// <param name="container">IUnityContainer</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        public Report_ViewModel(Report_Control_Interface view, IRegionManager regionManager, IUnityContainer container, IEventAggregator eventAggregator)
            : base(view)
        {
            this.regionManager = regionManager;
            this.container = container;
            this.eventAggregator = eventAggregator;


            #region Event Subscriptions

            eventAggregator.GetEvent<SART_WorkOrder_Selected_Event>().Subscribe(WorkOrder_Selected_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Close_Event>().Subscribe(SART_WorkOrder_Close_Handler, ThreadOption.UIThread, true);
            eventAggregator.GetEvent<SART_WorkOrder_Cancel_Event>().Subscribe(SART_WorkOrder_Cancel_Handler, ThreadOption.UIThread, true);

            #endregion

            #region Command Setups

            WorkOrderDetailedCommand = new DelegateCommand(CommandWorkOrderDetailed, CanCommandWorkOrderDetailed);
            WorkOrderSummaryCommand = new DelegateCommand(CommandWorkOrderSummary, CanCommandWorkOrderSummary);

            #endregion
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Properties

        #region Work_Order

        private SART_Work_Order _Work_Order;

        /// <summary>Property Work_Order of type SART_Work_Order</summary>
        public SART_Work_Order Work_Order
        {
            get { return _Work_Order; }
            set { _Work_Order = value; }
        }

        #endregion

        /// <summary>Public Member</summary>
        public String Header1 = "Segway Service";
        /// <summary>Public Property - Header2</summary>
        public String Header2 { get; set; }
        /// <summary>Public Property - Header3</summary>
        public String Header3 { get; set; }


        #region CUA_Log

        private SART_CU_Logs _CUA_Log;

        /// <summary>Property CUA_Log of type SART_CU_Logs</summary>
        public SART_CU_Logs CUA_Log
        {
            get
            {
                if (_CUA_Log == null)
                {
                    SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                    criteria.Add(new FieldData("Work_Order", Work_Order.Work_Order_ID));
                    criteria.Add(new FieldData("Side", CAN_CU_Sides.A.ToString()[0]));

                    List<SART_CU_Logs> logs = SART_Log_Web_Service_Client_REST.Select_SART_CU_Logs_Criteria(InfrastructureModule.Token, criteria);
                    if (logs != null)
                    {
                        if (logs.Count == 0)
                        {
                            _CUA_Log = null;
                        }
                        else if (logs.Count == 1)
                        {
                            _CUA_Log = logs[0];
                        }
                        else if (logs.Count > 1)
                        {
                            logs = logs.OrderBy(x => x.Date_Time_Extracted).ThenBy(x => x.ID).ToList();
                            _CUA_Log = logs[0];
                        }
                    }
                }
                return _CUA_Log;
            }
            set { _CUA_Log = value; }
        }

        #endregion


        #region CUB_Log

        private SART_CU_Logs _CUB_Log;

        /// <summary>Property CUB_Log of type SART_CU_Logs</summary>
        public SART_CU_Logs CUB_Log
        {
            get
            {
                if (_CUB_Log == null)
                {
                    SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                    criteria.Add(new FieldData("Work_Order", Work_Order.Work_Order_ID));
                    criteria.Add(new FieldData("Side", CAN_CU_Sides.B.ToString()[0]));

                    List<SART_CU_Logs> logs = SART_Log_Web_Service_Client_REST.Select_SART_CU_Logs_Criteria(InfrastructureModule.Token, criteria);
                    if (logs != null)
                    {
                        if (logs.Count == 0)
                        {
                            _CUB_Log = null;
                        }
                        else if (logs.Count == 1)
                        {
                            _CUB_Log = logs[0];
                        }
                        else if (logs.Count > 1)
                        {
                            logs = logs.OrderBy(x => x.Date_Time_Extracted).ThenBy(x => x.ID).ToList();
                            _CUB_Log = logs[0];
                        }
                    }
                }
                return _CUB_Log;
            }
            set
            {
                _CUB_Log = value;
                OnPropertyChanged("CUB_Log");
            }
        }

        #endregion


        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Control Properties

        #region Work_Order_ID

        private String _Work_Order_ID;

        /// <summary>Property Work_Order_ID of type String</summary>
        public String Work_Order_ID
        {
            get { return _Work_Order_ID; }
            set
            {
                _Work_Order_ID = value;
                WorkOrderDetailedCommand.RaiseCanExecuteChanged();
                WorkOrderSummaryCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("Work_Order_ID");
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

        private System.Windows.Media.Brush _PopupColor;

        /// <summary>ViewModel Property: PopupColor of type: Brush</summary>
        public System.Windows.Media.Brush PopupColor
        {
            get { return _PopupColor; }
            set
            {
                _PopupColor = value;
                OnPropertyChanged("PopupColor");
            }
        }

        #endregion

        #region PopupFontSize

        private Double _PopupFontSize = 36.0;

        /// <summary>Property PopupFontSize of type Double</summary>
        public Double PopupFontSize
        {
            get { return _PopupFontSize; }
            set
            {
                _PopupFontSize = value;
                OnPropertyChanged("PopupFontSize");
            }
        }

        #endregion

        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Command Handlers

        #region WorkOrderDetailedCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: WorkOrderDetailedCommand</summary>
        public DelegateCommand WorkOrderDetailedCommand { get; set; }
        private Boolean CanCommandWorkOrderDetailed() { return String.IsNullOrEmpty(Work_Order_ID) == false; }
        private void CommandWorkOrderDetailed()
        {
            try
            {
                logger.Trace("Entered");
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                //Application_Helper.DoEvents();

                if ((Work_Order = Get_Work_Order()) == null) return;
                CUA_Log = null;
                CUB_Log = null;
                PDF_Helper pdf = Generate_WorkOrder_Detailed();
                if (pdf != null)
                {
                    String path = Path.Combine(Application_Helper.Application_Folder_Name(), "Reports", String.Format("{0}.pdf", Work_Order.Work_Order_ID));
                    FileInfo fi = new FileInfo(path);
                    if (fi.Exists) fi.Delete();
                    if (fi.Directory.Exists == false) fi.Directory.Create();
                    pdf.Save(fi.FullName);
                    ProcessHelper.Run(fi.FullName, null, 0, true, false, false, false);
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = System.Windows.Media.Brushes.Pink;
                PopupMessage = msg;
                PopupFontSize = 16;
                PopupOpen = true;
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Trace("Leaving");
            }
        }

        /////////////////////////////////////////////
        #endregion


        #region WorkOrderSummaryCommand
        /////////////////////////////////////////////

        /// <summary>Delegate Command: WorkOrderSummaryCommand</summary>
        public DelegateCommand WorkOrderSummaryCommand { get; set; }
        private Boolean CanCommandWorkOrderSummary() { return String.IsNullOrEmpty(Work_Order_ID) == false; }
        private void CommandWorkOrderSummary()
        {
            try
            {
                logger.Trace("Entered");
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(true);
                //Application_Helper.DoEvents();

                if ((Work_Order = Get_Work_Order()) == null) return;
                CUA_Log = null;
                CUB_Log = null;
                PDF_Helper pdf = Generate_WorkOrder_Summary();
                if (pdf != null)
                {
                    String path = Path.Combine(Application_Helper.Application_Folder_Name(), "Reports", String.Format("{0}.pdf", Work_Order.Work_Order_ID));
                    FileInfo fi = new FileInfo(path);
                    if (fi.Exists) fi.Delete();
                    if (fi.Directory.Exists == false) fi.Directory.Create();
                    pdf.Save(fi.FullName);
                    ProcessHelper.Run(fi.FullName, null, 0, true, false, false, false);
                }
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
            }
            catch (Exception ex)
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                String msg = Exception_Helper.FormatExceptionString(ex);
                logger.Error(msg);
                PopupColor = System.Windows.Media.Brushes.Pink;
                PopupMessage = msg;
                PopupFontSize = 16;
                PopupOpen = true;
            }
            finally
            {
                eventAggregator.GetEvent<Shell_MouseCursor_Busy_Event>().Publish(false);
                logger.Trace("Leaving");
            }
        }

        /////////////////////////////////////////////
        #endregion

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handlers

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region WorkOrder_Selected_Handler  -- SART_WorkOrder_Selected_Event Event Handler
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WorkOrder_Selected_Handler(String WorkOrderID)
        {
            Work_Order_ID = WorkOrderID;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


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
            eventAggregator.GetEvent<ToolBar_Change_Navigation_Event>().Publish(new KeyValuePair<String, String>("Reports", Report_Control.Control_Name));
        }

        /// <summary>Public Method - OnNavigatedTo</summary>
        /// <param name="navigationContext">NavigationContext</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            eventAggregator.GetEvent<ToolBar_Visibility_Event>().Publish(true);
            eventAggregator.GetEvent<ToolBar_Selection_Event>().Publish("Reports");
        }

        #endregion
        ////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Miscellaneous Methods

        private SART_Work_Order Get_Work_Order()
        {
            if (InfrastructureModule.Current_Work_Order != null) return InfrastructureModule.Current_Work_Order;
            SART_Work_Order wo = Syteline_WO_Web_Service_Client_REST.Select_SART_Work_Order_WORK_ORDER_ID(InfrastructureModule.Token, Work_Order_ID);
            if (wo == null)
            {
                //////////////////////////////
                // Add some code here
                //////////////////////////////
            }
            return wo;
        }

        private Double New_Page(PDF_Helper pdf)
        {
            pdf.AddStandardPage(Header1, Header2, null, headerHeight: 80, logoKey: "Logo", logoWidth: XUnit.FromInch(1));
            XRect r = new XRect(pdf.CurrentMargin.TopLeft, new XSize(pdf.CurrentMargin.Width, 80));

            String data = String.Format("{0} / {1}", Work_Order.Work_Order_ID, Work_Order.PT_Serial);

            pdf.WriteLine(data, r, paraformat: PDF_Helper.XPA_CenterBottom, font: new XFont(PDF_Helper.FontFamily_Tahoma, 30), force: true);
            pdf.GoToTopOfPage();
            return pdf.CurrentPoint.Y;
        }

        private PDF_Helper Generate_WorkOrder_Detailed()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Header2 = "Work Order Detailed Report";
                String data = String.Format("{0} / {1}", Work_Order.Work_Order_ID, Work_Order.PT_Serial);

                PDF_Helper pdf = new PDF_Helper();
                pdf.Set_Default_Font(PDF_Helper.FontFamily_Tahoma, 7);
                pdf.CurrentBarCodeFont = new XFont(PDF_Helper.FontFamily_CourierNew, 6);
                pdf.Watermark = "Internal Use Only";
                pdf.AddImage("Logo", Assembly.GetExecutingAssembly(), "Segway-White.png");
                //pdf.Header_Font_3 = new XFont(PDF_Helper.FontFamily_Tahoma, 30);
                pdf.CreateStandardDocument(Header1, Header2, null, headerHeight: 80, logoKey: "Logo", logoWidth: XUnit.FromInch(1));

                XRect r = new XRect(pdf.CurrentMargin.TopLeft, new XSize(pdf.CurrentMargin.Width, 80));
                pdf.WriteLine(data, r, paraformat: PDF_Helper.XPA_CenterBottom, font: new XFont(PDF_Helper.FontFamily_Tahoma, 30), force: true);

                pdf.SetTabs(new double[] { .3, 1.5, 4.25, 5.45, 8.2 });
                pdf.GoToTopOfPage(5);
                pdf.Set_Default_Height(30.0);
                pdf.AddNewPageDelegate = New_Page;


                Write_Work_Order_Header(pdf);
                Write_Incident_Info(pdf);
                Write_Service_Request_Info(pdf);
                Write_Other_WorkOrders(pdf);
                Write_Warranties(pdf);
                Write_Receiving_Info(pdf);
                Write_Picture_Info(pdf);
                Write_Battery_Info(pdf);
                Write_Log_Info(pdf);
                Write_Task_Summary_Info(pdf);
                Write_Repair_Info(pdf);

                pdf.SetTabs(new Double[] { .3, 8.2 });
                r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight + 5);
                pdf.WriteLine("***  End of Report ***", r, XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                //pdf.WriteLine("The remainder of this report is currently under construction", r, XBrushes.Red, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);

                return pdf;
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                return null;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private PDF_Helper Generate_WorkOrder_Summary()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Header2 = "Work Order Summary Report";
                String data = String.Format("{0} / {1}", Work_Order.Work_Order_ID, Work_Order.PT_Serial);

                PDF_Helper pdf = new PDF_Helper();
                pdf.Set_Default_Font(PDF_Helper.FontFamily_Tahoma, 7);
                pdf.CurrentBarCodeFont = new XFont(PDF_Helper.FontFamily_CourierNew, 6);
                pdf.AddImage("Logo", Assembly.GetExecutingAssembly(), "Segway-White.png");

                //pdf.Header_Font_3 = new XFont(PDF_Helper.FontFamily_Tahoma, 30);

                pdf.CreateStandardDocument(Header1, Header2, null, headerHeight: 80, logoKey: "Logo", logoWidth: XUnit.FromInch(1));
                pdf.SetTabs(new double[] { .3, 1.5, 4.25, 5.45, 8.2 });
                pdf.GoToTopOfPage(5);
                XRect r = new XRect(pdf.CurrentMargin.TopLeft, new XSize(pdf.CurrentMargin.Width, 80));
                pdf.WriteLine(data, r, paraformat: PDF_Helper.XPA_CenterBottom, font: new XFont(PDF_Helper.FontFamily_Tahoma, 30), force: true);
                pdf.Set_Default_Height(30.0);
                pdf.AddNewPageDelegate = New_Page;


                Write_Work_Order_Header(pdf);
                Write_Incident_Info(pdf);
                Write_Service_Request_Info(pdf);
                Write_Warranties(pdf);
                Write_Receiving_Info(pdf);
                Write_Battery_Info(pdf);
                Write_Repair_Info(pdf);

                pdf.SetTabs(new Double[] { .3, 8.2 });
                r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight + 5);
                pdf.WriteLine("***  End of Report ***", r, XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                //pdf.WriteLine("The remainder of this report is currently under construction", r, XBrushes.Red, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);

                return pdf;
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private void Write_Other_WorkOrders(PDF_Helper pdf)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                pdf.SetTabs(new Double[] { .3, 8.2 });
                pdf.Set_Default_Height(pdf.Header_Font_3_Bold.Height);
                XRect r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
                pdf.WriteLine(String.Format("Other Work Orders for Serial Number: {0}", Work_Order.PT_Serial), r, null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.GotoNextLine();

                pdf.CurrentFont = new XFont(pdf.CurrentFont.FontFamily.Name, 7);
                pdf.Set_Default_Height(pdf.CurrentFont.Height);

                pdf.SetTabs(new Double[] { .3, 1, 2, 3, 5, 8.2 });

                List<SART_Work_Order> orders = Syteline_WO_Web_Service_Client_REST.Select_SART_Work_Order_PTSERIAL(InfrastructureModule.Token, Work_Order.PT_Serial);
                if (orders == null)
                {
                    logger.Warn("No orders found");
                    return;
                }
                Boolean first = true;
                foreach (SART_Work_Order order in orders)
                {
                    if ((order.Work_Order_ID != Work_Order.Work_Order_ID) && (String.IsNullOrEmpty(order.Work_Order_ID) == false))
                    {
                        XPoint lower = default(XPoint);
                        if (first == true)
                        {
                            pdf.WriteLine("Work Order", pdf.GetTabRegion(0), null, PDF_Helper.XPA_CenterMiddle);
                            pdf.WriteLine("Date", pdf.GetTabRegion(1), null, PDF_Helper.XPA_CenterMiddle);
                            pdf.WriteLine("Technician", pdf.GetTabRegion(2), null, PDF_Helper.XPA_CenterMiddle);
                            pdf.WriteLine("Problem", pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
                            pdf.WriteLine("Summary", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle);
                            pdf.GotoNextLine();
                            first = false;
                        }
                        pdf.WriteLine(order.Work_Order_ID, pdf.GetTabRegion(0), null, PDF_Helper.XPA_CenterMiddle);
                        if (order.Date_Created.HasValue)
                        {
                            pdf.WriteLine(order.Date_Created.Value.ToShortDateString(), pdf.GetTabRegion(1), null, PDF_Helper.XPA_CenterMiddle);
                        }
                        pdf.WriteLine(order.Technician_Name, pdf.GetTabRegion(2), null, PDF_Helper.XPA_CenterMiddle);
                        pdf.WriteLine(order.Problem_Description, pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
                        if (String.IsNullOrEmpty(order.Problem_Description) == false)
                        {
                            r = pdf.GetTabRegion(3);
                            String DescString = order.Problem_Description;
                            r = pdf.Calculate_Region_Size(r, ref DescString, pdf.CurrentFont);
                            pdf.WriteLine(DescString, r, null, PDF_Helper.XPA_LeftMiddle);
                            lower = r.BottomLeft;
                        }

                        if (String.IsNullOrEmpty(order.Observation_Technician) == false)
                        {
                            r = pdf.GetTabRegion(4);
                            String DescString = order.Observation_Technician;
                            r = pdf.Calculate_Region_Size(r, ref DescString, pdf.CurrentFont);
                            pdf.WriteLine(DescString, r, null, PDF_Helper.XPA_LeftMiddle);
                            if (lower == default(XPoint)) lower = r.BottomLeft;
                            else if (lower.Y < r.Bottom) lower = r.BottomLeft;
                        }

                        if (lower == default(XPoint)) pdf.GotoNextLine();
                        else
                        {
                            lower.Offset(0, 1);
                            pdf.CurrentPoint = lower;
                        }
                    }
                }

                Write_Line(pdf);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void Write_Line(PDF_Helper pdf)
        {
            pdf.SetTabs(new Double[] { .3, 8.2 });

            XRect r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
            pdf.DrawLine(r.BottomLeft, r.BottomRight, new XPen(pdf.Color_Gray, .5));
            pdf.GotoNextLine(2);
        }

        private void Write_Incident_Info(PDF_Helper pdf)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                pdf.SetTabs(new Double[] { .3, 8.2 });
                pdf.Set_Default_Height(pdf.Header_Font_3_Bold.Height);
                XRect r = pdf.GetTabRegion(0);
                pdf.WriteLine("Information from Incident:", r, null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.GotoNextLine();

                pdf.CurrentFont = new XFont(pdf.CurrentFont.FontFamily.Name, 7);
                pdf.Set_Default_Height(pdf.CurrentFont.Height);
                pdf.SetTabs(new Double[] { .3, 1, 2, 2.5, 4, 4.5, 5, 8.2 });

                FS_Incident incident = Syteline_Incident_Web_Service_Client_REST.Select_FS_Incident_SRONUM(InfrastructureModule.Token, Work_Order.Work_Order_ID);
                if (incident != null)
                {
                    pdf.WriteLine("Description:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                    if (String.IsNullOrEmpty(incident.Description) == false)
                    {
                        pdf.WriteLine(incident.Description, pdf.GetTabRegion(1, 6), null, PDF_Helper.XPA_LeftMiddle);
                    }

                    pdf.GotoNextLine();

                    List<FS_Inc_Reason> reasons = Syteline_IncReas_Web_Service_Client_REST.Select_FS_Inc_Reason_SRO_NUM(InfrastructureModule.Token, Work_Order.Work_Order_ID);
                    if (reasons != null)
                    {
                        foreach (FS_Inc_Reason reason in reasons)
                        {
                            r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
                            pdf.WriteLine("Reason:", r, null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                            if ((String.IsNullOrEmpty(reason.Reason_Gen) == false) && (String.IsNullOrEmpty(reason.Reason_Spec) == false))
                            {
                                FS_Reas_Spec spec = Syteline_ReasSpec_Web_Service_Client_REST.Select_FS_Reas_Spec_GEN_SPEC(InfrastructureModule.Token, reason.Reason_Gen, reason.Reason_Spec);
                                FS_Reas_Gen gen = Syteline_ReasGen_Web_Service_Client_REST.Select_FS_Reas_Gen_REASGEN(InfrastructureModule.Token, reason.Reason_Gen);
                                pdf.WriteLine("General:", pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                                pdf.WriteLine(gen.ReasonGen, pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle);
                                pdf.WriteLine(gen.Description, pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
                                pdf.WriteLine("Specific:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                                pdf.WriteLine(spec.ReasonSpec, pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle);
                                pdf.WriteLine(spec.Description, pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle);
                            }
                            else if (String.IsNullOrEmpty(reason.Reason_Gen) == false)
                            {
                                FS_Reas_Gen gen = Syteline_ReasGen_Web_Service_Client_REST.Select_FS_Reas_Gen_REASGEN(InfrastructureModule.Token, reason.Reason_Gen);
                                pdf.WriteLine("General:", pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                                pdf.WriteLine(gen.ReasonGen, pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle);
                                pdf.WriteLine(gen.Description, pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
                            }
                            pdf.GotoNextLine();
                        }
                    }

                    //if (false)
                    //{
                    //    List<Note_Data> notes = SART_2012_Web_Service_Client.Get_Incident_Notes(incident.Inc_Num);
                    //    if (notes != null)
                    //    {
                    //        foreach (Note_Data note in notes)
                    //        {
                    //            r = pdf.GetTabRegion(0);
                    //            pdf.WriteLine("Inc Note:", r, null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));

                    //            r = pdf.GetTabRegion(1);
                    //            pdf.WriteLine(String.Format("{0} ({1})", note.Creation_Date, note.User_Name), r, null, PDF_Helper.XPA_LeftMiddle, pdf.CurrentFont);

                    //            r = pdf.GetTabRegion(2, 5);
                    //            String noteString = note.Note;
                    //            r = pdf.GetRegionSize(r, ref noteString, pdf.CurrentFont);
                    //            pdf.WriteLine(noteString, r, null, PDF_Helper.XPA_LeftMiddle, pdf.CurrentFont);
                    //            pdf.CurrentPoint = r.BottomLeft;
                    //        }
                    //    }
                    //}
                }

                Write_Line(pdf);
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
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void Write_Service_Request_Info(PDF_Helper pdf)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                pdf.SetTabs(new Double[] { .3, 8.2 });
                pdf.Set_Default_Height(pdf.Header_Font_3_Bold.Height);
                XRect r = pdf.GetTabRegion(0);
                pdf.WriteLine("Information from Service Request:", r, null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.GotoNextLine();

                pdf.CurrentFont = new XFont(pdf.CurrentFont.FontFamily.Name, 7);
                pdf.Set_Default_Height(pdf.CurrentFont.Height);
                pdf.SetTabs(new Double[] { .3, 1, 2, 2.5, 4, 4.5, 5, 8.2 });

                List<Note_Data> notes = Syteline_NoteData_Web_Service_Client_REST.Get_SRO_NoteData(InfrastructureModule.Token, Work_Order.Work_Order_ID);
                if (notes != null)
                {
                    foreach (Note_Data note in notes)
                    {
                        r = pdf.GetTabRegion(0);
                        pdf.WriteLine("SR Note:", r, null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));

                        r = pdf.GetTabRegion(1);
                        pdf.WriteLine(String.Format("{0} ({1})", note.Creation_Date, note.User_Name), r, null, PDF_Helper.XPA_LeftMiddle, pdf.CurrentFont);

                        r = pdf.GetTabRegion(2, 5);
                        String noteString = note.Note;
                        r = pdf.Calculate_Region_Size(r, ref noteString, pdf.CurrentFont);
                        pdf.WriteLine(noteString, r, null, PDF_Helper.XPA_LeftMiddle, pdf.CurrentFont);
                        pdf.CurrentPoint = r.BottomLeft;
                    }
                }

                Write_Line(pdf);
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                eventAggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
                throw;
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

        private void Write_Work_Order_Header(PDF_Helper pdf)
        {
            ///////////////
            // Line 1
            XRect r = pdf.GetTabRegion(0);
            XFont f = new XFont(pdf.CurrentFont.FontFamily.Name, 10);

            pdf.WriteLine("Service Request:", r, null, PDF_Helper.XPA_LeftMiddle, f);

            if (String.IsNullOrEmpty(Work_Order.Work_Order_ID) == false)
            {
                r = pdf.GetTabRegion(1, pdf.CurrentDefaultHeight);
                r.Inflate(-30, -2);
                pdf.DrawBarCode128(Work_Order.Work_Order_ID, r, pdf.CurrentBarCodeFont);
            }

            r = pdf.GetTabRegion(2, pdf.CurrentDefaultHeight);
            r.Inflate(-10, 0);
            pdf.WriteLine("Customer:", r, null, PDF_Helper.XPA_RightMiddle, f);

            if (String.IsNullOrEmpty(Work_Order.Customer_Name) == false)
            {
                r = pdf.GetTabRegion(3, pdf.CurrentDefaultHeight);
                pdf.WriteLine(Work_Order.Customer_Name, r, null, PDF_Helper.XPA_LeftMiddle, f);
            }
            pdf.GotoNextLine();

            ///////////////
            // Line 2
            r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
            pdf.WriteLine("Serial Number:", r, null, PDF_Helper.XPA_LeftMiddle, f);

            if (String.IsNullOrEmpty(Work_Order.PT_Serial) == false)
            {
                r = pdf.GetTabRegion(1, pdf.CurrentDefaultHeight);
                r.Inflate(-30, -2);
                pdf.DrawBarCode128(Work_Order.PT_Serial, r, pdf.CurrentBarCodeFont);
            }

            r = pdf.GetTabRegion(2, pdf.CurrentDefaultHeight);
            r.Inflate(-10, 0);
            pdf.WriteLine("Logged By:", r, null, PDF_Helper.XPA_RightMiddle, f);

            if (String.IsNullOrEmpty(Work_Order.Entered_By) == false)
            {
                r = pdf.GetTabRegion(3, pdf.CurrentDefaultHeight);
                pdf.WriteLine(Work_Order.Entered_By, r, null, PDF_Helper.XPA_LeftMiddle, f);
            }
            pdf.GotoNextLine();

            ///////////////
            // Line 3
            r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
            pdf.WriteLine("Model:", r, null, PDF_Helper.XPA_LeftMiddle, f);
            if (String.IsNullOrEmpty(Work_Order.PT_Model) == false)
            {
                r = pdf.GetTabRegion(1, pdf.CurrentDefaultHeight);
                pdf.WriteLine(Work_Order.PT_Model, r, null, PDF_Helper.XPA_CenterMiddle, f);
            }

            r = pdf.GetTabRegion(2, pdf.CurrentDefaultHeight);
            r.Inflate(-10, 0);
            pdf.WriteLine("Repair Type:", r, null, PDF_Helper.XPA_RightMiddle, f);

            if (String.IsNullOrEmpty(Work_Order.Repair_Type) == false)
            {
                r = pdf.GetTabRegion(3, pdf.CurrentDefaultHeight);
                pdf.WriteLine(Work_Order.Repair_Type, r, null, PDF_Helper.XPA_LeftMiddle, f);
            }
            pdf.GotoNextLine();

            ///////////////
            // Line 4
            r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
            pdf.WriteLine("Odometer-A/B:", r, null, PDF_Helper.XPA_LeftTop, f);
            pdf.WriteLine("Time-A/B:", r, null, PDF_Helper.XPA_LeftBottom, f);

            r = pdf.GetTabRegion(1, pdf.CurrentDefaultHeight);
            r.Inflate(-30, -2);

            if ((CUA_Log != null) && (CUA_Log.PT_Odometer.HasValue))
            {
                pdf.WriteLine(InfrastructureModule.Format_CU_Odometer(CUA_Log.PT_Odometer), r, null, PDF_Helper.XPA_LeftTop, f);
            }
            if ((CUB_Log != null) && (CUB_Log.PT_Odometer.HasValue))
            {
                pdf.WriteLine(InfrastructureModule.Format_CU_Odometer(CUB_Log.PT_Odometer), r, null, PDF_Helper.XPA_RightTop, f);
            }
            if ((CUA_Log != null) && (CUA_Log.PT_OperatingTime.HasValue))
            {
                pdf.WriteLine(InfrastructureModule.Format_CU_OperatingTime(CUA_Log.PT_OperatingTime), r, null, PDF_Helper.XPA_LeftBottom, f);
            }
            if ((CUB_Log != null) && (CUB_Log.PT_OperatingTime.HasValue))
            {
                pdf.WriteLine(InfrastructureModule.Format_CU_OperatingTime(CUB_Log.PT_OperatingTime), r, null, PDF_Helper.XPA_RightBottom, f);
            }

            r = pdf.GetTabRegion(2, pdf.CurrentDefaultHeight);
            r.Inflate(-10, 0);
            pdf.WriteLine("Owner:", r, null, PDF_Helper.XPA_RightMiddle, f);

            if (String.IsNullOrEmpty(Work_Order.Owner) == false)
            {
                r = pdf.GetTabRegion(3, pdf.CurrentDefaultHeight);
                pdf.WriteLine(Work_Order.Owner, r, null, PDF_Helper.XPA_LeftMiddle, f);
            }
            pdf.GotoNextLine();

            ///////////////
            // Line 5
            r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
            pdf.WriteLine("Technician:", r, null, PDF_Helper.XPA_LeftMiddle, f);

            if (String.IsNullOrEmpty(Work_Order.Technician_Name) == false)
            {
                r = pdf.GetTabRegion(1, pdf.CurrentDefaultHeight);
                pdf.WriteLine(Work_Order.Technician_Name, r, null, PDF_Helper.XPA_CenterMiddle, f);
            }

            r = pdf.GetTabRegion(2, pdf.CurrentDefaultHeight);
            r.Inflate(-10, 0);
            pdf.WriteLine("Start Date:", r, null, PDF_Helper.XPA_RightMiddle, f);

            if (Work_Order.Start_Date.HasValue == true)
            {
                r = pdf.GetTabRegion(3, pdf.CurrentDefaultHeight);
                pdf.WriteLine(Work_Order.Start_Date.Value.ToShortDateString(), r, null, PDF_Helper.XPA_LeftMiddle, f);
            }
            //else if (Work_Order.star.Start_Date_2.HasValue == true)
            //{
            //    r = pdf.GetTabRegion(3, pdf.CurrentDefaultHeight);
            //    pdf.WriteLine(Work_Order.Start_Date_2.Value.ToShortDateString(), r, null, PDF_Helper.XPA_LeftMiddle, f);
            //}
            Write_Line(pdf);
        }

        private void Write_Warranties(PDF_Helper pdf)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                pdf.SetTabs(new Double[] { .3, 8.2 });
                pdf.Set_Default_Height(pdf.Header_Font_3_Bold.Height);
                XRect r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
                pdf.WriteLine(String.Format("Warranties for Serial Number: {0}", Work_Order.PT_Serial), r, null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.GotoNextLine();

                pdf.CurrentFont = new XFont(pdf.CurrentFont.FontFamily.Name, 7);
                pdf.Set_Default_Height(pdf.CurrentFont.Height);

                pdf.SetTabs(new Double[] { .3, 1.3, 2.3, 3, 8.2 });

                List<Unit_Warranty> warranties = Syteline_UntWarr_Web_Service_Client_REST.Select_Unit_Warranty_Serial(InfrastructureModule.Token, Work_Order.PT_Serial);
                if ((warranties != null) && (warranties.Count > 0))
                {
                    foreach (Unit_Warranty warranty in warranties)
                    {
                        pdf.WriteLine(warranty.Start_Date.ToShortDateString(), pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.WriteLine(warranty.End_Date.ToShortDateString(), pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle);
                        if (warranty.End_Date.Date < DateTime.Today)
                        {
                            pdf.WriteLine("Expired", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle);
                        }
                        pdf.WriteLine(warranty.Description, pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.GotoNextLine();
                    }
                }
                Write_Line(pdf);
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void Write_Receiving_Info(PDF_Helper pdf)
        {
            pdf.SetTabs(new Double[] { .3, 8.2 });
            pdf.Set_Default_Height(pdf.Header_Font_3_Bold.Height);
            XRect r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
            pdf.WriteLine("Receiving Information", r, null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
            pdf.GotoNextLine();

            pdf.CurrentFont = new XFont(pdf.CurrentFont.FontFamily.Name, 7);
            pdf.Set_Default_Height(pdf.CurrentFont.Height);

            pdf.SetTabs(new Double[] { .3, 1.5, 8.2 });

            pdf.WriteLine("Receiving Notes:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            if (String.IsNullOrEmpty(Work_Order.Rec_Notes) == false)
            {
                String note = Work_Order.Rec_Notes.Replace("<br />", "\n");
                r = pdf.Calculate_Region_Size(pdf.GetTabRegion(1), ref note, pdf.CurrentFont);
                pdf.WriteLine(note, r, null, PDF_Helper.XPA_LeftMiddle);
                pdf.CurrentPoint = r.BottomLeft;
                pdf.CurrentPoint.Offset(0, 1);
            }
            else
            {
                pdf.GotoNextLine();
            }
            pdf.GotoNextLine(pdf.CurrentFont.Height / -2);

            pdf.SetTabs(new Double[] { .3, 1.3, 2.3, 3.3, 4.3, 5.3, 6.3, 7.3, 8.2 });

            pdf.WriteLine("Batteries:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Rec_Batteries, pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("InfoKeys:", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Rec_Infokeys, pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("G1 Keys:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            //pdf.WriteLine(Work_Order.Rec_Batteries, pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Kick Stand:", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Rec_Kickstand, pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle);
            pdf.GotoNextLine();

            pdf.WriteLine("Wheels:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Rec_Wheels, pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Hub Caps:", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Rec_Hubcaps, pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Fenders:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Rec_Fenders, pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle);
            //pdf.WriteLine("Kick Stand:", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            //pdf.WriteLine(Work_Order.Rec_Kickstand, pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle);
            pdf.GotoNextLine();

            pdf.WriteLine("Console Trim:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Rec_Console_Trim, pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Charge Port:", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Rec_Charge_Port, pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Mats:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Rec_Mats, pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Comfort Mats:", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Rec_Comfort_Mats, pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle);
            pdf.GotoNextLine();

            Write_Line(pdf);

        }

        private void Write_Picture_Info(PDF_Helper pdf)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                pdf.SetTabs(new Double[] { .3, 8.2 });
                pdf.Set_Default_Height(pdf.Header_Font_3_Bold.Height);
                XRect r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
                pdf.WriteLine("Picture Information", r, null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.GotoNextLine();

                pdf.CurrentFont = new XFont(pdf.CurrentFont.FontFamily.Name, 7);
                pdf.Set_Default_Height(pdf.CurrentFont.Height);

                pdf.SetTabs(new Double[] { .3, 1.8, 2.8, 3.8, 8.2 });

                List<Seg_SART_Pictures_Nodata> pictures = Syteline_PicND_Web_Service_Client_REST.Select_Seg_SART_Pictures_Nodata_SRONUM(InfrastructureModule.Token, Work_Order.Work_Order_ID);
                if (pictures != null)
                {
                    foreach (Seg_SART_Pictures_Nodata picture in pictures)
                    {
                        String timestamp = String.Format("{0} {1}", picture.Create_Date.ToShortDateString(), picture.Create_Date.ToShortTimeString());
                        pdf.WriteLine(timestamp, pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.WriteLine(picture.User_Name, pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.WriteLine(picture.Name, pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.WriteLine(picture.Description, pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.GotoNextLine();
                    }
                }

                Write_Line(pdf);
            }
            catch (AuthenticationNull_Exception ane)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ane));
                throw;
            }
            catch (Authentication_Exception ae)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ae));
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }

        private void Write_Battery_Info(PDF_Helper pdf)
        {
            pdf.SetTabs(new Double[] { .3, 8.2 });
            pdf.Set_Default_Height(pdf.Header_Font_3_Bold.Height);
            XRect r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
            pdf.WriteLine("Battery Information", r, null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
            pdf.GotoNextLine(5);
            XPoint currentPoint = pdf.GetTabRegion(0).TopLeft;

            pdf.CurrentFont = new XFont(pdf.CurrentFont.FontFamily.Name, 7);
            pdf.Set_Default_Height(pdf.CurrentFont.Height);

            pdf.SetTabs(new Double[] { .5, 1.35, 2.35, 3.1, 4.45, 5.3, 6.3, 7.05, 8.2 });

            pdf.WriteLine("A Side (Front)", pdf.GetTabRegion(0, 4), null, PDF_Helper.XPA_CenterMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine("B Side (Rear)", pdf.GetTabRegion(4, 4), null, PDF_Helper.XPA_CenterMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.GotoNextLine(5);

            pdf.WriteLine("Serial:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Battery_Serial_Front, pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Revision:", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Batt_Comments_Front, pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Serial:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Battery_Serial_Rear, pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Revision:", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Batt_Comments_Rear, pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle);
            pdf.GotoNextLine();

            pdf.WriteLine("Status:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Battery_Status_Front, pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Resistance:", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Battery_Rbat_Front, pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Status:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Battery_Status_Rear, pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Resistance:", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Battery_Rbat_Rear, pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle);
            pdf.GotoNextLine();

            pdf.WriteLine("Reason Failed:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Failure_Reason_A, pdf.GetTabRegion(1, 3), null, PDF_Helper.XPA_LeftMiddle);
            pdf.WriteLine("Reason Failed:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            pdf.WriteLine(Work_Order.Failure_Reason_B, pdf.GetTabRegion(5, 3), null, PDF_Helper.XPA_LeftMiddle);
            pdf.GotoNextLine(5);

            XRect box = new XRect(new XPoint(XUnit.FromInch(.3), currentPoint.Y), new XPoint(XUnit.FromInch(8.2), pdf.CurrentPoint.Y));
            pdf.DrawBox(box, new XPen(pdf.Color_Black, .5));
            pdf.DrawLine(box.TopCenter(), box.BottomCenter());

            Write_Line(pdf);
        }



        private void Write_Log_Info(PDF_Helper pdf)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Write_Section_Header(pdf, "CU Log Information");

                List<SART_CU_Logs> logs = SART_Log_Web_Service_Client_REST.Get_SART_CU_Logs_WORK_ORDER(InfrastructureModule.Token, Work_Order.Work_Order_ID);
                if ((logs == null) || (logs.Count == 0))
                {
                    SqlBooleanCriteria criteria = new SqlBooleanCriteria();
                    criteria.Add(new FieldData("PT_Serial", Work_Order.PT_Serial));
                    if (Work_Order.Start_Date.HasValue == true)
                    {
                        criteria.Add(new FieldData("Date_Time_Extracted", Work_Order.Start_Date.Value.Date, FieldCompareOperator.GreaterThanOrEqual));
                    }
                    else
                    {
                        criteria.Add(new FieldData("Date_Time_Extracted", DateTime.Today.AddDays(-7), FieldCompareOperator.GreaterThanOrEqual));
                    }
                    logs = SART_Log_Web_Service_Client_REST.Select_SART_CU_Logs_Criteria(InfrastructureModule.Token, criteria);
                }

                if ((logs != null) && (logs.Count > 0))
                {
                    pdf.CurrentFont = new XFont(pdf.CurrentFont.FontFamily.Name, 7);
                    pdf.Set_Default_Height(pdf.CurrentFont.Height);

                    pdf.SetTabs(new Double[] { .3, .7, 1.4, 1.9, 3, 4.5, 5.2, 5.9, 6.6, 7.3, 8.2 });

                    pdf.WriteLine("Side", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                    pdf.WriteLine("Serial", pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                    pdf.WriteLine("Epic", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                    pdf.WriteLine("Extracted", pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                    pdf.WriteLine("User", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                    pdf.WriteLine("SW Build", pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                    pdf.WriteLine("SW Vers", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                    pdf.WriteLine("Odometer", pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                    pdf.WriteLine("Time", pdf.GetTabRegion(8), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                    pdf.GotoNextLine();

                    logs = logs.OrderBy(x => x.Date_Time_Extracted).ThenBy(x => x.ID).ToList();
                    foreach (SART_CU_Logs log in logs)
                    {
                        Boolean changed = false;
                        if (String.IsNullOrEmpty(log.Work_Order) == true)
                        {
                            changed = true;
                            log.Work_Order = Work_Order.Work_Order_ID;
                        }
                        if (log.Odometer.HasValue == false)
                        {
                            changed = true;
                            log.Odometer = log.PT_Odometer;
                        }
                        if (log.Operating_Time.HasValue == false)
                        {
                            changed = true;
                            log.Operating_Time = log.PT_OperatingTime;
                        }
                        if (changed == true) SART_Log_Web_Service_Client_REST.Insert_SART_CU_Logs_Object(InfrastructureModule.Token, log);

                        pdf.WriteLine(log.Side.ToString(), pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.WriteLine(log.CU_Serial, pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.WriteLine(log.CU_Serial_Epic, pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.WriteLine(InfrastructureModule.Format_Timestamp(log.Date_Time_Extracted), pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.WriteLine(log.User_Name, pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.WriteLine(log.SW_Build, pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle);
                        pdf.WriteLine(log.SW_Version, pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle);

                        if (log.PT_Odometer.HasValue)
                        {
                            //Conversion.Meters = (Double)log.PT_Odometer.Value;
                            //String odometer;
                            //if (Application_Helper.IsMetric)
                            //{
                            //    odometer = String.Format("{0:F2}  km", Conversion.Kilometers);
                            //}
                            //else
                            //{
                            //    odometer = String.Format("{0:F2}  mi", Conversion.Miles);
                            //}
                            pdf.WriteLine(InfrastructureModule.Format_CU_Odometer(log.PT_Odometer), pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle);
                        }
                        if (log.PT_OperatingTime.HasValue)
                        {
                            pdf.WriteLine(InfrastructureModule.Format_CU_OperatingTime(log.PT_OperatingTime), pdf.GetTabRegion(8), null, PDF_Helper.XPA_LeftMiddle);
                        }
                        pdf.GotoNextLine();
                    }
                }

                Write_Line(pdf);
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                throw;
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

        private static XRect Write_Section_Header(PDF_Helper pdf, String header)
        {
            pdf.SetTabs(new Double[] { .3, 8.2 });
            pdf.Set_Default_Height(pdf.Header_Font_3_Bold.Height);
            XRect r = pdf.GetTabRegion(0, pdf.CurrentDefaultHeight);
            pdf.WriteLine(header, r, null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
            pdf.GotoNextLine();
            return r;
        }


        /// <summary>Public Method - Format_Task_Result</summary>
        /// <param name="task">Boolean?</param>
        /// <param name="tab">int</param>
        /// <param name="pdf">PDF_Helper</param>
        public void Format_Task_Result(Boolean? task, int tab, PDF_Helper pdf)
        {
            if (task.HasValue == false)
            {
                pdf.WriteLine("Not Performed", pdf.GetTabRegion(tab), XBrushes.DarkGoldenrod, PDF_Helper.XPA_LeftMiddle);
            }
            else if (task.Value == true)
            {
                pdf.WriteLine("Passed", pdf.GetTabRegion(tab), pdf.Brush_Green, PDF_Helper.XPA_LeftMiddle);
            }
            else
            {
                pdf.WriteLine("Failed", pdf.GetTabRegion(tab), pdf.Brush_Red, PDF_Helper.XPA_LeftMiddle);
            }
        }


        /// <summary>Public Method - Format_Date</summary>
        /// <param name="date">DateTime?</param>
        /// <param name="tab">int</param>
        /// <param name="pdf">PDF_Helper</param>
        public void Format_Date(DateTime? date, int tab, PDF_Helper pdf)
        {
            if (date.HasValue == false) return;
            else
            {
                pdf.WriteLine(date.Value.ToShortDateString(), pdf.GetTabRegion(tab));
            }
        }


        private void Write_Task_Summary_Info(PDF_Helper pdf)
        {
            Write_Section_Header(pdf, "Summary Information");

            pdf.CurrentFont = new XFont(pdf.CurrentFont.FontFamily.Name, 7);
            pdf.Set_Default_Height(pdf.CurrentFont.Height);

            pdf.SetTabs(new Double[] { .3, 1.5, 8.2 });

            pdf.WriteLine("Technician's Notes:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            if (String.IsNullOrEmpty(Work_Order.Observation_Technician) == false)
            {
                String note = Work_Order.Observation_Technician.Replace("<br />", "\n").Replace("\r", "");
                XRect r = pdf.Calculate_Region_Size(pdf.GetTabRegion(1), ref note);
                if (pdf.TestRegion(r) == false)
                {
                    while (String.IsNullOrEmpty(note) == false)
                    {
                        List<String> paragraph = new List<String>(note.Split(new char[] { '\n' }, StringSplitOptions.None));
                        List<String> paraRemain = new List<String>();
                        while (pdf.TestRegion(r) == false)
                        {
                            paraRemain.Insert(0, paragraph[paragraph.Count - 1]);
                            paragraph.RemoveAt(paragraph.Count - 1);
                            note = Strings.MergeName(paragraph, '\n');
                            r = pdf.Calculate_Region_Size(pdf.GetTabRegion(1), ref note);
                        }
                        pdf.WriteLine(note, r);
                        pdf.CurrentPoint = r.BottomLeft;
                        pdf.CurrentPoint.Offset(0, 1);
                        pdf.GotoNextLine();
                        if (paraRemain.Count > 0)
                        {
                            pdf.WriteLine("(Notes continued)", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 5, XFontStyle.Bold));
                        }
                        note = Strings.MergeName(paraRemain, '\n');
                        r = pdf.Calculate_Region_Size(pdf.GetTabRegion(1), ref note);
                    }
                }
                else
                {
                    pdf.WriteLine(note, r, null, PDF_Helper.XPA_LeftMiddle);
                    pdf.CurrentPoint = r.BottomLeft;
                    pdf.CurrentPoint.Offset(0, 1);
                }
            }
            else
            {
                pdf.GotoNextLine();
            }
            pdf.GotoNextLine();

            pdf.SetTabs(new Double[] { .3, 1.1, 2.3, 3.1, 4.3, 5.1, 6.3, 7.1, 8.2 });

            pdf.WriteLine("Start Config:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_Start_Config, 1, pdf);

            pdf.WriteLine("CU-A Log:", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_CUA_Extracted, 3, pdf);

            pdf.WriteLine("CU-B Log:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_CUB_Extracted, 5, pdf);

            pdf.WriteLine("CU-A Code:", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_CUA_Loaded, 7, pdf);

            pdf.GotoNextLine();


            pdf.WriteLine("CU-B Code:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_CUB_Loaded, 1, pdf);

            pdf.WriteLine("BSA Code:", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_BSA_Code_Loaded, 3, pdf);

            pdf.WriteLine("Motor:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_NormalMotor_Test, 5, pdf);

            pdf.WriteLine("Rider Detect:", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_RiderDetect_Test, 7, pdf);

            pdf.GotoNextLine();


            pdf.WriteLine("LED Test:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_LED_Test, 1, pdf);

            pdf.WriteLine("BSA Test:", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_BSA_Code_Loaded, 3, pdf);

            if (Application_Helper.Application_Name().Contains("Pilot") == true)
            {
                pdf.WriteLine("Ride Test:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
                Format_Task_Result(Work_Order.Is_Ride_Test, 5, pdf);
            }

            pdf.WriteLine("Final Config:", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 7, XFontStyle.Bold));
            Format_Task_Result(Work_Order.Is_Final_Config, 7, pdf);

            pdf.GotoNextLine();

            Write_Line(pdf);
        }


        private void Write_Repair_Info(PDF_Helper pdf)
        {
            Write_Section_Header(pdf, "Repair Information");

            XFont headFont = new XFont(pdf.CurrentFont.FontFamily.Name, 8, XFontStyle.Bold);
            XFont textFont = new XFont(pdf.CurrentFont.FontFamily.Name, 7);
            XFont barcodeFont = new XFont(pdf.CurrentFont.FontFamily.Name, 5);

            pdf.CurrentFont = textFont;
            pdf.Set_Default_Height(pdf.CurrentFont.Height);


            pdf.SetTabs(new Double[] { .3, 1.5, 8.2 });

            pdf.WriteLine("Repair Notes:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, headFont);
            if (Work_Order.Repair_Completed == true)
            {
                String note = Work_Order.Repair_Performed_Note.Replace("<br />", "\n").Replace("\r", "");
                XRect r = pdf.Calculate_Region_Size(pdf.GetTabRegion(1), ref note);
                if (pdf.TestRegion(r) == false)
                {
                    while (String.IsNullOrEmpty(note) == false)
                    {
                        List<String> paragraph = new List<String>(note.Split(new char[] { '\n' }, StringSplitOptions.None));
                        List<String> paraRemain = new List<String>();
                        while (pdf.TestRegion(r) == false)
                        {
                            paraRemain.Insert(0, paragraph[paragraph.Count - 1]);
                            paragraph.RemoveAt(paragraph.Count - 1);
                            note = Strings.MergeName(paragraph, '\n');
                            r = pdf.Calculate_Region_Size(pdf.GetTabRegion(1), ref note);
                        }
                        pdf.WriteLine(note, r);
                        pdf.CurrentPoint = r.BottomLeft;
                        pdf.CurrentPoint.Offset(0, 1);
                        pdf.GotoNextLine();
                        if (paraRemain.Count > 0)
                        {
                            pdf.WriteLine("(Notes continued)", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, new XFont(pdf.CurrentFont.FontFamily.Name, 5, XFontStyle.Bold));
                        }
                        note = Strings.MergeName(paraRemain, '\n');
                        r = pdf.Calculate_Region_Size(pdf.GetTabRegion(1), ref note);
                    }
                }
                else
                {
                    pdf.WriteLine(note, r, null, PDF_Helper.XPA_LeftMiddle);
                    pdf.CurrentPoint = r.BottomLeft;
                    pdf.CurrentPoint.Offset(0, 1);
                }
            }
            else
            {
                pdf.GotoNextLine();
            }



            List<SART_WO_Components> comps = SART_WOComp_Web_Service_Client_REST.Select_SART_WO_Components_WORK_ORDER_ID(InfrastructureModule.Token, Work_Order.Work_Order_ID);
            if (comps != null)
            {
                comps.Sort(new SART_WO_Components_Sequence_Comparer());
                pdf.GotoNextLine();

                pdf.SetTabs(new Double[] { .5, .75, 2.5, 2.75, 3.5, 4.25, 5, 7.5, 8.0, 8.2 });
                pdf.Set_Default_Height(pdf.CurrentFont.Height * 2.5);
                XPen boxPen = new XPen(pdf.Color_Black, .5);

                pdf.WriteBoxedLine("#", pdf.GetTabRegion(0), null, boxPen, pdf.Brush_Black, headFont, PDF_Helper.XPA_CenterMiddle);
                pdf.WriteBoxedLine("Part Number", pdf.GetTabRegion(1), null, boxPen, pdf.Brush_Black, headFont, PDF_Helper.XPA_CenterTop);
                pdf.WriteBoxedLine("Description", pdf.GetTabRegion(1), null, boxPen, pdf.Brush_Black, headFont, PDF_Helper.XPA_CenterBottom);
                pdf.WriteBoxedLine("Qty", pdf.GetTabRegion(2), null, boxPen, pdf.Brush_Black, headFont, PDF_Helper.XPA_CenterMiddle);
                pdf.WriteBoxedLine("Installed", pdf.GetTabRegion(3), null, boxPen, pdf.Brush_Black, headFont, PDF_Helper.XPA_CenterMiddle);
                pdf.WriteBoxedLine("Action", pdf.GetTabRegion(4), null, boxPen, pdf.Brush_Black, headFont, PDF_Helper.XPA_CenterMiddle);
                pdf.WriteBoxedLine("Approval", pdf.GetTabRegion(5), null, boxPen, pdf.Brush_Black, headFont, PDF_Helper.XPA_CenterMiddle);
                pdf.WriteBoxedLine("Old Serial", pdf.GetTabRegion(6), null, boxPen, pdf.Brush_Black, headFont, PDF_Helper.XPA_CenterTop);
                pdf.WriteBoxedLine("New Serial", pdf.GetTabRegion(6), null, boxPen, pdf.Brush_Black, headFont, PDF_Helper.XPA_CenterBottom);
                pdf.WriteBoxedLine("SAC", pdf.GetTabRegion(7), null, boxPen, pdf.Brush_Black, headFont, PDF_Helper.XPA_CenterMiddle);
                pdf.GotoNextLine();

                pdf.Set_Default_Height(pdf.CurrentFont.Height * 5);
                XRect r;

                foreach (SART_WO_Components comp in comps)
                {
                    for (int x = 0; x <= 7; x++)
                    {
                        r = pdf.GetTabRegion(x, (Double)pdf.CurrentFont.Height * 5);
                        if (pdf.TestRegion(r) == false)
                        {
                            pdf.AddNewPageDelegate(pdf);
                            pdf.GoToTopOfPage(10);
                            r = pdf.GetTabRegion(x, (Double)pdf.CurrentFont.Height * 5);
                        }
                        pdf.DrawBox(r, boxPen);
                    }

                    pdf.WriteLine(comp.Sequence.ToString(), pdf.GetTabRegion(0), null, PDF_Helper.XPA_CenterMiddle, textFont);

                    r = pdf.GetTabRegion(1, (Double)pdf.CurrentFont.Height * 2.25);
                    r.Inflate(-10, 0);
                    r.Offset(0, 3);
                    pdf.DrawBarCode128(comp.Part_Number, r, barcodeFont);

                    r = pdf.GetTabRegion(1);
                    r.Offset(0, -8);
                    pdf.WriteLine(comp.Part_Name, r, null, PDF_Helper.XPA_CenterBottom, textFont);
                    pdf.WriteLine(comp.Quantity.ToString(), pdf.GetTabRegion(2), null, PDF_Helper.XPA_CenterMiddle, textFont);
                    pdf.WriteLine(comp.Installed, pdf.GetTabRegion(3), null, PDF_Helper.XPA_CenterMiddle, textFont);
                    pdf.WriteLine(comp.Change_Type, pdf.GetTabRegion(4), null, PDF_Helper.XPA_CenterMiddle, textFont);
                    pdf.WriteLine(comp.Change_Approval, pdf.GetTabRegion(5), null, PDF_Helper.XPA_CenterMiddle, textFont);

                    r = pdf.GetTabRegion(6, (Double)pdf.CurrentFont.Height * 2.25);
                    r.Inflate(-10, -1);
                    pdf.DrawBarCode128(comp.Serial_Number_Old, r, barcodeFont);
                    r.Offset(0, r.Height + 5);
                    pdf.DrawBarCode128(comp.Serial_Number_New, r, barcodeFont);

                    Format_SAC(comp.Service_Code, 7, pdf);
                    pdf.GotoNextLine();
                }
            }

            pdf.Set_Default_Height(pdf.CurrentFont.Height);
            pdf.GotoNextLine();

            //pdf.CurrentFont = textFont;
            //pdf.CurrentDefaultHeight = pdf.CurrentFont.Height;

            //pdf.SetTabs(new Double[] { .3, 1.4, 2.3, 3.4, 4.3, 5.4, 6.3, 7.4, 8.2 });

            //pdf.WriteLine("Labor Hours:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, headFont);
            //if (Work_Order.Labor_Hours.HasValue)
            //{
            //    pdf.WriteLine(Work_Order.Labor_Hours.Value.ToString("F2"), pdf.GetTabRegion(1));
            //}
            //pdf.GotoNextLine();

            //Dictionary<String, DateTime> status = SART_2012_Web_Service_Client.Get_Work_Order_Status_History(Work_Order.Work_Order_ID);
            //pdf.WriteLine("Ready To Quote:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, headFont);
            //pdf.WriteLine("Date Quoted:", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, headFont);
            //pdf.WriteLine("Approved:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, headFont);
            //pdf.WriteLine("Declined:", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, headFont);

            ////Format_Date(Work_Order.Product_Shipped, 3, pdf);

            //if (status.ContainsKey("RdytoQte") == true) { Format_Date(status["RdytoQte"], 1, pdf); }
            //if (status.ContainsKey("Quoted") == true) { Format_Date(status["Quoted"], 3, pdf); }
            //if (status.ContainsKey("Rep Appr") == true) { Format_Date(status["Rep Appr"], 5, pdf); }
            //if (status.ContainsKey("Rep Decl") == true) { Format_Date(status["Rep Decl"], 7, pdf); }
            //pdf.GotoNextLine();

            //pdf.WriteLine("Repair Completed:", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, headFont);
            //pdf.WriteLine("Ready To Ship:", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, headFont);
            //pdf.WriteLine("Shipped:", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, headFont);
            //pdf.WriteLine("Closed:", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, headFont);

            //if (status.ContainsKey("Rep Comp") == true) { Format_Date(status["Rep Comp"], 1, pdf); }
            //if (status.ContainsKey("Shippable") == true) { Format_Date(status["Shippable"], 3, pdf); }
            //if (Work_Order.Shipped_Date.HasValue == true) { Format_Date(Work_Order.Shipped_Date, 5, pdf); }
            //if (status.ContainsKey("Closed") == true) { Format_Date(status["Closed"], 7, pdf); }

            //pdf.GotoNextLine();


            Write_Line(pdf);
        }


        private void Format_SAC(String sc, int tab, PDF_Helper pdf)
        {
            if (String.IsNullOrEmpty(sc)) return;
            String[] sac = Strings.Split(sc, ':');
            pdf.WriteLine(sac[sac.Length - 1], pdf.GetTabRegion(tab), null, PDF_Helper.XPA_CenterMiddle);
        }

        private void Reset()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Work_Order_ID = null;
                Work_Order = null;
                CUA_Log = null;
                CUB_Log = null;
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
