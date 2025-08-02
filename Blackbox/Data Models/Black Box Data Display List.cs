using NLog;
using PdfSharp.Drawing;
using Segway.SART.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.Tools.PDFHelper;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Data_Display_List</summary>
    public class Black_Box_Data_Display_List
    {
        /// <summary>Protected Member - logger</summary>
        protected static Logger logger = Logger_Helper.GetCurrentLogger();

        #region BBData

        private static List<Black_Box_Data_Display> _BBData;

        /// <summary>Property BBData of type List&lt;Black_Box_Data_Display&gt;</summary>
        public static List<Black_Box_Data_Display> BBData
        {
            get
            {
                if (_BBData == null)
                {
                    _BBData = new List<Black_Box_Data_Display>();
                    _BBData.Add(new Black_Box_Data_Display("Record", "Rec", 0, false, typeof(int)));
                    _BBData.Add(new Black_Box_Data_Display("Adjusted_Frame_Count", "Adj\nFrame\nCount", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Frame_Count", "Frame\nCount", 0, hex: false));
                    _BBData.Add(new Black_Box_Data_Display("State_Bits", "State Bits", 0));
                    _BBData.Add(new Black_Box_Data_Display("Controller_Flags", "Controller\nFlags", 0));
                    _BBData.Add(new Black_Box_Data_Display("SK_Test_Word_1", "SK Test\nWord 1", 0));
                    _BBData.Add(new Black_Box_Data_Display("SK_Test_Word_2", "SK Test\nWord 2", 0));
                    _BBData.Add(new Black_Box_Data_Display("SK_Test_Word_3", "SK Test\nWord 3", 0));
                    _BBData.Add(new Black_Box_Data_Display("Pse_Info_Bits", "PSE Info\nBits", 0));
                    _BBData.Add(new Black_Box_Data_Display("Pitch_Angle", "Pitch\nAngle", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Pitch_Rate", "Pitch\nRate", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Roll_Angle", "Roll\nAngle", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Yaw_Rate", "Yaw\nRate", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Left_Wheel_Speed", "Left\nWheel\nSpeed", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Right_Wheel_Speed", "Right\nWheel\nSpeed", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Steering_Command", "Steering\nCmd", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Vbus", "Vbus", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Bridge_Current", "Bridge\nCurrent", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Left_Motor_IQ", "Left\nMotor\nIQ", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Right_Motor_IQ", "Right\nMotor\nIQ", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Left_Motor_VQ", "Left\nMotor\nVQ", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Right_Motor_VQ", "Right\nMotor\nVQ", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Current_Limit", "Current\nLimit", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Desired_Pitch_Offset", "Desired\nPitch\nOffset", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Speed_Limit", "Speed\nLimit", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Steering_Reduction", "Steering\nReduction", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Desired_Yaw_Command", "Desired\nYaw\nCmd", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Pitch_Command", "Pitch\nCmd", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Yaw_Command", "Yaw\nCmd", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Shake_Command", "Shake\nCmd", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Left_Traction_Command", "Left\nTract\nCmd", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Right_Traction_Command", "Right\nTract\nCmd", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("ARS_0", "ARS 0", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("ARS_1", "ARS 1", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("ARS_2", "ARS 2", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("ARS_3", "ARS 3", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("ARS_4", "ARS 4", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Local_Pitch_ACC", "Local\nPitch\nAcc", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Local_Roll_ACC", "Local\nRoll\nAcc", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Local_Steering_Sensor", "Local\nSteering\nSensor", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("ACC_Temperature", "Acc\nTemp", 0, false));
                    _BBData.Add(new Black_Box_Data_Display("Entry_CRC", "Entry\nCRC", 0));
                    _BBData.Add(new Black_Box_Data_Display("CRC_Is_Valid", "CRC IS\nValid", 0, false, typeof(Boolean)));
                }
                return _BBData;
            }
            set
            {
                _BBData = value;
            }
        }

        #endregion

        /// <summary>Public Static Method - CalculateFont</summary>
        /// <param name="pdf">PDF_Helper</param>
        /// <param name="data">List&lt;BSA_Black_Box_Data&gt;</param>
        /// <param name="font">XFont</param>
        /// <param name="width">Double</param>
        /// <returns>XFont</returns>
        public static XFont CalculateFont(PDF_Helper pdf, List<BSA_Black_Box_Data> data, XFont font, Double width)
        {
            Type T = typeof(BSA_Black_Box_Data);

            while (true)
            {
                Clear_Widths();
                foreach (var item in data)
                {
                    foreach (var bbd in _BBData)
                    {
                        PropertyInfo pi = T.GetProperty(bbd.PropertyName);
                        if (pi == null) throw new Exception(String.Format("Unknown Property named: {0}", bbd.PropertyName));
                        object obj = pi.GetValue(item, null);
                        if (obj != null)
                        {
                            String strData = "";
                            if (pi.PropertyType == typeof(Int32))
                            {
                                strData = obj.ToString();
                            }
                            else if (pi.PropertyType == typeof(short?))
                            {
                                if (bbd.DataType == typeof(Boolean))
                                {
                                    Int16? dat = (Int16?)obj;
                                    if (dat.HasValue == false) strData = "No";
                                    else if (dat.Value == 0) strData = "No";
                                    else strData = "Yes";
                                }
                                else if (bbd.DisplayHex == true)
                                {
                                    strData = String.Format("x{0:X4}", obj);
                                }
                                else
                                {
                                    strData = String.Format("{0}", obj);
                                }
                            }

                            var StrSize = pdf.MeasureString(strData, font);
                            bbd.Width = Math.Max(bbd.Width, StrSize.Width);
                        }
                    }
                }

                CalculateHeaderWidths(pdf, font);
                if (CalculatePropertyWidths() < width) return font;
                font = new XFont(font.FontFamily.Name, font.Size - 1);
                if (font.Size < 6) return font; // throw new Exception("An acceptable font size could not be calculated");
            }
        }

        /// <summary>Public Contructor - static</summary>
        public static void Clear_Widths()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                foreach (var data in BBData)
                {
                    data.Width = 0;
                }
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
        }

        /// <summary>Public Contructor - static</summary>
        public static Double CalculatePropertyWidths()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                Double totalWidth = 1;
                foreach (var data in BBData)
                {
                    totalWidth += data.Width + 3;
                }
                return totalWidth;
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
        }

        private static void CalculateHeaderWidths(PDF_Helper pdf, XFont font)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                foreach (var data in BBData)
                {
                    XSize regSize = pdf.MeasureString(data.Header, font);
                    Double headWidth = regSize.Width;
                    data.Width = Math.Max(data.Width, headWidth);
                }

            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                throw;
            }
        }

        /// <summary>Public Static Method - Write</summary>
        /// <param name="pdf">PDF_Helper</param>
        /// <param name="data">BSA_Black_Box_Data</param>
        public static void Write(PDF_Helper pdf, BSA_Black_Box_Data data)
        {
            Type T = typeof(BSA_Black_Box_Data);

            double left = Calculate_Starting_Left(pdf);
            XPen pen = new XPen(pdf.Color_Black, .5);
            XStringFormat align = PDF_Helper.XPA_LeftMiddle;

            foreach (var bbd in _BBData)
            {
                PropertyInfo pi = T.GetProperty(bbd.PropertyName);
                if (pi == null) throw new Exception(String.Format("Unknown Property named: {0}", bbd.PropertyName));
                object obj = pi.GetValue(data, null);
                if (obj != null)
                {
                    String strData = "";
                    if (pi.PropertyType == typeof(Int32))
                    {
                        strData = obj.ToString();
                        align = PDF_Helper.XPA_RightMiddle;
                    }
                    else if (pi.PropertyType == typeof(short?))
                    {
                        if (bbd.DataType == typeof(Boolean))
                        {
                            Int16? dat = (Int16?)obj;
                            if (dat.HasValue == false) strData = "No";
                            else if (dat.Value == 0) strData = "No";
                            else strData = "Yes";
                            align = PDF_Helper.XPA_CenterMiddle;
                        }
                        else if (bbd.DisplayHex == true)
                        {
                            strData = String.Format("x{0:X4}", obj);
                            align = PDF_Helper.XPA_CenterMiddle;
                        }
                        else
                        {
                            strData = String.Format("{0}", obj);
                            align = PDF_Helper.XPA_RightMiddle;
                        }
                    }

                    pdf.CurrentRegion = new XRect(new XPoint(left, pdf.CurrentPoint.Y), new XSize(bbd.Width + 3, pdf.CurrentFont.Height));
                    //pdf.WriteLine(strData, r, null, PDF_Helper.XPA_LeftMiddle, pdf.CurrentFont);
                    pdf.CurrentRegion = pdf.WriteBoxedLine(strData, pdf.CurrentRegion, null, pen, pdf.Brush_Black, pdf.CurrentFont, align, true);
                    left += pdf.CurrentRegion.Width;
                }
            }
            pdf.CurrentPoint = pdf.CurrentRegion.BottomLeft;
        }

        private static double Calculate_Starting_Left(PDF_Helper pdf)
        {
            return pdf.CurrentMargin.Left + ((pdf.CurrentMargin.Width - CalculatePropertyWidths()) / 2);
        }

        /// <summary>Public Static Method - Write_Headers</summary>
        /// <param name="pdf">PDF_Helper</param>
        public static void Write_Headers(PDF_Helper pdf)
        {
            Type T = typeof(BSA_Black_Box_Data);
            Double left = Calculate_Starting_Left(pdf);
            if (left < pdf.CurrentMargin.Left) left = pdf.CurrentMargin.Left + 1;
            XPen pen = new XPen(pdf.Color_Black, 1);
            foreach (var bbd in _BBData)
            {
                XSize headSize = pdf.MeasureString(bbd.Header, pdf.CurrentFont);
                pdf.CurrentRegion = new XRect(new XPoint(left, pdf.CurrentPoint.Y), new XSize(bbd.Width + 3, pdf.CurrentFont.Height * 3));
                pdf.WriteLine(bbd.Header, pdf.CurrentRegion, null, PDF_Helper.XPA_CenterMiddle, pdf.CurrentFont);
                //pdf.WriteBoxedLine(bbd.Header, pdf.CurrentRegion, null, pen, pdf.Brush_Black, pdf.CurrentFont, PDF_Helper.XPA_LeftMiddle, true, 2);
                left += pdf.CurrentRegion.Width;
            }
            pdf.CurrentPoint = pdf.CurrentRegion.BottomLeft;
        }
    }
}
