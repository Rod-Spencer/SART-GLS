using Microsoft.Practices.ObjectBuilder2;
using NLog;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.PDFHelper;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Services</summary>
    public class Black_Box_Services
    {
        /// <summary>Protected Member - logger</summary>
        protected static Logger logger = Logger_Helper.GetCurrentLogger();
        /// <summary>Public Property - Error_Message</summary>
        public static String Error_Message { get; set; }

        private static Black_Box_Data_Display_List BBDList = new Black_Box_Data_Display_List();


        /// <summary>Public Static Method - Report</summary>
        /// <param name="pdf">PDF_Helper</param>
        /// <param name="bbox">BSA_Black_Box</param>
        /// <param name="token">AuthenticationToken</param>
        public static void Report(PDF_Helper pdf, BSA_Black_Box bbox, AuthenticationToken token)
        {
            pdf.AddCustom("Segway Service Report", "BSA Black Box", null, 25, 11, headPad: 100);
            pdf.AddNewPageDelegate = NewPage;

            Write_BlackBox_Info(pdf, bbox);

            List<BSA_Black_Box_Header> headers = SART_BBBH_Web_Service_Client_REST.Select_BSA_Black_Box_Header_BLACKBOX_KEY(token, bbox.Black_Box_Key);
            if (headers != null)
            {
                Boolean first = true;
                foreach (var header in headers)
                {
                    if (first == false) NewPageNoHeader(pdf);
                    Write_BlackBox_Header_Info(token, pdf, header);
                    first = false;
                }
            }
        }


        private static void NewPageNoHeader(PDF_Helper pdf)
        {
            pdf.AddCustomPage(pdf.Header_String_1, pdf.Header_String_2, pdf.Header_String_3,
                pagewidth: pdf.GetCurrentPageWidth(), pageheight: pdf.GetCurrentPageHeight(),
                headPad: 100);
            pdf.GoToTopOfPage();
            pdf.GotoNextLine();
        }

        private static Double NewPage(PDF_Helper pdf)
        {
            NewPageNoHeader(pdf);
            Black_Box_Data_Display_List.Write_Headers(pdf);
            return pdf.CurrentPoint.Y;
        }

        private static void Write_BlackBox_Info(PDF_Helper pdf, BSA_Black_Box bbox)
        {
            // Tab Positions            0    1    2     3     4     5     6    7     8     9     10    11   
            pdf.SetTabs(new Double[] { .75, 2.0, 3.25, 4.25, 5.75, 6.25, 7.0, 8.5, 10.75, 11.75, 13.5, 14.25, 16.25 });

            pdf.WriteLine("Work Order:", pdf.GetTabRegion(0, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(bbox.Work_Order, pdf.GetTabRegion(1, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.WriteLine("PT Serial:", pdf.GetTabRegion(2, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(bbox.Unit_ID_Serial_Number, pdf.GetTabRegion(3, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.WriteLine("Side:", pdf.GetTabRegion(4, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(bbox.Side, pdf.GetTabRegion(5, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.WriteLine("Date Extracted:", pdf.GetTabRegion(6, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(bbox.Date_Time_Entered.Value.ToString("yyyy-MM-dd HH:mm:ss"), pdf.GetTabRegion(7, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.WriteLine("BSA Serial:", pdf.GetTabRegion(8, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(bbox.BSA_Serial_Number, pdf.GetTabRegion(9, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.WriteLine("User:", pdf.GetTabRegion(10, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(bbox.User_Name, pdf.GetTabRegion(11, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.GotoNextLine();

            pdf.DrawLine(pdf.GetTabRegion(0, 12, pdf.Header_Font_2_Bold.Height));
            pdf.GotoNextLine();
        }

        private static void Write_BlackBox_Header_Info(AuthenticationToken token, PDF_Helper pdf, BSA_Black_Box_Header header)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Double[] tabs = new Double[] { 2.5, 3.85, 5.0, 6.35, 7.5, 8.85, 10.0, 11.35, 12.5, 13.85, 14.5 };
                for (int x = 0; x < tabs.Length; x++)
                {
                    tabs[x] /= 17;
                    tabs[x] *= pdf.GetCurrentPageWidth();
                }
                pdf.SetTabs(tabs);

                var r = pdf.GetTabRegion(0, 10);

                pdf.WriteLine(String.Format("Log: {0}", header.Log), r, null, PDF_Helper.XPA_CenterMiddle, pdf.Header_Font_2);
                pdf.CurrentRegion = r;
                pdf.GotoNextLine(10);
                pdf.WriteLine("CU S/W Version", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.CU_Software_Version), pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("BSA S/W Version", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.BSA_Software_Version), pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Record Index", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("{0}", header.Record_Index), pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Log Version", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.Data_Log_Version), pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Fault Word #1", pdf.GetTabRegion(8), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.Fault_Word_1), pdf.GetTabRegion(9), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.GotoNextLine();
                pdf.WriteLine("Raw VOC", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("{0}", header.Raw_Voc), pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Raw RBat", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("{0}", header.Raw_Rbat), pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Frame Count", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("{0}", header.Frame_Count), pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("PSE Stat Word", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.Pse_Status_Word), pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Fault Word #2", pdf.GetTabRegion(8), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.Fault_Word_2), pdf.GetTabRegion(9), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.GotoNextLine();
                pdf.WriteLine("PSEF A ZeroMean", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("{0}", header.Psef_Azeromean), pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Local COMM Faults", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.BSA_Local_Comm_Faults), pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Local Sensor Faults", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.BSA_Local_Sensor_Faults), pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Local Module Faults", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.BSA_Local_Module_Faults), pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Fault Word #3", pdf.GetTabRegion(8), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.Fault_Word_3), pdf.GetTabRegion(9), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.GotoNextLine();
                pdf.WriteLine("PSEF B ZeroMean", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("{0}", header.Psef_Bzeromean), pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Remote COMM Faults", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.BSA_Remote_Comm_Faults), pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Remote Sensor Faults", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.BSA_Remote_Sensor_Faults), pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Remote Module Faults", pdf.GetTabRegion(6), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.BSA_Remote_Module_Faults), pdf.GetTabRegion(7), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Fault Word #4", pdf.GetTabRegion(8), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.Fault_Word_4), pdf.GetTabRegion(9), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.GotoNextLine();
                pdf.WriteLine("Header CRC", pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("x{0:X4}", header.CRC), pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("CRC Is Valid", pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("{0}", header.Is_CRC_OK), pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.WriteLine("Operation Time", pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(Convert_Operating_Time(header.Operation_Time), pdf.GetTabRegion(5, 3), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3_Bold);
                pdf.GotoNextLine();

                r = pdf.GetTabRegion(0, 10, 20);
                pdf.DrawLine(r);
                pdf.GotoNextLine();
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }


            List<BSA_Black_Box_Data> bbData = SART_BBBD_Web_Service_Client_REST.Select_BSA_Black_Box_Data_HEADER_KEY(token, header.Header_Key);
            if (bbData != null)
            {
                Write_BlackBox_Data_Info(pdf, bbData);
            }
        }

        private static void Write_BlackBox_Data_Info(PDF_Helper pdf, List<BSA_Black_Box_Data> bbData)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                var storefont = pdf.CurrentFont;
                pdf.CurrentFont = Black_Box_Data_Display_List.CalculateFont(pdf, bbData, new XFont("Arial", 8), pdf.CurrentMargin.Width);

                Black_Box_Data_Display_List.Write_Headers(pdf);
                foreach (var item in bbData)
                {
                    Black_Box_Data_Display_List.Write(pdf, item);
                }
                pdf.CurrentFont = storefont;
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

        private static String Convert_Operating_Time(Int32? seconds)
        {
            if (seconds.HasValue == false) return "";
            int sec = seconds.Value * 15;
            int hour = sec / 3600;
            sec %= 3600;
            int min = sec / 60;
            sec %= 60;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}", seconds.Value);
            if (seconds.Value > 0) sb.Append(" - ");
            if (hour > 0)
            {
                sb.AppendFormat("Hours: {0}", hour);
            }
            if (min > 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.AppendFormat("Minutes: {0}", min);
            }
            if (sec > 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.AppendFormat("Seconds: {0}", sec);
            }

            return sb.ToString();
        }

    }
}
