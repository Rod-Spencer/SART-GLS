using NLog;
using PdfSharp.Drawing;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.PDFHelper;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Services_Graph</summary>
    public class Black_Box_Services_Graph
    {
        /// <summary>Protected Member - logger</summary>
        protected static Logger logger = Logger_Helper.GetCurrentLogger();

        private static BSA_Black_Box blackBox = null;
        private static AuthenticationToken blackBoxToken;
        private static Manufacturing_Models blackBoxModel;
        private static BSA_Black_Box_Header blackBoxHeader;
        private static List<String> GraphNames = null;



        /// <summary>Public Static Method - Report</summary>
        /// <param name="pdf">PDF_Helper</param>
        /// <param name="bbox">BSA_Black_Box</param>
        /// <param name="token">AuthenticationToken</param>
        /// <param name="model">Manufacturing_Models</param>
        /// <param name="graphNames">List&lt;String&gt; - </param>
        public static void Report(PDF_Helper pdf, BSA_Black_Box bbox, AuthenticationToken token, Manufacturing_Models model, List<String> graphNames)
        {
            blackBox = bbox;
            blackBoxToken = token;
            blackBoxModel = model;
            GraphNames = graphNames;

            pdf.Header_String_1 = "Segway Service Report";
            pdf.Header_String_2 = "BSA Black Box";
            pdf.Header_String_3 = null;
            pdf.AddNewPageDelegate = NewPage;

            var blackBoxHeaders = SART_BBBH_Web_Service_Client_REST.Select_BSA_Black_Box_Header_BLACKBOX_KEY(token, bbox.Black_Box_Key);
            if (blackBoxHeaders != null)
            {
                //Boolean first = true;
                foreach (var header in blackBoxHeaders)
                {
                    blackBoxHeader = header;
                    //if (first == true)
                    //{
                    NewPage(pdf);
                    //Write_BlackBox_Info(pdf);
                    //Write_BlackBox_Header_Info(blackBoxToken, pdf, blackBoxHeader, blackBoxModel);
                    //}
                    Write_Graphs(pdf, token, model, header);
                    //first = false;
                }
            }
        }

        private static void Write_Graphs(PDF_Helper pdf, AuthenticationToken token, Manufacturing_Models model, BSA_Black_Box_Header header)
        {
            List<BSA_Black_Box_Data> bbData = SART_BBBD_Web_Service_Client_REST.Select_BSA_Black_Box_Data_HEADER_KEY(token, header.Header_Key);
            if (bbData != null)
            {
                Write_BlackBox_Data_Graphs(pdf, bbData, header.Log, model);
            }
        }

        private static void NewPageNoHeader(PDF_Helper pdf)
        {
            pdf.AddLedgerLandscapePage(pdf.Header_String_1, pdf.Header_String_2, pdf.Header_String_3, headPad: 100);
            pdf.GoToTopOfPage();
            pdf.GotoNextLine();
        }

        private static Double NewPage(PDF_Helper pdf)
        {
            NewPageNoHeader(pdf);
            Write_BlackBox_Info(pdf);
            Write_BlackBox_Header_Info(blackBoxToken, pdf, blackBoxHeader, blackBoxModel);
            pdf.CurrentPoint = new XPoint(pdf.CurrentPoint.X, 140);
            return pdf.CurrentPoint.Y;
        }

        private static void Write_BlackBox_Info(PDF_Helper pdf)
        {
            // Tab Positions            0    1    2     3     4     5     6    7     8     9     10    11   
            pdf.SetTabs(new Double[] { .75, 2.0, 3.25, 4.25, 5.75, 6.25, 7.0, 8.5, 10.75, 11.75, 13.5, 14.25, 16.25 });

            pdf.WriteLine("Work Order:", pdf.GetTabRegion(0, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(blackBox.Work_Order, pdf.GetTabRegion(1, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.WriteLine("PT Serial:", pdf.GetTabRegion(2, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(blackBox.Unit_ID_Serial_Number, pdf.GetTabRegion(3, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.WriteLine("Side:", pdf.GetTabRegion(4, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(blackBox.Side, pdf.GetTabRegion(5, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.WriteLine("Date Extracted:", pdf.GetTabRegion(6, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(blackBox.Date_Time_Entered.Value.ToString("yyyy-MM-dd HH:mm:ss"), pdf.GetTabRegion(7, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.WriteLine("BSA Serial:", pdf.GetTabRegion(8, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(blackBox.BSA_Serial_Number, pdf.GetTabRegion(9, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.WriteLine("User:", pdf.GetTabRegion(10, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2);
            pdf.WriteLine(blackBox.User_Name, pdf.GetTabRegion(11, 20.0), paraformat: PDF_Helper.XPA_LeftMiddle, font: pdf.Header_Font_2_Bold);
            pdf.CurrentPoint = pdf.GetTabRegion(0, 1, pdf.Header_Font_2.Height).BottomLeft;
            var r = pdf.GetTabRegion(0, 12, 5.0);
            //pdf.DrawLine(r, new XPen(pdf.Color_Black, .5));
            pdf.CurrentPoint = r.BottomLeft;
        }


        private static void Write_BlackBox_Header_Info(AuthenticationToken token, PDF_Helper pdf, BSA_Black_Box_Header header, Manufacturing_Models model)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                // Tab Positions            0    1    2     3     4     5     6    7     8     9     10    11   
                pdf.SetTabs(new Double[] { .75, 2.0, 4.25, 6.25, 8.5, 10.5, 14.25, 16.25 });

                var r = pdf.GetTabRegion(0);
                pdf.WriteLine(String.Format("Log: {0}", header.Log), pdf.GetTabRegion(0), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.CurrentRegion = r;
                pdf.WriteLine(String.Format("CU Build: {0}", header.CU_Build), pdf.GetTabRegion(1), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("CU Part Number: {0}", header.CU_Build), pdf.GetTabRegion(2), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("BSA Build: {0}", header.BSA_Flight_Code_Build), pdf.GetTabRegion(3), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("BSA Flight Vers: {0}", header.BSA_Flight_Code_Version), pdf.GetTabRegion(4), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.WriteLine(String.Format("Op Time: {0}", Convert_Operating_Time(header.Operation_Time)), pdf.GetTabRegion(5), null, PDF_Helper.XPA_LeftMiddle, pdf.Header_Font_3);
                pdf.GotoNextLine();

                r = pdf.GetTabRegion(0, 7, 20);
                pdf.DrawLine(r);
                pdf.CurrentPoint = r.BottomLeft;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
        }


        private static void Write_BlackBox_Data_Graphs(PDF_Helper pdf, List<BSA_Black_Box_Data> bbData, int log, Manufacturing_Models model)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                var storefont = pdf.CurrentFont;
                pdf.CurrentFont = Black_Box_Data_Display_List.CalculateFont(pdf, bbData, new XFont("Arial", 8), pdf.CurrentMargin.Width);
                Black_Box_Data_Graph_Data.Load(bbData);
                Black_Box_Graphs graphs = new Black_Box_Graphs();
                graphs.Load();
                int graphCount = 0;

                XRect graphArea = pdf.CurrentRegion;
                XRect graphRegion = default(XRect);
                XRect legendArea = default(XRect);
                XSize graphAreaSize = new XSize(XUnit.FromInch(7.5), XUnit.FromInch(4));
                XSize graphSize = new XSize(XUnit.FromInch(6.5), XUnit.FromInch(2.5));

                foreach (var graphName in GraphNames)
                {
                    Black_Box_Data_Graph graph = graphs.Find(graphName);
                    if (graph == null) continue;

                    if (graph.Legend == BlackBox.Reporter.Black_Box_Graph_Legend_Types.UnDefined) graph.Legend = BlackBox.Reporter.Black_Box_Graph_Legend_Types.Below;

                    ////////////////////////////////////////////////////////
                    // Calculate Graph Area Size
                    graphAreaSize = new XSize(XUnit.FromInch(graph.AreaSize.Width), XUnit.FromInch(graph.AreaSize.Height));
                    if ((graphAreaSize == XSize.Empty) || ((graphAreaSize.Width == 0) && (graphAreaSize.Height == 0)))
                    {
                        graph.AreaSize.Width = 7.5;
                        graph.AreaSize.Height = 4;
                        graphAreaSize = new XSize(XUnit.FromInch(graph.AreaSize.Width), XUnit.FromInch(graph.AreaSize.Height));
                    }
                    // Calculate Graph Area Size
                    ////////////////////////////////////////////////////////


                    ////////////////////////////////////////////////////////
                    // Calculate Graph Region Size
                    graphSize = new XSize(XUnit.FromInch(graph.GraphSize.Width), XUnit.FromInch(graph.GraphSize.Height));
                    if ((graphSize == XSize.Empty) || ((graphSize.Width == 0) && (graphSize.Height == 0)))
                    {
                        graph.GraphSize.Width = 6.5;
                        graph.GraphSize.Height = 2.5;
                        graphSize = new XSize(XUnit.FromInch(graph.GraphSize.Width), XUnit.FromInch(graph.GraphSize.Height));
                    }
                    // Calculate Graph Region Size
                    ////////////////////////////////////////////////////////

                    if (graph.XAxis.Generation == Black_Box_Axis_Generation.Auto) graph.XAxis.Generation = Black_Box_Axis_Generation.Manual;


                    ////////////////////////////////////////////////////////
                    // Draw Graph Area Box
                    if ((graphCount & 1) == 0)
                    {
                        if (((graphCount % 4) == 0) && (graphCount > 0))
                        {
                            NewPage(pdf);
                            graphArea = pdf.CurrentRegion;
                        }
                        graphArea = new XRect(new XPoint(pdf.CurrentPage.Width / 2 - graphAreaSize.Width, graphArea.Top), graphAreaSize);
                        graphArea.Offset(-10, 10);
                    }
                    else
                    {
                        graphArea = new XRect(new XPoint(pdf.CurrentPage.Width / 2, graphArea.Top), graphAreaSize);
                        graphArea.Offset(10, 0);
                    }
                    graphArea = pdf.DrawBox(graphArea);
                    // Draw Graph Area Box
                    ////////////////////////////////////////////////////////



                    ////////////////////////////////////////////////////////
                    // Draw Legend Area 
                    if (graph.Legend == BlackBox.Reporter.Black_Box_Graph_Legend_Types.Below)
                    {
                        legendArea = new XRect(new XPoint(graphArea.X, graphArea.Bottom - XUnit.FromInch(.5)), new XPoint(graphArea.Right, graphArea.Bottom));
                        pdf.DrawLine(legendArea.TopLeft, legendArea.TopRight);
                    }
                    // Draw Legend Area 
                    ////////////////////////////////////////////////////////


                    ////////////////////////////////////////////////////////
                    // Draw Graph Box
                    graphRegion = new XRect(graphArea.TopLeft, graphSize);
                    graphRegion.Offset(XUnit.FromInch(.75), XUnit.FromInch(.35));
                    pdf.DrawBox(graphRegion, new XPen(pdf.Color_Black, .25), pdf.Brush_AliceBlue);
                    // Draw Graph Box
                    ////////////////////////////////////////////////////////


                    /////////////////////////////////////////
                    // Write Graph Title
                    XRect titleRegion = new XRect(graphArea.TopLeft, new XSize(graphArea.Width, graphRegion.Top - graphArea.Top));
                    pdf.WriteLine(graph.Graph_Name, titleRegion, paraformat: PDF_Helper.XPA_CenterMiddle, font: pdf.Header_Font_2);
                    // Write Graph Title
                    /////////////////////////////////////////


                    /////////////////////////////////////////
                    // Write X-Axis hash lines
                    double bottom = 0;
                    if (graph.XAxis.Generation == Black_Box_Axis_Generation.Manual)
                    {
                        for (Double x = graph.XAxis.Min; x <= graph.XAxis.Max; x += graph.XAxis.Increment)
                        {
                            if ((x != graph.XAxis.Min) && (x != graph.XAxis.Max))
                            {
                                double X = graphRegion.Left + ((x - graph.XAxis.Min) * (graphRegion.Width / graph.XAxis.Max));
                                XPen pen = new XPen(pdf.Color_Gray, .25);
                                pen.DashStyle = XDashStyle.Dash;
                                pdf.DrawLine(new XPoint(X, graphRegion.Top), new XPoint(X, graphRegion.Bottom), pen);
                            }
                            XRect r = new XRect(new XPoint(graphRegion.Left + ((x - graph.XAxis.Min) * (graphRegion.Width / graph.XAxis.Max)), graphRegion.Bottom), new XSize(25, pdf.Header_Font_3.Height + 3));
                            r.Offset(r.Width / -2, 3);
                            pdf.WriteLine(x.ToString(), r, paraformat: PDF_Helper.XPA_CenterTop);
                            bottom = Math.Max(bottom, r.Bottom);
                        }
                    }
                    // Write X-Axis hash lines
                    /////////////////////////////////////////

                    /////////////////////////////////////////
                    // Write X-Axis Label
                    XRect label = new XRect(new XPoint(graphRegion.Left, bottom), new XSize(graphRegion.Width, pdf.Header_Font_3.Height));
                    pdf.WriteLine(graph.XAxis.Label, label, paraformat: PDF_Helper.XPA_CenterTop, font: pdf.Header_Font_3);
                    // Write X-Axis Label
                    /////////////////////////////////////////

                    double legendWidth = graphArea.Width / graph.Column_Data.Count;

                    if (graph.YAxis.Generation == Black_Box_Axis_Generation.Auto)
                    {
                        graph.YAxis.Max = int.MinValue;
                        graph.YAxis.Min = int.MaxValue;
                    }

                    foreach (var cd in graph.Column_Data)
                    {
                        if (graph.Legend == BlackBox.Reporter.Black_Box_Graph_Legend_Types.Below)
                        {
                            ////////////////////////////////////////////
                            // Draw Legend Entry

                            // Draw Legend Line
                            XSize legendItemSize = pdf.MeasureString(cd.Display_Name, pdf.Header_Font_3);
                            legendItemSize.Width += XUnit.FromInch(.5);

                            int ndx = graph.Column_Data.IndexOf(cd);
                            double X = legendArea.Left + (ndx * legendWidth) + ((legendWidth - legendItemSize.Width) / 2);
                            XColor c = pdf.Find_Color(cd.Color);
                            XPoint p1 = new XPoint(X, legendArea.Center.Y);
                            XPoint p2 = new XPoint(X + XUnit.FromInch(.35).Point, p1.Y);
                            pdf.DrawLine(p1, p2, new XPen(c, 2.0));

                            // Draw Legend Label
                            label = new XRect(new XPoint(X, legendArea.Top), new XSize(legendItemSize.Width, legendArea.Height));
                            pdf.WriteLine(cd.Display_Name, label, paraformat: PDF_Helper.XPA_RightMiddle, font: pdf.Header_Font_3);
                            // Draw Legend Entry
                            ////////////////////////////////////////////
                        }

                        if (Black_Box_Data_Graph_Data.Data.ContainsKey(cd.Column_Name) == true)
                        {
                            cd.Set_Data(Black_Box_Data_Graph_Data.Data[cd.Column_Name]);
                            foreach (var data in cd.Get_Data(model))
                            {
                                graph.YAxis.Max = Math.Max(graph.YAxis.Max, data);
                                graph.YAxis.Min = Math.Min(graph.YAxis.Min, data);
                            }
                        }
                    }

                    if (graph.YAxis.Generation == Black_Box_Axis_Generation.Auto)
                    {
                        /////////////////////////////////////////
                        // Define Y-Axis parameters
                        if (graph.YAxis.Min < graph.YAxis.Max)
                        {
                            double dval = graph.YAxis.Max;
                            double adjust = Math.Pow(10, graph.YAxis.Power);
                            dval /= adjust;
                            dval = Math.Ceiling(dval) * adjust;
                            graph.YAxis.Max = Math.Max(graph.YAxis.Max, dval);

                            dval = graph.YAxis.Min;
                            dval /= adjust;
                            dval = Math.Floor(dval) * adjust;
                            graph.YAxis.Min = dval;
                            graph.YAxis.Min = Math.Min(graph.YAxis.Min, dval);
                        }
                        // Define Y-Axis parameters
                        /////////////////////////////////////////
                    }

                    graph.YAxis.Ticks = graph.YAxis.Max - graph.YAxis.Min;
                    if (graph.YAxis.Ticks == 0)
                    {
                        graph.YAxis.Max = Math.Ceiling(graph.YAxis.Max);
                        graph.YAxis.Min = Math.Floor(graph.YAxis.Min);
                        graph.YAxis.Ticks = graph.YAxis.Max - graph.YAxis.Min;
                        if (graph.YAxis.Ticks == 0)
                        {
                            graph.YAxis.Max = graph.YAxis.Max + 1;
                            graph.YAxis.Min = graph.YAxis.Min - 1;
                            graph.YAxis.Ticks = graph.YAxis.Max - graph.YAxis.Min;
                        }
                    }
                    if (graph.YAxis.MajorTicks == 0) graph.YAxis.MajorTicks = (graph.YAxis.Ticks / graph.YAxis.Increment);
                    graph.YAxis.Increment = graph.YAxis.Ticks / graph.YAxis.MajorTicks;


                    /////////////////////////////////////////
                    // Write Y-Axis hash lines and ticks
                    String fmt = String.Format("{{0:N{0}}}", graph.YAxis.Decimals);
                    int count = 0;
                    for (double x = graph.YAxis.Min; x <= graph.YAxis.Max; x += graph.YAxis.Increment, count++)
                    {
                        double Y = graphRegion.Bottom - (count * (graphRegion.Height / graph.YAxis.MajorTicks));
                        if ((x != graph.YAxis.Min) && (x != graph.YAxis.Max))
                        {
                            XPen pen = new XPen(pdf.Color_Gray, .25);
                            pen.DashStyle = XDashStyle.Dash;
                            pdf.DrawLine(new XPoint(graphRegion.Left, Y), new XPoint(graphRegion.Right, Y), pen);
                        }
                        XRect r = new XRect(new XPoint(graphRegion.Left - 3, Y), new XSize(50, pdf.Header_Font_3.Height));
                        r.Offset(-50, r.Height / -2);
                        pdf.WriteLine(String.Format(fmt, x), r, paraformat: PDF_Helper.XPA_RightMiddle);
                        //bottom = Math.Max(bottom, r.Bottom);
                    }
                    // Write Y-Axis hash lines and ticks
                    /////////////////////////////////////////

                    /////////////////////////////////////////
                    // Write Y-Axis Label
                    double xoffset = (graphRegion.Left - graphArea.Left) / 4;
                    XRect labelRect = new XRect(new XPoint(graphArea.Left + xoffset, graphRegion.Bottom), new XSize(graphRegion.Height, pdf.Header_Font_3.Height));
                    pdf.WriteLine_Rotated(graph.YAxis.Label, labelRect, null, PDF_Helper.XSF_CenterMiddle, pdf.Header_Font_3, angle: -90.0);
                    // Write Y-Axis Label
                    /////////////////////////////////////////


                    double xdist = graphRegion.Width / graph.XAxis.Ticks;
                    double ydist = graphRegion.Height / graph.YAxis.MajorTicks;
                    foreach (var cd in graph.Column_Data)
                    {
                        XPen pen = new XPen(pdf.Find_Color(cd.Color), .5);
                        if (graph.Legend == BlackBox.Reporter.Black_Box_Graph_Legend_Types.InLine)
                        {
                            int ndx = graph.Column_Data.IndexOf(cd);
                            XPoint pt1 = new XPoint(graphRegion.Left + 3, graphRegion.Top + (ndx * ydist));
                            XRect legReg = new XRect(pt1, new XSize(XUnit.FromInch(graph.GraphSize.Width), ydist));
                            pdf.WriteLine(legReg, cd.Display_Name, pdf.Find_Brush(cd.Color), paraformat: PDF_Helper.XPA_LeftBottom, font: new XFont(pdf.CurrentFont.FontFamily.Name, 7));
                        }
                        var data = cd.Get_Data(model);
                        for (int x = 0; x < data.Count - 1; x++)
                        {
                            Double dval = data[x];
                            dval -= graph.YAxis.Min;
                            dval /= graph.YAxis.Ticks;
                            dval *= graphRegion.Height;
                            XPoint pt1 = new XPoint(graphRegion.Left + (x * xdist), graphRegion.Bottom - dval);
                            dval = data[x + 1];
                            dval -= graph.YAxis.Min;
                            dval /= graph.YAxis.Ticks;
                            dval *= graphRegion.Height;
                            XPoint pt2 = new XPoint(graphRegion.Left + ((x + 1) * xdist), graphRegion.Bottom - dval);
                            pdf.DrawLine(pt1, pt2, pen);
                        }
                    }
                    ////if (((graphCount % 4) == 0) && (graphCount > 0))
                    //{
                    //    pdf.AddNewPage(pdf);
                    //}
                    if ((graphCount & 1) == 1)
                    {
                        graphArea = new XRect(graphArea.BottomLeft, graphArea.Size);
                    }
                    graphCount++;
                }

                graphs.Save();
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
