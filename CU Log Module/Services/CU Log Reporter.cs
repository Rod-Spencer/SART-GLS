using NLog;
using PdfSharp.Drawing;
using Segway.Modules.SART_Infrastructure;
using Segway.PDF.Objects;
using Segway.SART.CULog.Objects;
using Segway.SART.Objects;
using Segway.Service.CAN;
using Segway.Service.Common;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.Tools.PDFHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Segway.Modules.CU_Log_Module
{
    public class CU_Log_Reporter
    {
        private static Logger logger = Logger_Helper.GetCurrentLogger();
        const String Header1 = "Segway Service Report";
        String Header2 = "G2 PT CU Log";
        String Header3 = "";

        List<UInt16> LogArray = null;
        List<Fault_Extract> faultArray = null;
        //Boolean global_legacy_header_flag = false;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="log"></param>
        /// <param name="showRaw"></param>
        /// <param name="Report_Type"></param>
        /// <param name="reverseFaults"></param>
        /// <returns></returns>
        public PDF_Helper Generate(SART_CU_Logs log, Boolean showRaw, G2_Report_Types Report_Type = G2_Report_Types.Basic, Boolean reverseFaults = false)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                PDF_Helper pdf = new PDF_Helper();
                Header2 = String.Format("{0} / {1} - {2}", log.Work_Order, log.PT_Serial, log.CU_Side);
                Header3 = String.Format("{0} Report", Report_Type);
                if (reverseFaults == true)
                {
                    Header3 += "  (Reverse Order)";
                }
                pdf.CreateLetterDocument(Header1, Header2, Header3, headerHeight: 56, content: (Header_Content)3);
                Write_Footnote(pdf);
                pdf.AddNewPageDelegate = NewPage;
                pdf.SetTabs(new double[] { .5, 2, 4.5, 6, 8, 8.2 });
                pdf.GoToTopOfPage();

                LogArray = new List<ushort>(Strings.HexStringToShortArray(log.Log_Data.Replace(" ", "")));
                faultArray = CU_Log_Helper.Extract_Faults(LogArray.ToArray());
                if (faultArray == null)
                {
                    pdf.CurrentFont = new XFont(pdf.CurrentFont.FontFamily.Name, 40);
                    pdf.WriteLine("Corrupted Log!\n\nLog must be Formatted", pdf.CurrentRegion, pdf.Brush_Red, PDF_Helper.XPA_CenterTop);
                    return pdf;
                }
                if (reverseFaults == true)
                {
                    List<Fault_Extract> reversefaultArray = new List<Fault_Extract>();
                    int ndx = -1;
                    foreach (Fault_Extract fault in faultArray)
                    {
                        if (fault.Terminator == CU_Log_Statics.OPERATIONAL_FAULT_TERMINATOR)
                        {
                            ndx = 0;
                            reversefaultArray.Insert(ndx, fault);
                        }
                        else if (fault.Terminator == CU_Log_Statics.LINK_FAULT_TERMINATOR)
                        {
                            ndx++;
                            reversefaultArray.Insert(ndx, fault);
                        }
                    }
                    faultArray = reversefaultArray;
                }

                Write_Header_Information(pdf, log, Report_Type);

                pdf.DrawLine(pdf.GetTabRegion(0, 4));
                pdf.GotoNextLine();

                // String[] faults = CU_Log_Statics.Extract_Faults_To_StringArray(log.Log_Data);
                String Initfault = CU_Log_Helper.Extract_Initialization_Fault(log.Log_Data);
                if ((Initfault == null) || Initfault.StartsWith("FFFFFFFFFFFF"))
                {
                    pdf.WriteLine("No Initialization Fault", pdf.GetTabRegion(0, 4), paraformat: PDF_Helper.XPA_CenterMiddle, font: pdf.Header_Font_2);
                }
                else
                {
                    G2Log_Fault init = new G2Log_Fault(Initfault);
                    pdf.WriteLine("Most Recent Initialization Fault", pdf.GetTabRegion(0, 4), paraformat: PDF_Helper.XPA_CenterMiddle, font: pdf.Header_Font_2);
                    pdf.GotoNextLine(5);
                    Report_Fault(pdf, init, log, Report_Type);
                }

                if ((faultArray != null) && (faultArray.Count > 0))
                {
                    G2Log_Fault opFault = null;
                    foreach (Fault_Extract fault in faultArray)
                    {
                        if (fault.Terminator == CU_Log_Statics.OPERATIONAL_FAULT_TERMINATOR)
                        {
                            opFault = new G2Log_Fault(fault.Data);
                            XRect r = pdf.GetTabRegion(0, 4);
                            r.Inflate(-XUnit.FromInch(1), 0);
                            r.Offset(0, 10);
                            pdf.DrawLine(r);
                            pdf.CurrentPoint = r.BottomLeft;
                            pdf.WriteLine(String.Format("Fault #{0}", fault.OrdinalFault), pdf.GetTabRegion(0, 4), paraformat: PDF_Helper.XPA_CenterMiddle, font: pdf.Header_Font_2);
                            pdf.GotoNextLine();
                            if (fault.Status != Fault_Extract_Status.Good)
                            {
                                pdf.WriteLine("This Fault has an incorrect format and may contain incorrect data", pdf.GetTabRegion(0, 4), pdf.Brush_Red, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3_Bold);
                                pdf.GotoNextLine();
                                pdf.WriteLine(String.Format("{0}", fault), pdf.GetTabRegion(0, 4), pdf.Brush_Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                                pdf.GotoNextLine();
                            }
                            Report_Fault(pdf, opFault, log, Report_Type);
                        }
                        else if (fault.Terminator == CU_Log_Statics.LINK_FAULT_TERMINATOR)
                        {
                            G2Log_Link opLink = new G2Log_Link(fault.Data, opFault);

                            XRect r = pdf.GetTabRegion(0, 4);
                            r.Inflate(-XUnit.FromInch(2), 0);
                            pdf.DrawLine(r);
                            pdf.CurrentPoint = r.BottomLeft;
                            pdf.WriteLine(String.Format("Link #{0} of Fault #{1}", fault.OrdinalLink, fault.OrdinalFault), pdf.GetTabRegion(0, 4), pdf.Brush_Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                            pdf.GotoNextLine();
                            if (fault.Status != Fault_Extract_Status.Good)
                            {
                                pdf.WriteLine("This Linked Fault has an incorrect format and may contain incorrect data", pdf.GetTabRegion(0, 4), pdf.Brush_Red, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3_Bold);
                                pdf.GotoNextLine();
                                pdf.WriteLine(String.Format("{0}", fault), pdf.GetTabRegion(0, 4), pdf.Brush_Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                                pdf.GotoNextLine();
                            }
                            Report_Link(pdf, opLink, log, Report_Type);
                        }
                    }
                }
                else
                {
                    pdf.GotoNextLine();
                    pdf.GotoNextLine();
                    XRect r = pdf.GetTabRegion(0, 4, 50);
                    pdf.CurrentRegion = pdf.WriteLine("No Operational Faults", r, pdf.Brush_Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                }
                pdf.GotoNextLine();
                XRect rr = pdf.GetTabRegion(0, 4);
                pdf.DrawLine(rr);
                pdf.GotoNextLine();
                rr = pdf.GetTabRegion(0, 4);
                pdf.WriteLine("End of Report", rr, paraformat: PDF_Helper.XPA_CenterMiddle);

                if ((Report_Type == G2_Report_Types.Advanced) && (showRaw == true))
                {
                    NewPage(pdf);
                    faultArray = CU_Log_Helper.Extract_Faults(LogArray.ToArray());
                    pdf.SetTabs(new double[] { 0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5 });
                    pdf.GotoNextLine();
                    pdf.GotoNextLine();

                    Double storeTop = pdf.Top;

                    pdf.WriteLine(pdf.GetTabRegion(0, 9), "Beginning of Raw Data", null, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                    pdf.GotoNextLine();
                    //pdf.CurrentFont = new XFont("Courier New", 12, XFontStyle.Bold);
                    rr = pdf.GetTabRegion(0, 9);
                    pdf.DrawLine(rr);
                    pdf.GotoNextLine();

                    String[] data = Strings.Split(log.Log_Data, ' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int x = 0; x < 256; x += 8)
                    {
                        pdf.CurrentFont = new XFont("Courier New", 12);
                        pdf.WriteLine(0, String.Format("{0:X2}:", x));
                        pdf.CurrentFont = new XFont("Courier New", 12, XFontStyle.Bold);
                        for (int y = 0; y < 8; y++)
                        {
                            pdf.WriteLine(pdf.GetTabRegion(y + 1), data[x + y], GetColor(x + y, faultArray));
                        }
                        pdf.GotoNextLine();
                    }
                    pdf.GotoNextLine();
                    rr = pdf.GetTabRegion(0, 9);
                    pdf.DrawLine(rr);
                    pdf.GotoNextLine();
                    pdf.WriteLine(pdf.GetTabRegion(0, 9), "End of Raw Data", null, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);

                    pdf.Top = storeTop;
                    pdf.PushTabs(new double[] { 5.2, 5.5, 5.6, 6.0, 8 });

                    pdf.WriteLine(pdf.GetTabRegion(0, 4), "Word Mappings", null, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                    pdf.GotoNextLine();
                    rr = pdf.GetTabRegion(0, 4);
                    pdf.DrawLine(rr);
                    pdf.GotoNextLine();

                    XRect r = pdf.GetTabRegion(0, 4);
                    pdf.WriteLine(r, "Header", XBrushes.Purple, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                    r.Offset(0, 1);
                    pdf.DrawLine(r.BottomLeft, r.BottomRight, new XPen(pdf.Color_Purple, .5));
                    r.Offset(0, 1);
                    pdf.Top = r.Bottom;

                    pdf.WriteLine(0, CU_Log_Statics.HEADER_REVISION.ToString(), XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Revision", XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, CU_Log_Statics.HEADER_BUS_VOLTAGE.ToString(), XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Bus Voltage", XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, CU_Log_Statics.HEADER_BATTERY_RESISTANCE.ToString(), XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Battery Resistance", XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, String.Format("{0}-{1}", CU_Log_Statics.HEADER_OPERATION_TIME_HIGH, CU_Log_Statics.HEADER_OPERATION_TIME_LOW), XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Operation Time", XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, CU_Log_Statics.HEADER_HALT_FAULT_FILE.ToString(), XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Halt Fault File", XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, CU_Log_Statics.HEADER_HALT_FAULT_LINE.ToString(), XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Halt Fault Line", XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, CU_Log_Statics.HEADER_COUNTERS.ToString(), XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Counters", XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, CU_Log_Statics.HEADER_NEXT_FAULT_INDEX.ToString(), XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Next Fault Index", XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, String.Format("{0}-{1}", CU_Log_Statics.HEADER_ODOMETER_HIGH, CU_Log_Statics.HEADER_ODOMETER_LOW), XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Odometer", XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    //pdf.WriteLine(0, CU_Log_Statics.HEADER_SIZE.ToString(), XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    //pdf.WriteLine(1, "Size", XBrushes.Purple, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();

                    pdf.PushTabs(new double[] { 5.2, 6.6, 8 });

                    r = pdf.GetTabRegion(0, 2);
                    pdf.WriteLine(r, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                    r = pdf.GetTabRegion(0);
                    pdf.WriteLine(r, "Init/Op. Fault", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                    r = pdf.GetTabRegion(1);
                    pdf.WriteLine(r, "Link Fault", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);

                    r = pdf.GetTabRegion(0, 2);
                    r.Offset(0, 1);
                    pdf.DrawLine(r.BottomLeft, r.BottomRight, new XPen(pdf.Color_Black, .5));
                    r.Offset(0, 1);
                    pdf.Top = r.Bottom;
                    pdf.PopTabs();

                    pdf.WriteLine(0, "0-1", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Operation Time", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "2-3", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Odometer", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "4", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "0", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Hazards", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "5", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "1", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Fault Comm", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "6", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "2", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Fault Sensor Local", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "7", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "3", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Fault Sensor Remote", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "8", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "4", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Fault Actuator Local", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "9", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "5", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Fault Actuator Remote", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "10", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "6", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "TSW-1", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "11", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "7", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "TSW-2", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "12", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "8", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "TSW-3", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "13", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "9", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "GP STS-1", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "14", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "10", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "GP STS-2", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "15", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "11", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "GP STS-3", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "16", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "12", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "GP STS-4", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "17", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "13", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Pitch", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "18", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "14", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Pitch Rate", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "19", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "15", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Wheel Speed Left", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "20", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "16", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Wheel Speed Right", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "21", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "17", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "Fault Time After Power/Fault", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();
                    pdf.WriteLine(0, "22", XBrushes.Blue, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(1, "/", XBrushes.Black, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(2, "18", XBrushes.Green, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_3);
                    pdf.WriteLine(3, "End Of Fault Marker", XBrushes.Black, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                    pdf.GotoNextLine();

                    pdf.PopTabs();
                }
                return pdf;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
                //return null;
            }
            finally
            {
                logger.Debug($"Leaving - {MethodBase.GetCurrentMethod().Name}");
            }
        }

        private XBrush GetColor(int pos, List<Fault_Extract> faults)
        {
            if (pos < 11) return XBrushes.Purple;
            if (pos < 34) return XBrushes.Red;
            for (int x = 0; x < faults.Count; x++)
            {
                Fault_Extract fault = faults[x];

                if (fault.PositionStart > fault.PositionEnd)
                {
                    if ((pos >= fault.PositionStart) || (pos <= fault.PositionEnd))
                    {
                        if ((x % 2) == 1) return XBrushes.Blue;
                        return XBrushes.Green;
                    }
                }
                else if (fault.PositionStart < fault.PositionEnd)
                {
                    if ((pos >= fault.PositionStart) && (pos <= fault.PositionEnd))
                    {
                        if ((x % 2) == 1) return XBrushes.Blue;
                        return XBrushes.Green;
                    }
                }
            }
            return XBrushes.Black;
        }

        private static void Write_Footnote(PDF_Helper pdf)
        {
            XFont fnt = new XFont("Times New Roman", 6);
            XPoint pt = new XPoint(pdf.CurrentMargin.Left, pdf.CurrentMargin.Bottom + fnt.Height);
            pdf.WriteLine(pt, String.Format("Generated by {0}", Application_Helper.Name_And_Version()), font: fnt, force: true);
        }

        private void Write_Header_Information(PDF_Helper pdf, SART_CU_Logs log, G2_Report_Types Report_Type)
        {
            String headerStr = log.Log_Data.Replace(" ", "").Substring(0, 4 * CU_Log_Statics.HEADER_SIZE);
            String wheelSpeedMessage = "This build is an early Gen-2 release that did not differentiate between the i2 and x2.  Therefore, both the i2 and x2 wheel speeds will be reported.";
            UInt16[] headerWords = Strings.HexStringToShortArray(headerStr);


            XRect r = pdf.GetTabRegion(0, height: pdf.Header_Font_1_Bold.Height);
            pdf.WriteLine("Work Order", r, paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.CurrentFontBold);

            r = pdf.GetTabRegion(1, height: pdf.Header_Font_1_Bold.Height);
            pdf.WriteLine(log.Work_Order, r, paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_1_Bold);

            r = pdf.GetTabRegion(2, height: pdf.Header_Font_1_Bold.Height);
            pdf.WriteLine("PT Serial/Side", r, paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.CurrentFontBold);

            r = pdf.GetTabRegion(3, height: pdf.Header_Font_1_Bold.Height);
            pdf.WriteLine(String.Format("{0} - {1}", log.PT_Serial, log.Side), r, paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_1_Bold);
            pdf.CurrentPoint = r.BottomLeft;




            r = pdf.GetTabRegion(0, height: pdf.Header_Font_1_Bold.Height);
            pdf.WriteLine("Operation Time", r, paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.CurrentFontBold);
            r = pdf.GetTabRegion(1, height: pdf.Header_Font_1_Bold.Height);
            pdf.WriteLine(InfrastructureModule.Format_CU_OperatingTime(log.PT_OperatingTime), r, paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_1_Bold);
            r = pdf.GetTabRegion(2, height: pdf.Header_Font_1_Bold.Height);
            pdf.WriteLine("Odometer", r, paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.CurrentFontBold);
            r = pdf.GetTabRegion(3, height: pdf.Header_Font_1_Bold.Height);
            pdf.WriteLine(InfrastructureModule.Format_CU_Odometer(log.PT_Odometer), r, paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_1_Bold);
            pdf.CurrentPoint = r.BottomLeft;



            int Battery_Resistance = LogArray[CU_Log_Statics.HEADER_BATTERY_RESISTANCE];
            r = pdf.GetTabRegion(0, height: pdf.Header_Font_1_Bold.Height);
            pdf.WriteLine("Battery Resistance", r, paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.CurrentFontBold);
            r = pdf.GetTabRegion(1, height: pdf.Header_Font_1_Bold.Height);
            String ohms = String.Format("~ {0:F4} (rbat ohms) ", Battery_Resistance / 1024.0);
            pdf.WriteLine(ohms, r, paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_1_Bold);
            var size = pdf.MeasureString(ohms, pdf.Header_Font_1_Bold);
            r.Offset(size.Width, 0);
            pdf.WriteLine("(At most recent Critical Hazard)", r, paraformat: PDF_Helper.XPA_LeftBottom, font: pdf.CurrentFont);
            pdf.CurrentPoint = r.BottomLeft;



            logger.Debug("Header: {0}", Converter.UInt16ArrayToHexString(headerWords));
            pdf.WriteLine(0, "Extracted");
            pdf.WriteLine(1, Format_Date_String(log.Date_Time_Extracted));
            XRect er = pdf.GetTabRegion(1);
            er.Inflate(-20, 0);
            pdf.WriteLine(er, log.SART_Extraction, paraformat: PDF_Helper.XPA_RightMiddle);
            pdf.WriteLine(2, "Extracted By");
            pdf.WriteLine(3, log.User_Name);
            pdf.GotoNextLine();




            pdf.WriteLine(0, "Build");
            pdf.WriteLine(1, log.SW_Build);
            pdf.WriteLine(2, "SW Version");
            pdf.WriteLine(3, log.SW_Version);
            pdf.GotoNextLine();

            switch (int.Parse(log.SW_Build.Substring(0, 4).Trim(), NumberStyles.Integer))
            {
                case 1275:
                case 1351:
                case 1352:
                case 1281:
                    break;
                default:
                    XFont font = new XFont("Times New Roman", 8);
                    //XPoint left = pdf.GetTabPoint(1);
                    //XRect r = new XRect(left, new XSize((pdf.CurrentPage.Width - (2 * left.X)), font.Height));

                    r = pdf.GetTabRegion(1, 2);
                    r = pdf.Calculate_Region_Size(r, ref wheelSpeedMessage, font);

                    pdf.WriteLine(r, wheelSpeedMessage, pdf.Brush_Blue, PDF_Helper.XPA_CenterMiddle, font);
                    //pdf.WriteLine(r, wheelSpeedMessage, pdf.Brush_Blue, null, font);
                    pdf.CurrentPoint = r.BottomLeft;
                    break;
            }

            pdf.WriteLine(0, "SW Part No");
            pdf.WriteLine(1, log.SW_Part_Number);
            pdf.WriteLine(2, "CU Serial");
            pdf.WriteLine(3, log.CU_Serial);
            pdf.GotoNextLine();

            if (Report_Type == G2_Report_Types.Advanced)
            {
                pdf.WriteLine(0, "Startup Count");
                pdf.WriteLine(1, ((headerWords[CU_Log_Statics.HEADER_COUNTERS] >> 8) & 0x7F).ToString());
                pdf.WriteLine(2, "Shutdown Count");
                pdf.WriteLine(3, (headerWords[CU_Log_Statics.HEADER_COUNTERS] & 0x7F).ToString());
                pdf.GotoNextLine();

                pdf.WriteLine(0, "Log Revision");
                pdf.WriteLine(1, headerWords[CU_Log_Statics.HEADER_REVISION].ToString());
                pdf.GotoNextLine();
            }

            Write_Header_Hazard_Info(pdf, Report_Type);

            if ((headerWords[CU_Log_Statics.HEADER_HALT_FAULT_FILE] != 0) || (headerWords[CU_Log_Statics.HEADER_HALT_FAULT_LINE] != 0))
            {
                r = pdf.GetTabRegion(0, 4);
                String msg;
                if (Report_Type == G2_Report_Types.Basic)
                {
                    msg = "HALT Processor Fault";
                }
                else
                {
                    msg = String.Format("ALERT! SK halt processor recorded at line {0} of file {1}.",
                        headerWords[CU_Log_Statics.HEADER_HALT_FAULT_LINE], headerWords[CU_Log_Statics.HEADER_HALT_FAULT_FILE]);
                }
                pdf.WriteLine(msg, r, pdf.Brush_Red, PDF_Helper.XPA_CenterMiddle);
                pdf.GotoNextLine();
            }

            if ((headerWords[CU_Log_Statics.FAULT_WRITE_POINTER_INDEX] & CU_Log_Statics.FAULT_LATCH_MASK) != 0)
            {
                r = pdf.GetTabRegion(0, 4, pdf.Header_Font_2.Height);
                pdf.WriteLine("Fault Log is Latched!", r, pdf.Brush_Red, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                pdf.GotoNextLine();
            }
        }

        private String Format_Seconds_Time(UInt32 seconds)
        {
            if (seconds == 0) return "0 seconds";
            TimeSpan ts = new TimeSpan(0, 0, (int)seconds);
            return Format_Time(ts);
        }

        private String Format_Milliseconds_Time(UInt32 ms)
        {
            if (ms == 0) return "0 milliseconds";
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, (int)ms);
            return Format_Time(ts);
        }

        private string Format_Time(TimeSpan ts)
        {
            StringBuilder sb = new StringBuilder();

            if (ts.Days != 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.AppendFormat("{0} day", (int)ts.Days);
                if (ts.Days > 1) sb.Append("s");
            }

            if (ts.Hours != 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.AppendFormat("{0} hour", (int)ts.Hours);
                if (ts.Hours > 1) sb.Append("s");
            }

            if (ts.Minutes != 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.AppendFormat("{0} minute", ts.Minutes);
                if (ts.Minutes > 1) sb.Append("s");
            }

            if (ts.Seconds != 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.AppendFormat("{0} second", ts.Seconds);
                if (ts.Seconds > 1) sb.Append("s");
            }

            if (ts.Milliseconds != 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.AppendFormat("{0} millisecond", ts.Milliseconds);
                if (ts.Milliseconds > 1) sb.Append("s");
            }

            return sb.ToString();
        }

        private void Report_Fault(PDF_Helper pdf, G2Log_Fault init, SART_CU_Logs log, G2_Report_Types Report_Type)
        {
            XRect r = pdf.GetTabRegion(0, 4);
            pdf.WriteLine(String.Format("Occurred {0} after Power Cycle", Format_Seconds_Time(init.FaultTimeAfterPower)), r, paraformat: PDF_Helper.XPA_CenterTop);
            pdf.GotoNextLine(5);
            pdf.WriteLine(0, "Operating Time", font: pdf.CurrentFontBold);
            pdf.WriteLine(1, InfrastructureModule.Format_Short_Time(init.Time), font: pdf.CurrentFontBold);
            pdf.WriteLine(2, "Odometer", font: pdf.CurrentFontBold);
            Conversion.Meters = init.Odometer;
            if (Application_Helper.IsMetric)
            {
                pdf.WriteLine(3, String.Format("{0:F2}  km", Conversion.Kilometers), font: pdf.CurrentFontBold);
            }
            else
            {
                pdf.WriteLine(3, String.Format("{0:F2}  mi", Conversion.Miles), font: pdf.CurrentFontBold);
            }
            pdf.GotoNextLine();

            if (Report_Type == G2_Report_Types.Advanced)
            {
                Write_Faults(pdf, init, true);

                ////////////////////////////////////////////////////////////////
                //// Hazards
                //XPoint startPoint = pdf.CurrentPoint;
                //int startPageNo = pdf.CurrentPageNumber;
                //Process_Faults(pdf, init.Hazards, new G2_Hazard_Faults(), "Hazard Faults", bold: true);
                //XPoint LowPoint = pdf.CurrentPoint;
                //int pageno = pdf.CurrentPageNumber;

                //Calculate_Start_Point(pdf, startPoint, startPageNo);
                //Process_Faults(pdf, init.Hazards, new G2_Hazard_Faults(), "Hazard Faults", location: G2_CU_Location.Remote);
                //Position_End_Point(pdf, pageno, LowPoint);
                //// Hazards
                ////////////////////////////////////////////////////////////////


                ////////////////////////////////////////////////////////////////
                //// Comm Faults
                //startPoint = pdf.CurrentPoint;
                //startPageNo = pdf.CurrentPageNumber;
                //Process_Faults(pdf, init.Fault_Comm, new G2_Comm_Faults(), "Local Comm Faults", bold: true);
                //LowPoint = pdf.CurrentPoint;
                //pageno = pdf.CurrentPageNumber;

                //Calculate_Start_Point(pdf, startPoint, startPageNo);
                //Process_Faults(pdf, init.Fault_Comm, new G2_Comm_Faults(), "Remote Comm Faults", location: G2_CU_Location.Remote);
                //Position_End_Point(pdf, pageno, LowPoint);
                //// Comm Faults
                ////////////////////////////////////////////////////////////////

                ////////////////////////////////////////////////////////////////
                //// Sensor Faults
                //startPoint = pdf.CurrentPoint;
                //startPageNo = pdf.CurrentPageNumber;
                //Process_Faults(pdf, init.Fault_Sensor_Local, new G2_Sensor_Faults_Local(), "Local Sensor Faults", bold: true);
                //LowPoint = pdf.CurrentPoint;
                //pageno = pdf.CurrentPageNumber;

                //Calculate_Start_Point(pdf, startPoint, startPageNo);
                //Process_Faults(pdf, init.Fault_Sensor_Remote, new G2_Sensor_Faults_Remote(), "Remote Sensor Faults", location: G2_CU_Location.Remote);
                //Position_End_Point(pdf, pageno, LowPoint);
                //// Sensor Faults
                ////////////////////////////////////////////////////////////////


                ////////////////////////////////////////////////////////////////
                //// Actuator Faults
                //startPoint = pdf.CurrentPoint;
                //startPageNo = pdf.CurrentPageNumber;
                //Process_Faults(pdf, init.Fault_Actuator_Local, new G2_Actuator_Faults_Local(), "Local Actuator Faults", bold: true);
                //LowPoint = pdf.CurrentPoint;
                //pageno = pdf.CurrentPageNumber;

                //Calculate_Start_Point(pdf, startPoint, startPageNo);
                //Process_Faults(pdf, init.Fault_Actuator_Remote, new G2_Actuator_Faults_Remote(), "Remote Actuator Faults", location: G2_CU_Location.Remote);
                //Position_End_Point(pdf, pageno, LowPoint);
                //// Actuator Faults
                ////////////////////////////////////////////////////////////////
            }
            else
            {
                Process_Faults(pdf, init.Hazards, new G2_Hazard_Faults(), "Hazard Faults", bold: true);
                Process_Faults(pdf, (UInt16)(init.Fault_Comm & CU_Log_Statics.LOCAL_COMM_FAULTS), new G2_Comm_Faults(), "Comm Faults", bold: true);
                Process_Faults(pdf, init.Fault_Sensor_Local, new G2_Sensor_Faults_Local(), "Sensor Faults", bold: true);
                if ((init.GP_STS1 == 0xDECA) && (init.GP_STS2 == 0xFC0F) && (init.GP_STS3 == 0xFEEB) && (init.GP_STS4 == 0xEBAD))
                {
                    Process_Faults(pdf, (UInt16)(init.Fault_Actuator_Local & (UInt16)Masks.MOTOR_DRIVE_FAULT_MASK), new G2_Alternate_Actuator_Faults(), "Actuator Faults", bold: true);
                }
                else
                {
                    Process_Faults(pdf, (UInt16)(init.Fault_Actuator_Local & (UInt16)Masks.MOTOR_DRIVE_FAULT_MASK), new G2_Actuator_Faults_Local(), "Actuator Faults", bold: true);
                }
            }

            if (Report_Type == G2_Report_Types.Basic)
            {
                Decode_Basic_Report(pdf, init, log.CU_Side);
            }
            else if (Report_Type == G2_Report_Types.Advanced)
            {
                Decode_Advanced_Report(pdf, log, init, log.CU_Side);
            }

            if (Report_Type == G2_Report_Types.Advanced)
            {
                pdf.GotoNextLine(pdf.CurrentFont.Height / -2);
                pdf.WriteLine(0, "Pitch");
                pdf.WriteLine(1, String.Format("{0:F4} (degrees)", (Int16)init.Pitch / CU_Log_Statics.PITCH_CT_PER_PITCH_DEG));
                pdf.WriteLine(2, "Pitch Rate");
                pdf.WriteLine(3, String.Format("{0:F4} (deg/sec)", (Int16)init.PitchRate / CU_Log_Statics.RATE_CT_PER_RATE_DEGPERSEC));
                pdf.GotoNextLine();

                Write_WheelSpeeds(pdf, init, log);
                pdf.GotoNextLine();
            }

        }

        private void Write_WheelSpeeds(PDF_Helper pdf, G2Log_Link link, SART_CU_Logs log)
        {
            G2Log_Fault f = new G2Log_Fault(link);
            Write_WheelSpeeds(pdf, f, log);
        }

        private void Write_WheelSpeeds(PDF_Helper pdf, G2Log_Fault init, SART_CU_Logs log)
        {
            try
            {
                switch (int.Parse(log.SW_Build.Substring(0, 4).Trim(), NumberStyles.Integer))
                {
                    case 1275:
                    case 1351:
                        // Left Wheel
                        Write_I2_LeftWheel(pdf, init);

                        // Right Wheel
                        Write_I2_RightWheel(pdf, init);
                        pdf.GotoNextLine();
                        break;

                    case 1352:
                    case 1281:
                        // Left Wheel
                        Write_X2_LeftWheel(pdf, init);

                        // Right Wheel
                        Write_X2_RightWheel(pdf, init);
                        pdf.GotoNextLine();
                        break;

                    default:
                        // I2
                        // Left Wheel
                        Write_I2_LeftWheel(pdf, init);

                        // Right Wheel
                        Write_I2_RightWheel(pdf, init);
                        pdf.GotoNextLine();


                        // X2
                        // Left Wheel
                        Write_X2_LeftWheel(pdf, init);

                        // Right Wheel
                        Write_X2_RightWheel(pdf, init);
                        pdf.GotoNextLine();
                        break;
                }
            }
            catch
            {
                // I2
                // Left Wheel
                Write_I2_LeftWheel(pdf, init);

                // Right Wheel
                Write_I2_RightWheel(pdf, init);
                pdf.GotoNextLine();


                // X2
                // Left Wheel
                Write_X2_LeftWheel(pdf, init);

                // Right Wheel
                Write_X2_RightWheel(pdf, init);
                pdf.GotoNextLine();
            }
        }

        private void Write_X2_RightWheel(PDF_Helper pdf, G2Log_Fault init)
        {
            pdf.WriteLine(2, "X2 Right Wheel Speed");
            Conversion.Miles = (Int16)init.WheelSpeedRight / CU_Log_Statics.X_WHEEL_COUNTS_TO_MPH;
            if (Application_Helper.IsMetric)
            {
                pdf.WriteLine(3, String.Format("{0:F4} (km/h)", Conversion.Kilometers));
            }
            else
            {
                pdf.WriteLine(3, String.Format("{0:F4} (mph)", Conversion.Miles));
            }
        }

        private void Write_X2_LeftWheel(PDF_Helper pdf, G2Log_Fault init)
        {
            pdf.WriteLine(0, "X2 Left Wheel Speed");
            Conversion.Miles = (Int16)init.WheelSpeedLeft / CU_Log_Statics.X_WHEEL_COUNTS_TO_MPH;
            if (Application_Helper.IsMetric)
            {
                pdf.WriteLine(1, String.Format("{0:F4} (km/h)", Conversion.Kilometers));
            }
            else
            {
                pdf.WriteLine(1, String.Format("{0:F4} (mph)", Conversion.Miles));
            }
        }

        private void Write_I2_RightWheel(PDF_Helper pdf, G2Log_Fault init)
        {
            pdf.WriteLine(2, "I2 Right Wheel Speed");
            Conversion.Miles = (Int16)init.WheelSpeedRight / CU_Log_Statics.I_WHEEL_COUNTS_TO_MPH;
            if (Application_Helper.IsMetric)
            {
                pdf.WriteLine(3, String.Format("{0:F4} (km/h)", Conversion.Kilometers));
            }
            else
            {
                pdf.WriteLine(3, String.Format("{0:F4} (mph)", Conversion.Miles));
            }
        }

        private void Write_I2_LeftWheel(PDF_Helper pdf, G2Log_Fault init)
        {
            pdf.WriteLine(0, "I2 Left Wheel Speed");
            Conversion.Miles = (Int16)init.WheelSpeedLeft / CU_Log_Statics.I_WHEEL_COUNTS_TO_MPH;
            if (Application_Helper.IsMetric)
            {
                pdf.WriteLine(1, String.Format("{0:F4} (km/h)", Conversion.Kilometers));
            }
            else
            {
                pdf.WriteLine(1, String.Format("{0:F4} (mph)", Conversion.Miles));
            }
        }

        private void Report_Link(PDF_Helper pdf, G2Log_Link init, SART_CU_Logs log, G2_Report_Types Report_Type)
        {
            XRect r = pdf.GetTabRegion(0, 4);
            pdf.WriteLine(String.Format("Occurred {0} after fault", Format_Milliseconds_Time(init.MillisecondsAfterPriorFault)), r, pdf.Brush_Green, PDF_Helper.XPA_CenterTop);
            pdf.GotoNextLine(5);

            if (Report_Type == G2_Report_Types.Advanced)
            {
                pdf.WriteLine(0, "Pitch");
                pdf.WriteLine(1, String.Format("{0:F4} (degrees)", (Int16)init.Pitch / CU_Log_Statics.PITCH_CT_PER_PITCH_DEG));
                pdf.WriteLine(2, "Pitch Rate");
                pdf.WriteLine(3, String.Format("{0:F4} (deg/sec)", (Int16)init.PitchRate / CU_Log_Statics.RATE_CT_PER_RATE_DEGPERSEC));
                pdf.GotoNextLine();

                Write_WheelSpeeds(pdf, init, log);
                pdf.GotoNextLine();

                Write_Faults(pdf, init, false);
            }
            else
            {
                Process_Faults(pdf, init.Hazards, new G2_Hazard_Faults(), "Hazard Faults");
                Process_Faults(pdf, (UInt16)(init.Fault_Comm & CU_Log_Statics.LOCAL_COMM_FAULTS), new G2_Comm_Faults(), "Comm Faults");
                //Process_Faults(pdf, init.Fault_Comm, new G2_Comm_Faults(), "Comm Faults");
                Process_Faults(pdf, init.Fault_Sensor_Local, new G2_Sensor_Faults_Local(), "Sensor Faults");

                if ((init.GP_STS1 == 0xDECA) && (init.GP_STS2 == 0xFC0F) && (init.GP_STS3 == 0xFEEB) && (init.GP_STS4 == 0xEBAD))
                {
                    Process_Faults(pdf, init.Fault_Actuator_Local, new G2_Alternate_Actuator_Faults(), "Actuator Faults");
                }
                else
                {
                    Process_Faults(pdf, init.Fault_Actuator_Local, new G2_Actuator_Faults_Local(), "Actuator Faults");
                }
            }

            if (Report_Type == G2_Report_Types.Basic)
            {
                Decode_Basic_Report(pdf, new G2Log_Fault(init), log.CU_Side);
            }
            else if (Report_Type == G2_Report_Types.Advanced)
            {
                Decode_Advanced_Report(pdf, log, new G2Log_Fault(init), log.CU_Side);
            }
        }


        private Double NewPage(PDF_Helper pdf)
        {
            if (pdf.AddLetterPage(Header1, Header2, Header3, headerHeight: 56, content: (Header_Content)3) == true) Write_Footnote(pdf);
            pdf.GoToTopOfPage();
            return pdf.CurrentPoint.Y;
        }

        private void Process_Faults<T>(PDF_Helper pdf, UInt16 fault, G2_Fault_Base<T> faultbase, String header, Boolean bold = false, G2_CU_Location location = G2_CU_Location.NotApplicable)
        {
            logger.Debug("Fault: 0x{0:X4}, Type: {1}", fault, faultbase.GetType());
            if (fault == 0) return;
            Boolean headerPrinted = false;
            XFont headerfont = pdf.Header_Font_3;
            XFont normalfont = pdf.CurrentFont;
            int TabPoint = location == G2_CU_Location.Remote ? 2 : 0;
            if (bold == true)
            {
                normalfont = headerfont = new XFont(headerfont.FontFamily.Name, headerfont.Size, XFontStyle.Bold);
                //normalfont = new XFont(headerfont.FontFamily.Name, headerfont.Size, XFontStyle.Bold);
            }

            foreach (T e in faultbase.Keys)
            {
                if (faultbase.IsSet(e, fault) == true)
                {
                    G2_Fault_Definition def = faultbase.FaultList[e];
                    if (IsLocationMatch(def.Location, location) == false) continue;

                    XBrush brush = null;
                    switch (def.Severe)
                    {
                        case SeverityColor.Blue:
                            brush = pdf.Brush_Blue;
                            break;
                        case SeverityColor.Red:
                            brush = pdf.Brush_Red;
                            break;
                        default:
                            brush = pdf.Brush_Black;
                            break;
                    }

                    if (headerPrinted == false)
                    {
                        pdf.GotoNextLine(pdf.CurrentFont.Height / -2);
                        pdf.WriteLine(TabPoint, header, font: headerfont);
                        headerPrinted = true;
                    }
                    pdf.WriteLine(def.Name, TabPoint + 1, textColor: brush, font: normalfont);
                    pdf.GotoNextLine();
                }
            }
        }

        void Decode_Advanced_Report(PDF_Helper pdf, SART_CU_Logs log, G2Log_Fault fault, CAN_CU_Sides side)
        {
            // XRect r = default(XRect);
            double offset = XUnit.FromInch(1);
            Boolean printedTSW = false;

            Boolean printGP = true;
            ////////////////////////////////////////////////////////////////////////
            /// General Purpose Words

            pdf.GotoNextLine(pdf.CurrentFont.Height / -2);
            pdf.WriteLine(0, "General Purpose Status", font: pdf.Header_Font_3_Bold);
            pdf.WriteLine(1, String.Format("{0:X4} {1:X4} {2:X4} {3:X4}", fault.GP_STS1, fault.GP_STS2, fault.GP_STS3, fault.GP_STS4), font: pdf.Header_Font_3);
            pdf.GotoNextLine();

            if ((fault.GP_STS1 == 0xDECA) && (fault.GP_STS2 == 0xFC0F) && (fault.GP_STS3 == 0xFEEB) && (fault.GP_STS4 == 0xEBAD))
            {
                Write_GP(pdf, "Stack Fault Mapped to Frame Fault", ref printGP, fault);
            }
            else if ((fault.GP_STS1 == 0xAAAA) && (fault.GP_STS2 == 0xBBBB) && (fault.GP_STS3 == 0xCCCC) && (fault.GP_STS4 == 0x2152))
            {
                Write_GP(pdf, "App Overrun Fault Mapped to Training Decel", ref printGP, fault);
            }
            else if ((fault.Hazards == 0) && (fault.Fault_Comm != 0) && (fault.Fault_Sensor_Local == 0) && (fault.Fault_Sensor_Remote == 0) && (fault.Fault_Actuator_Local == 0))
            {
                if ((fault.Fault_Comm & CU_Log_Statics.TK_MGR_MODE_SYNC_TIMEOUT_FAULT) == 0)
                {
                    // all toolkits initialized!
                    if ((fault.GP_STS1 & 0xFF) == 0xFF)
                    {
                        List<G2_General_Purpose_Test> tests = new List<G2_General_Purpose_Test>()
                        {
                            new G2_General_Purpose_Test(1, (UInt16)CU_Log_Statics.CU_UI, false, "Initialization CU-UI Bus Fault"),
                            new G2_General_Purpose_Test(1, (UInt16)CU_Log_Statics.CU_CU, false, "Initialization CU-CU Bus Fault"),
                            new G2_General_Purpose_Test(1, (UInt16)CU_Log_Statics.CU_IMU, false, "Initialization CU-BSA Bus Fault"),
                            new G2_General_Purpose_Test(1, (UInt16)CU_Log_Statics.CU_BCU, false, "Initialization CU-BCU Bus Fault")
                        };

                        foreach (var test in tests)
                        {
                            if (test.Test(fault) == true)
                            {
                                Write_GP(pdf, ref printGP, fault, test);
                            }
                        }

                        //if ((fault.GP_STS1 & CU_Log_Statics.CU_UI) != CU_Log_Statics.CU_UI)
                        //{
                        //    Write_GP(pdf, "Initialization CU-UI Bus Fault", ref printGP, fault);
                        //}

                        //if ((fault.GP_STS1 & CU_Log_Statics.CU_CU) != CU_Log_Statics.CU_CU)
                        //{
                        //    Write_GP(pdf, "Initialization CU-CU Bus Fault", ref printGP, fault);
                        //}

                        //if ((fault.GP_STS1 & CU_Log_Statics.CU_IMU) != CU_Log_Statics.CU_IMU)
                        //{
                        //    Write_GP(pdf, "Initialization CU-BSA Bus Fault", ref printGP, fault);
                        //}

                        //if ((fault.GP_STS1 & CU_Log_Statics.CU_BCU) != CU_Log_Statics.CU_BCU)
                        //{
                        //    Write_GP(pdf, "Initialization CU-BCU Bus Fault", ref printGP, fault);
                        //}


                        if ((fault.Fault_Comm & CU_Log_Statics.TK_MGR_TDM_SLOT_FAULT) != 0)
                        {
                            Write_GP(pdf, String.Format("Bad Slot Count {0}", fault.GP_STS4), ref printGP, fault);
                        }
                    }
                }
            }

            // If a critical hazard, battery status is put in gp_status_3.
            // An actuator fault could overwrite.

            // hazard and no actuator faults
            if ((fault.Hazards != 0) && (fault.Fault_Actuator_Local == 0) && (fault.Fault_Actuator_Remote == 0))
            {
                List<G2_General_Purpose_Test> tests = new List<G2_General_Purpose_Test>()
                        {
                            new G2_General_Purpose_Test(3, (UInt16)0x0200, true, "Battery Cold Charge Limit"),
                            new G2_General_Purpose_Test(3, (UInt16)0x0400, true, "Battery Cold"),
                            new G2_General_Purpose_Test(3, (UInt16)0x0800, true, "Battery Cool"),
                            new G2_General_Purpose_Test(3, (UInt16)0x1000, true, "Battery Over Voltage"),
                            new G2_General_Purpose_Test(3, (UInt16)0x2000, true, "Low Block Battery Voltage"),
                            new G2_General_Purpose_Test(3, (UInt16)0x4000, true, "Battery Hot"),
                            new G2_General_Purpose_Test(3, (UInt16)0x8000, true, "Battery Warm"),
                            new G2_General_Purpose_Test(3, (UInt16)0x00AA, true, "Low Battery Power Estimate"),
                        };

                foreach (var test in tests)
                {
                    if (test.Test(fault) == true)
                    {
                        Write_GP(pdf, ref printGP, fault, test);
                    }
                }


                //if ((fault.GP_STS3 & 0x0200) != 0)
                //{
                //    Write_GP(pdf, "Battery Cold Charge Limit", ref printGP, fault);
                //}
                //if ((fault.GP_STS3 & 0x0400) != 0)
                //{
                //    Write_GP(pdf, "Battery Cold", ref printGP, fault);
                //}
                //if ((fault.GP_STS3 & 0x0800) != 0)
                //{
                //    Write_GP(pdf, "Battery Cool", ref printGP, fault);
                //}
                //if ((fault.GP_STS3 & 0x1000) != 0)
                //{
                //    Write_GP(pdf, "Battery Over Voltage", ref printGP, fault);
                //}
                //if ((fault.GP_STS3 & 0x2000) != 0)
                //{
                //    Write_GP(pdf, "Low Block Battery Voltage", ref printGP, fault);
                //}
                //if ((fault.GP_STS3 & 0x4000) != 0)
                //{
                //    Write_GP(pdf, "Battery Hot", ref printGP, fault);
                //}
                //if ((fault.GP_STS3 & 0x8000) != 0)
                //{
                //    Write_GP(pdf, "Battery Warm", ref printGP, fault);
                //}
                //if ((fault.GP_STS3 & 0x00AA) == 0x00AA)
                //{
                //    Write_GP(pdf, "Low Battery Power Estimate", ref printGP, fault);
                //}
            }

            // actuator fault, and no hazards or sensor faults
            if ((fault.Hazards == 0) && (fault.Fault_Sensor_Local == 0) && (fault.Fault_Sensor_Remote == 0) && (fault.Fault_Actuator_Local != 0))
            {
                if ((fault.Fault_Actuator_Local & CU_Log_Statics.TK_MGR_BATTERY_FAULT) != 0)	 // local battery fault
                {
                    List<G2_General_Purpose_Test> tests = new List<G2_General_Purpose_Test>()
                        {
                            new G2_General_Purpose_Test(3, (UInt16)0x8000, true, "Battery Charging Failure"),
                            new G2_General_Purpose_Test(3, (UInt16)0x4000, true, "Battery Charging Failure"),
                            new G2_General_Purpose_Test(3, (UInt16)0x2000, true, "Battery Discharge Failure (invalid temperature)"),
                            new G2_General_Purpose_Test(3, (UInt16)0x1000, true, "Battery Discharge Failure (invalid voltage)"),
                            new G2_General_Purpose_Test(3, (UInt16)0x0800, true, "Battery Discharge Failure (invalid current)"),
                            new G2_General_Purpose_Test(3, (UInt16)0x0400, true, "Battery EEprom Checksum Error"),
                            new G2_General_Purpose_Test(3, (UInt16)0x0200, true, "Battery Software Incompatibility Error"),
                            new G2_General_Purpose_Test(3, (UInt16)0x0080, true, "Battery Program Checksum Error"),
                            new G2_General_Purpose_Test(3, (UInt16)0x0040, true, "Battery General Hardware Error"),
                            new G2_General_Purpose_Test(3, (UInt16)0x0020, true, "Battery High Current Discharge Not Ready"),
                        };

                    foreach (var test in tests)
                    {
                        if (test.Test(fault) == true)
                        {
                            Write_GP(pdf, ref printGP, fault, test);
                        }
                    }

                    //if ((fault.GP_STS3 & 0xC000) != 0)
                    //{
                    //    Write_GP(pdf, "Battery Charging Failure", ref printGP, fault);
                    //}
                    //if ((fault.GP_STS3 & 0x2000) != 0)
                    //{
                    //    Write_GP(pdf, "Battery Discharge Failure (invalid temperature)", ref printGP, fault);
                    //}
                    //if ((fault.GP_STS3 & 0x1000) != 0)
                    //{
                    //    Write_GP(pdf, "Battery Discharge Failure (invalid voltage)", ref printGP, fault);
                    //}
                    //if ((fault.GP_STS3 & 0x0800) != 0)
                    //{
                    //    Write_GP(pdf, "Battery Discharge Failure (invalid current)", ref printGP, fault);
                    //}
                    //if ((fault.GP_STS3 & 0x0400) != 0)
                    //{
                    //    Write_GP(pdf, "Battery EEprom Checksum Error", ref printGP, fault);
                    //}
                    //if ((fault.GP_STS3 & 0x0200) != 0)
                    //{
                    //    Write_GP(pdf, "Battery Software Incompatibility Error", ref printGP, fault);
                    //}
                    //if ((fault.GP_STS3 & 0x0080) != 0)
                    //{
                    //    Write_GP(pdf, "Battery Program Checksum Error", ref printGP, fault);
                    //}
                    //if ((fault.GP_STS3 & 0x0040) != 0)
                    //{
                    //    Write_GP(pdf, "Battery General Hardware Error", ref printGP, fault);
                    //}
                    //if ((fault.GP_STS3 & 0x0020) != 0)
                    //{
                    //    Write_GP(pdf, "Battery High Current Discharge Not Ready", ref printGP, fault);
                    //}
                }


                report_actuator(pdf, fault, side, ref printGP);


                if ((fault.Fault_Actuator_Local & CU_Log_Statics.ANY_MOTOR_DRIVE_FAULT) != 0)
                {
                    List<G2_General_Purpose_Test> tests = new List<G2_General_Purpose_Test>()
                        {
                            new G2_General_Purpose_Test(1, (UInt16)CU_Log_Statics.HALL_SUM_OR_RAIL_FAULT, true, "Hall Sum Or Rail Fault"),
                            new G2_General_Purpose_Test(1, (UInt16)CU_Log_Statics.HALL_R_MAGNITUDE_FAULT, true, "Hall R Magnitude Fault"),
                            new G2_General_Purpose_Test(1, (UInt16)CU_Log_Statics.HALL_MUX_FAULT, true, "Hall Mux Fault"),
                        };

                    foreach (var test in tests)
                    {
                        if (test.Test(fault) == true)
                        {
                            Write_GP(pdf, ref printGP, fault, test);
                        }
                    }

                    //if ((fault.GP_STS1 & CU_Log_Statics.HALL_SUM_OR_RAIL_FAULT) != 0)
                    //{
                    //    Write_GP(pdf, "Hall Sum Or Rail Fault", ref printGP, fault);
                    //}

                    //if ((fault.GP_STS1 & CU_Log_Statics.HALL_R_MAGNITUDE_FAULT) != 0)
                    //{
                    //    Write_GP(pdf, "Hall R Magnitude Fault", ref printGP, fault);
                    //}

                    //if ((fault.GP_STS1 & CU_Log_Statics.HALL_MUX_FAULT) != 0)
                    //{
                    //    Write_GP(pdf, "Hall Mux Fault", ref printGP, fault);
                    //}
                }


                else if ((fault.Fault_Actuator_Local & CU_Log_Statics.TK_MGR_ACTUATOR_DEGRADED_FAULT) != 0)
                {
                    List<G2_General_Purpose_Test> tests = new List<G2_General_Purpose_Test>()
                        {
                            new G2_General_Purpose_Test(1, (UInt16)CU_Log_Statics.HALL_SUM_DEGRADED, true, "Hall Sum Degraded Fault"),
                            new G2_General_Purpose_Test(1, (UInt16)CU_Log_Statics.HALL_R_MAGNITUDE_DEGRADED, true, "Hall R Magnitude Degraded Fault"),
                        };

                    foreach (var test in tests)
                    {
                        if (test.Test(fault) == true)
                        {
                            Write_GP(pdf, ref printGP, fault, test);
                        }
                    }

                    //if ((fault.GP_STS1 & CU_Log_Statics.HALL_SUM_DEGRADED) != 0)
                    //{
                    //    Write_GP(pdf, "Hall Sum Degraded Fault", ref printGP, fault);
                    //}

                    //if ((fault.GP_STS1 & CU_Log_Statics.HALL_R_MAGNITUDE_DEGRADED) != 0)
                    //{
                    //    Write_GP(pdf, "Hall R Magnitude Degraded Fault", ref printGP, fault);
                    //}
                }
            }
            //    if (printGP == false) pdf.GotoNextLine();

            // If a critical hazard, battery voltage is put in gp_status_1 and
            // speed limit in gp_status4....nothing can overwrite since the hazards
            // are put in last in the embedded code.
            if (fault.Hazards != 0)
            {
                pdf.GotoNextLine(pdf.CurrentFont.Height / -2);

                try
                {
                    switch (int.Parse(log.SW_Build.Substring(0, 4).Trim(), NumberStyles.Integer))
                    {
                        // I2
                        case 1275:
                        case 1351:
                            Write_I2_SpeedLimit(pdf, fault);
                            pdf.WriteLine(2, "Battery Voltage");
                            pdf.WriteLine(3, String.Format("{0:F2} (Voc)", (float)((Int16)(fault.GP_STS1)) / 4));
                            pdf.GotoNextLine();
                            break;

                        // X2
                        case 1352:
                        case 1281:
                            Write_X2_SpeedLimit(pdf, fault);
                            pdf.WriteLine(2, "Battery Voltage");
                            pdf.WriteLine(3, String.Format("{0:F2} (Voc)", (float)((Int16)(fault.GP_STS1)) / 4));
                            pdf.GotoNextLine();
                            break;

                        default:
                            Write_I2_SpeedLimit(pdf, fault);
                            pdf.WriteLine(2, "Battery Voltage");
                            pdf.WriteLine(3, String.Format("{0:F2} (Voc)", (float)((Int16)(fault.GP_STS1)) / 4));
                            pdf.GotoNextLine();
                            Write_X2_SpeedLimit(pdf, fault);
                            pdf.GotoNextLine();
                            break;
                    }
                }
                catch
                {
                    Write_I2_SpeedLimit(pdf, fault);
                    pdf.WriteLine(2, "Battery Voltage");
                    pdf.WriteLine(3, String.Format("{0:F2} (Voc)", (float)((Int16)(fault.GP_STS1)) / 4));
                    pdf.GotoNextLine();
                    Write_X2_SpeedLimit(pdf, fault);
                    pdf.GotoNextLine();
                }

            }

            // If a critical hazard, desired pitch is put in gp_status_2.
            // However, an actuator fault could overwrite, so only decode if no
            // actuator faults.
            if ((fault.Hazards != 0) && (fault.Fault_Actuator_Local == 0) && (fault.Fault_Actuator_Remote == 0))
            {
                pdf.WriteLine(0, "Desired Pitch");
                pdf.WriteLine(1, String.Format("~ {0:F4} (degrees)", (float)((Int16)fault.GP_STS2) / CU_Log_Statics.PITCH_CT_PER_PITCH_DEG));
                pdf.GotoNextLine();
            }

            /// General Purpose Words
            ////////////////////////////////////////////////////////////////////////


            //if (printed == false)
            //{
            pdf.GotoNextLine(pdf.CurrentFont.Height / -2);
            pdf.WriteLine(0, "Test Status Words", font: pdf.Header_Font_3);
            pdf.WriteLine(1, String.Format("{0:X4} {1:X4} {2:X4}", fault.TSW1, fault.TSW2, fault.TSW3), font: pdf.Header_Font_3);
            pdf.GotoNextLine();
            //}

            ////////////////////////////////////////////////////////////////////////
            /// Test Status Word 1 (TSW-1)
            if ((fault.TSW1 & 0x0001) != 0)
            {
                Write_TSW_Bits(pdf, "0001 0000 0000");
                Write_TSW(pdf, "Dual Stator Decel Response", ref printedTSW);
            }

            if ((fault.TSW1 & 0x0004) != 0)
            {
                Write_TSW_Bits(pdf, "0004 0000 0000");
                Write_TSW(pdf, "Single Stator Decel Response", ref printedTSW);
            }

            if ((fault.TSW1 & 0x0008) != 0)
            {
                Write_TSW_Bits(pdf, "0008 0000 0000");
                Write_TSW(pdf, "Disable Motor Response", ref printedTSW);
            }

            Write_TSW_Bits(pdf, "0080 0000 0000");
            Write_TSW(pdf, String.Format("Motor Power {0}", ((fault.TSW1 & 0x0080) != 0) ? "On" : "Off"), ref printedTSW);

            if ((fault.TSW1 & 0x0100) != 0)
            {
                Write_TSW_Bits(pdf, "0100 0000 0000");
                Write_TSW(pdf, "Balance Mode", ref printedTSW);
            }
            else if ((fault.TSW1 & 0x0080) != 0)
            {
                Write_TSW_Bits(pdf, "0080 0000 0000");
                Write_TSW(pdf, "Follow Mode", ref printedTSW);
            }
            else if ((fault.TSW2 & 0x1800) != 0)
            {
                Write_TSW_Bits(pdf, "0000 1800 0000");
                Write_TSW(pdf, "Disable Mode", ref printedTSW);
            }
            else
            {
                Write_TSW_Bits(pdf, "0000 1800 0000");
                Write_TSW(pdf, "Transition or Disable Mode (Most likely Disable Mode)", ref printedTSW);
            }

            if ((fault.TSW1 & 0x8000) != 0)
            {
                Write_TSW_Bits(pdf, "8000 0000 0000");
                Write_TSW(pdf, "PSE 1-Axis Mode", ref printedTSW);
            }

            if ((fault.TSW1 & 0x2000) != 0)
            {
                Write_TSW_Bits(pdf, "2000 0000 0000");
                Write_TSW(pdf, "Using Local Inertial Data", ref printedTSW);
            }

            if ((fault.TSW1 & 0x4000) != 0)
            {
                Write_TSW_Bits(pdf, "4000 0000 0000");
                Write_TSW(pdf, "Using Remote Inertial Data", ref printedTSW);
            }
            /// Test Status Word 1 (TSW-1)
            ////////////////////////////////////////////////////////////////////////


            ////////////////////////////////////////////////////////////////////////
            /// Test Status Word 2 (TSW-2)
            if ((fault.TSW2 & 0x0001) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0001 0000");
                Write_TSW(pdf, "Using Local Yaw Data", ref printedTSW);
            }

            if ((fault.TSW2 & 0x0002) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0002 0000");
                Write_TSW(pdf, "Using Remote Yaw Data", ref printedTSW);
            }

            if ((fault.TSW2 & 0x0010) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0010 0000");
                Write_TSW(pdf, "Yaw Slewed to Zero", ref printedTSW);
            }

            if ((fault.TSW2 & 0x0020) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0020 0000");
                Write_TSW(pdf, "BSA Side A ARS Fault", ref printedTSW);
            }

            if ((fault.TSW2 & 0x0040) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0040 0000");
                Write_TSW(pdf, "BSA Side B ARS Fault", ref printedTSW);
            }

            if ((fault.TSW2 & 0x0080) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0080 0000");
                Write_TSW(pdf, "BSA Tilt Sensor Fault", ref printedTSW);
            }

            //if ((fault.TSW2 & 0x8000) != 0)
            //{
            Write_TSW_Bits(pdf, "0000 E000 0000");
            Write_TSW(pdf, String.Format("{0} Rider Detect(s) Active", (fault.TSW2 >> 13) & 0x7), ref printedTSW);
            //}
            /// Test Status Word 2 (TSW-2)
            ////////////////////////////////////////////////////////////////////////

            ////////////////////////////////////////////////////////////////////////
            /// Test Status Word 3 (TSW-3)

            if ((fault.TSW3 & 0x0001) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0000 0001");
                Write_TSW(pdf, "Inertial Yaw Data Active", ref printedTSW);
            }

            if ((fault.TSW3 & 0x0002) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0000 0002");
                Write_TSW(pdf, "Limit Speed Response for Low Battery Active", ref printedTSW);
            }

            if ((fault.TSW3 & 0x0004) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0000 0004");
                Write_TSW(pdf, "Riderless Balance Gains Active", ref printedTSW);
            }

            if ((fault.TSW3 & 0x0008) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0000 0008");
                Write_TSW(pdf, "Traction Control Recently Active", ref printedTSW);
            }

            if ((fault.TSW3 & 0x0010) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0000 0010");
                Write_TSW(pdf, "Stuck Hall Mux Fault", ref printedTSW);
            }

            if ((fault.TSW3 & 0x0C00) == 0x0C00)
            {
                Write_TSW_Bits(pdf, "0000 0000 0C00");
                Write_TSW(pdf, "Lithium Battery Chemistry", ref printedTSW);
            }

            else if ((fault.TSW3 & 0x0400) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0000 0400");
                Write_TSW(pdf, "NiCad Battery Chemistry", ref printedTSW);
            }

            else if ((fault.TSW3 & 0x0800) != 0)
            {
                Write_TSW_Bits(pdf, "0000 0000 0800");
                Write_TSW(pdf, "NiMH Battery Chemistry", ref printedTSW);
            }
            /// Test Status Word 3 (TSW-3)
            ////////////////////////////////////////////////////////////////////////

        }

        private static void Write_X2_SpeedLimit(PDF_Helper pdf, G2Log_Fault fault)
        {
            pdf.WriteLine(0, "X2 Current Speed Limit");
            Conversion.Miles = (Double)(((Int16)(fault.GP_STS4)) / CU_Log_Statics.X_WHEEL_COUNTS_TO_MPH);
            if (Application_Helper.IsMetric) pdf.WriteLine(1, String.Format("~ {0:F4} (km/h)", Conversion.Kilometers));
            else pdf.WriteLine(1, String.Format("~ {0:F4} (mph)", Conversion.Miles));
        }

        private static void Write_I2_SpeedLimit(PDF_Helper pdf, G2Log_Fault fault)
        {
            pdf.WriteLine(0, "I2 Current Speed Limit");
            Conversion.Miles = (Double)(((Int16)(fault.GP_STS4)) / CU_Log_Statics.I_WHEEL_COUNTS_TO_MPH);
            if (Application_Helper.IsMetric) pdf.WriteLine(1, String.Format("~ {0:F4} (km/h)", Conversion.Kilometers));
            else pdf.WriteLine(1, String.Format("~ {0:F4} (mph)", Conversion.Miles));
        }

        void Decode_Basic_Report(PDF_Helper pdf, G2Log_Fault fault, CAN_CU_Sides side)
        {
            //pdf.WriteLine("Basic Decoding", pdf.GetTabRegion(0, 4), paraformat: PDF_Helper.XPA_CenterMiddle, font: pdf.Header_Font_2);
            //pdf.GotoNextLine(5);

            Boolean printed = false;
            // no other faults but comm
            if ((fault.Hazards == 0) && (fault.Fault_Comm != 0) && (fault.Fault_Sensor_Local == 0) &&
                (fault.Fault_Sensor_Remote == 0) && (fault.Fault_Actuator_Local == 0))
            {
                // all toolkits initialized!
                if ((fault.GP_STS1 & 0xFF) == 0xFF)
                {
                    if ((fault.GP_STS1 & CU_Log_Statics.CU_UI) != CU_Log_Statics.CU_UI)
                    {
                        Write_GP(pdf, "Initialization CU-UI Bus Fault", ref printed, fault);
                    }

                    if ((fault.GP_STS1 & CU_Log_Statics.CU_CU) != CU_Log_Statics.CU_CU)
                    {
                        Write_GP(pdf, "Initialization CU-CU Bus Fault", ref printed, fault);
                    }

                    if ((fault.GP_STS1 & CU_Log_Statics.CU_IMU) != CU_Log_Statics.CU_IMU)
                    {
                        Write_GP(pdf, "Initialization CU-BSA Bus Fault", ref printed, fault);
                    }

                    if ((fault.GP_STS1 & CU_Log_Statics.CU_BCU) != CU_Log_Statics.CU_BCU)
                    {
                        Write_GP(pdf, "Initialization CU-BCU Bus Fault", ref printed, fault);
                    }
                }
            }

            if ((fault.TSW3 & 0x0C00) == 0x0C00)
            {
                Write_GP(pdf, "Lithium Battery Chemistry", ref printed, fault);
            }
            else if ((fault.TSW3 & 0x0400) == 0x0400)
            {
                Write_GP(pdf, "NiCad Battery Chemistry", ref printed, fault);
            }
            else if ((fault.TSW3 & 0x0800) == 0x0800)
            {
                Write_GP(pdf, "NiMH Battery Chemistry", ref printed, fault);
            }

            // If a critical hazard, battery status is put in fault.GP_STS3.
            // An actuator fault could overwrite.

            // hazard and no actuator faults
            if ((fault.Hazards != 0) && (fault.Fault_Actuator_Local == 0) && (fault.Fault_Actuator_Remote == 0))
            {
                if ((fault.GP_STS3 & 0x0200) == 0x0200)
                {
                    Write_GP(pdf, "Battery Cold Charge Limit", ref printed, fault);
                }
                if ((fault.GP_STS3 & 0x0400) == 0x0400)
                {
                    Write_GP(pdf, "Battery Cold", ref printed, fault);
                }
                if ((fault.GP_STS3 & 0x800) == 0x800)
                {
                    Write_GP(pdf, "Battery Cool", ref printed, fault);
                }
                if ((fault.GP_STS3 & 0x1000) == 0x1000)
                {
                    Write_GP(pdf, "Battery Over Voltage", ref printed, fault);
                }
                if ((fault.GP_STS3 & 0x2000) == 0x2000)
                {
                    Write_GP(pdf, "Low Block Battery Voltage", ref printed, fault);
                }
                if ((fault.GP_STS3 & 0x4000) == 0x4000)
                {
                    Write_GP(pdf, "Battery Hot", ref printed, fault);
                }
                if ((fault.GP_STS3 & 0x8000) == 0x8000)
                {
                    Write_GP(pdf, "Battery Warm", ref printed, fault);
                }
                if ((fault.GP_STS3 & 0x00AA) == 0x00AA)
                {
                    Write_GP(pdf, "Low Battery Power Estimate", ref printed, fault);
                }
            }


            // actuator fault, and no hazards or sensor faults
            if ((fault.Hazards == 0) && (fault.Fault_Sensor_Local == 0) && (fault.Fault_Sensor_Remote == 0) && (fault.Fault_Actuator_Local != 0))
            {
                if ((fault.Fault_Actuator_Local & CU_Log_Statics.BATTERY_FAULT) == CU_Log_Statics.BATTERY_FAULT)      // local battery fault
                {
                    if ((fault.GP_STS3 & 0x0200) == 0x0200)
                    {
                        Write_GP(pdf, "Battery Software Incompatibility Error", ref printed, fault);
                    }
                }

                report_actuator(pdf, fault, side, ref printed);
            }

            // If a critical hazard, battery voltage is put in gp_status_1 and
            // speed limit in gp_status4....nothing can overwrite since the hazards
            // are put in last in the embedded code.
            if (fault.Hazards != 0)
            {
                pdf.GotoNextLine(pdf.CurrentFont.Height / -2);
                pdf.WriteLine(0, "Battery Voltage");
                pdf.WriteLine(1, String.Format("~ {0:F2} (Voc)", ((float)fault.GP_STS1) / 4.0));
                pdf.GotoNextLine();
            }
        }


        void report_actuator(PDF_Helper pdf, G2Log_Fault fault, CAN_CU_Sides side_id, ref Boolean printed)
        {
            Boolean localprinted = false;
            if ((fault.Fault_Actuator_Local & CU_Log_Statics.ANY_MOTOR_DRIVE_FAULT) != 0)
            {
                if ((fault.GP_STS2 & CU_Log_Statics.LEFT_AMP_FAULT) != 0)
                {
                    Write_GP(pdf, String.Format("(Left Side Fault) ({0:X4})", fault.GP_STS2), ref printed, fault);
                    localprinted = true;
                }
                if ((fault.GP_STS3 & CU_Log_Statics.RIGHT_AMP_FAULT) != 0)
                {
                    Write_GP(pdf, String.Format("(Right Side Fault) ({0:X4})", fault.GP_STS3), ref printed, fault);
                    localprinted = true;
                }
            }

            else if ((fault.Fault_Actuator_Local & CU_Log_Statics.TK_MGR_MOTOR_VOLTAGE_CONSISTENCY_FAULT) == CU_Log_Statics.TK_MGR_MOTOR_VOLTAGE_CONSISTENCY_FAULT)
            {
                if (side_id == CAN_CU_Sides.A)
                {
                    if ((fault.GP_STS2 & CU_Log_Statics.AMP1_TEST) != 0)
                    {
                        Write_GP(pdf, String.Format("(Left Side Fault)", fault.GP_STS2), ref printed, fault);
                        localprinted = true;
                    }
                    if ((fault.GP_STS2 & CU_Log_Statics.AMP2_TEST) != 0)
                    {
                        Write_GP(pdf, String.Format("(Right Side Fault)", fault.GP_STS2), ref printed, fault);
                        localprinted = true;
                    }
                }

                else
                {
                    if ((fault.GP_STS2 & CU_Log_Statics.AMP1_TEST) != 0)
                    {
                        Write_GP(pdf, String.Format("(Right Side Fault)", fault.GP_STS2), ref printed, fault);
                        localprinted = true;
                    }
                    if ((fault.GP_STS2 & CU_Log_Statics.AMP2_TEST) != 0)
                    {
                        Write_GP(pdf, String.Format("(Left Side Fault)", fault.GP_STS2), ref printed, fault);
                        localprinted = true;
                    }
                }
            }

            if (localprinted) pdf.GotoNextLine(pdf.CurrentFont.Height / -2);
        }

        private void Write_Header_Hazard_Info(PDF_Helper pdf, G2_Report_Types Report_Type)
        {
            if (pdf == null) throw new ArgumentNullException("Parameter pdf (PDF_Helper) can not be null");
            if (LogArray == null) throw new ArgumentNullException("Log data has not been extracted");
            if (LogArray.Count != 256) throw new ArgumentException("Log data is invalid");


            try
            {
                logger.Trace("Entered");

                int Bus_Voltage = LogArray[CU_Log_Statics.HEADER_BUS_VOLTAGE];
                //if (Bus_Voltage != 0)
                //{
                pdf.WriteLine(0, "Bus Voltage");
                pdf.WriteLine(String.Format("~ {0:F4}  (vbus) (At most recent Critical Hazard)", Bus_Voltage / 4.0), pdf.GetTabRegion(1, 3));
                pdf.GotoNextLine();
                //}
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
            finally
            {
                logger.Trace("Leaving");
            }

#if RemoveThis
            if (faultArray.Count != 0)
            {
                if (CU_Log_Statics.Faults_Contain_Hazard(faultArray) == true)
                {
                    if (global_legacy_header_flag == true)
                    {

                    }
                    else
                    {
                        pdf.WriteLine(0, "Battery Resistance");
                        pdf.WriteLine(String.Format("~ {0:F4} (rbat ohms) (At most recent Critical Hazard)", Battery_Resistance / 1024.0), pdf.GetTabRegion(1, 3));
                        pdf.GotoNextLine();

                        if (Report_Type == G2_Report_Types.Advanced)
                        {
                            pdf.WriteLine(0, "Bus Voltage");
                            pdf.WriteLine(String.Format("~ {0:F4}  (vbus) (At most recent Critical Hazard)", Bus_Voltage / 4.0), pdf.GetTabRegion(1, 3));
                            pdf.GotoNextLine();
                        }
                    }
                }
            }
#endif
        }

        private void Write_GP(PDF_Helper pdf, String str, ref Boolean printed, G2Log_Fault fault)
        {
            //if (printed == false)
            //{
            //    pdf.GotoNextLine(pdf.CurrentFont.Height / -2);
            //    pdf.WriteLine(0, "General Purpose Status", font: pdf.Header_Font_3_Bold);
            //    pdf.WriteLine(1, String.Format("{0:X4} {1:X4} {2:X4} {3:X4}", fault.GP_STS1, fault.GP_STS2, fault.GP_STS3, fault.GP_STS4), font: pdf.Header_Font_3);
            //    pdf.GotoNextLine();
            //    printed = true;
            //}
            XRect r = pdf.GetTabRegion(1, 2);
            pdf.WriteLine(str, r, font: pdf.CurrentFontBold);
            pdf.GotoNextLine();
        }

        private void Write_GP(PDF_Helper pdf, ref Boolean printed, G2Log_Fault fault, G2_General_Purpose_Test test)
        {
            //if (printed == false)
            //{
            //    pdf.GotoNextLine(pdf.CurrentFont.Height / -2);
            //    pdf.WriteLine(0, "General Purpose Status", font: pdf.Header_Font_3_Bold);
            //    pdf.WriteLine(1, String.Format("{0:X4} {1:X4} {2:X4} {3:X4}", fault.GP_STS1, fault.GP_STS2, fault.GP_STS3, fault.GP_STS4), font: pdf.Header_Font_3);
            //    pdf.GotoNextLine();
            //    printed = true;
            //}
            if (test != null) Write_GP_Mask(pdf, test);
            Write_GP(pdf, test.Description, ref printed, fault);
        }

        private void Write_GP_Mask(PDF_Helper pdf, G2_General_Purpose_Test test)
        {
            StringBuilder sb = new StringBuilder();
            for (int x = 1; x <= 4; x++)
            {
                sb.AppendFormat("{0:X4} ", x == test.Word ? test.Mask : 0);
            }
            XRect r = pdf.GetTabRegion(0);
            r.Inflate(-8, 0);
            pdf.WriteLine(r, sb.ToString().Trim(), paraformat: PDF_Helper.XPA_RightMiddle, font: pdf.Small_M2_Font /* new XFont(pdf.CurrentFont.FontFamily.Name, 7)*/);
        }


        private void Write_TSW_Bits(PDF_Helper pdf, string v)
        {
            XRect r = pdf.GetTabRegion(0);
            r.Inflate(-8, 0);
            pdf.WriteLine(r, v, paraformat: PDF_Helper.XPA_RightMiddle, font: pdf.Small_M2_Font);
        }


        private void Write_TSW(PDF_Helper pdf, String str, ref Boolean printed)
        {
            //if (printed == false)
            //{
            //    pdf.GotoNextLine(pdf.CurrentFont.Height / -2);
            //    pdf.WriteLine(0, "Test Status Words", font: pdf.Header_Font_3);
            //}
            XRect r = pdf.GetTabRegion(1, 2);
            pdf.WriteLine(str, r, font: pdf.CurrentFont);
            pdf.GotoNextLine();
            printed = true;
        }

        private Boolean IsLocationMatch(G2_CU_Location faultLocation, G2_CU_Location testlocation)
        {
            if (testlocation == faultLocation) return true;
            if ((testlocation <= G2_CU_Location.Local) && (faultLocation <= G2_CU_Location.Local)) return true;
            return false;
        }

        private void Calculate_Start_Point(PDF_Helper pdf, XPoint savePoint, int pageno)
        {
            if (pageno < pdf.CurrentPageNumber)
            {
                pdf.SelectPage(pageno);
            }
            pdf.CurrentPoint = savePoint;
        }

        private void Position_End_Point(PDF_Helper pdf, int pageno, XPoint LowPoint)
        {
            if (pageno > pdf.CurrentPageNumber)
            {
                pdf.SelectPage(pageno);
                pdf.CurrentPoint = LowPoint;
            }
            else if (pageno == pdf.CurrentPageNumber)
            {
                if (LowPoint.Y > pdf.CurrentPoint.Y)
                {
                    pdf.CurrentPoint = LowPoint;
                }
            }
        }

        private String Format_Date_String(DateTime? ts)
        {
            if ((ts == null) || (ts.HasValue == false)) return "";
            List<String> parts = new List<String>(Strings.Split(ts.Value.ToLongDateString(), ','));
            parts.RemoveAt(0);
            return String.Format("{0} @ {1}", Strings.MergeName(parts, ','), ts.Value.ToLongTimeString()).Trim();
        }


        private void Write_Faults(PDF_Helper pdf, G2Log_Base init, Boolean BoldPrint)
        {
            //////////////////////////////////////////////////////////////
            // Hazards
            XPoint startPoint = pdf.CurrentPoint;
            int startPageNo = pdf.CurrentPageNumber;
            Process_Faults(pdf, init.Hazards, new G2_Hazard_Faults(), "Local Hazards", bold: BoldPrint);
            XPoint LowPoint = pdf.CurrentPoint;
            int pageno = pdf.CurrentPageNumber;

            Calculate_Start_Point(pdf, startPoint, startPageNo);
            Process_Faults(pdf, init.Hazards, new G2_Hazard_Faults(), "Remote Hazards", location: G2_CU_Location.Remote);
            Position_End_Point(pdf, pageno, LowPoint);
            // Hazards
            //////////////////////////////////////////////////////////////


            //////////////////////////////////////////////////////////////
            // Comm Faults
            startPoint = pdf.CurrentPoint;
            startPageNo = pdf.CurrentPageNumber;
            Process_Faults(pdf, init.Fault_Comm, new G2_Comm_Faults(), "Local Comm Faults", bold: BoldPrint);
            LowPoint = pdf.CurrentPoint;
            pageno = pdf.CurrentPageNumber;

            Calculate_Start_Point(pdf, startPoint, startPageNo);
            Process_Faults(pdf, init.Fault_Comm, new G2_Comm_Faults(), "Remote Comm Faults", location: G2_CU_Location.Remote);
            Position_End_Point(pdf, pageno, LowPoint);
            // Comm Faults
            //////////////////////////////////////////////////////////////

            //////////////////////////////////////////////////////////////
            // Sensor Faults
            startPoint = pdf.CurrentPoint;
            startPageNo = pdf.CurrentPageNumber;
            Process_Faults(pdf, init.Fault_Sensor_Local, new G2_Sensor_Faults_Local(), "Local Sensor Faults", bold: BoldPrint);
            LowPoint = pdf.CurrentPoint;
            pageno = pdf.CurrentPageNumber;

            Calculate_Start_Point(pdf, startPoint, startPageNo);
            Process_Faults(pdf, init.Fault_Sensor_Remote, new G2_Sensor_Faults_Remote(), "Remote Sensor Faults", location: G2_CU_Location.Remote);
            Position_End_Point(pdf, pageno, LowPoint);
            // Sensor Faults
            //////////////////////////////////////////////////////////////


            //////////////////////////////////////////////////////////////
            // Actuator Faults
            startPoint = pdf.CurrentPoint;
            startPageNo = pdf.CurrentPageNumber;
            Process_Faults(pdf, init.Fault_Actuator_Local, new G2_Actuator_Faults_Local(), "Local Actuator Faults", bold: BoldPrint);
            LowPoint = pdf.CurrentPoint;
            pageno = pdf.CurrentPageNumber;

            Calculate_Start_Point(pdf, startPoint, startPageNo);
            Process_Faults(pdf, init.Fault_Actuator_Remote, new G2_Actuator_Faults_Remote(), "Remote Actuator Faults", location: G2_CU_Location.Remote);
            Position_End_Point(pdf, pageno, LowPoint);
            // Actuator Faults
            //////////////////////////////////////////////////////////////
        }
    }
}
