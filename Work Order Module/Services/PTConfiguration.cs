using Microsoft.Practices.Unity;
using NLog;
using Segway.Login.Objects;
using Segway.Manufacturing.Objects;
using Segway.Modules.SART_Infrastructure;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.Common;
using Segway.Service.Controls.StatusBars;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.Objects;
using Segway.Service.SART.Client.REST;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Segway.Modules.WorkOrder
{
    public class PTConfiguration
    {
        private static Logger logger = Logger_Helper.GetCurrentLogger();


        public static List<SART_PT_Configuration> Get_PT_Configuration(String PTSerial, String woID, out Boolean IsError, out String ErrorMsg)
        {
            IsError = false;
            ErrorMsg = null;

            Dictionary<String, Dictionary<String, Dictionary<String, String>>> CI = new Dictionary<string, Dictionary<string, Dictionary<String, String>>>();
            Production_Line_Assembly ptAssembly = null;
            List<Production_Line_Assembly> ptSubAssemblies = null;
            List<Stage1_Functional_Info> ptManufConfig1 = null;
            List<Stage2_Functional_Info> ptManufConfig2 = null;
            //List<SART_PT_Configuration> ptPrevServiceReq = null;
            //List<SART_Work_Order> ptServices = null;

            Dictionary<String, SART_PT_Configuration> configurations = new Dictionary<String, SART_PT_Configuration>();
            SART_PT_Configuration manufacture = new SART_PT_Configuration();
            manufacture.Type = "Manufactured";
            String key;

            try
            {
                //////////////////////////////////////////////////////////////////
                /// Stage 1 Functional Info Records
                try
                {
                    logger.Debug("Retrieving STAGE1_FUNCTIONAL_INFO records.");
                    //SART_Infrastructure.eventAggregator.GetEvent<StatusBar_Region1_Event>().Publish("Retrieving STAGE1_FUNCTIONAL_INFO records...");
                    ptManufConfig1 = Manufacturing_S1FI_Web_Service_Client_REST.Select_Stage1_Functional_Info_UNIT_ID_SERIAL_NUMBER(InfrastructureModule.Token, PTSerial);
                    if (ptManufConfig1 == null)
                    {
                        ErrorMsg = String.Format("Request for STAGE1_FUNCTIONAL_INFO records returned a null for PT: {0}.", PTSerial);
                        logger.Error(ErrorMsg);
                        IsError = true;
                    }
                    else
                    {
                        foreach (Stage1_Functional_Info data in ptManufConfig1)
                        {
                            if (manufacture.Date_Time_Created == null)
                            {
                                if (data.Date_Time_Entered.HasValue) manufacture.Date_Time_Created = data.Date_Time_Entered;
                                else if (data.Date_Time_Tested.HasValue) manufacture.Date_Time_Created = data.Date_Time_Tested;
                            }
                            if (String.IsNullOrEmpty(data.BSA_A_Serial_Number) == false) manufacture.BSA_A_Serial = data.BSA_A_Serial_Number;
                            if (String.IsNullOrEmpty(data.BSA_A_Part_Number) == false) manufacture.BSA_A_Part_Number = data.BSA_A_Part_Number;
                            if (String.IsNullOrEmpty(data.BSA_A_SW_Version) == false) manufacture.BSA_A_SW_Version = Convert_SWVersion(data.BSA_A_SW_Version);

                            if (String.IsNullOrEmpty(data.BSA_B_Serial_Number) == false) manufacture.BSA_B_Serial = data.BSA_B_Serial_Number;
                            if (String.IsNullOrEmpty(data.BSA_B_Part_Number) == false) manufacture.BSA_B_Part_Number = data.BSA_B_Part_Number;
                            if (String.IsNullOrEmpty(data.BSA_B_SW_Version) == false) manufacture.BSA_B_SW_Version = Convert_SWVersion(data.BSA_B_SW_Version);

                            if (String.IsNullOrEmpty(data.CU_A_Part_Number) == false) manufacture.CUA_Part_Number = data.CU_A_Part_Number;
                            if (String.IsNullOrEmpty(data.CU_A_Serial_Number) == false) manufacture.CUA_Serial = data.CU_A_Serial_Number;
                            if (String.IsNullOrEmpty(data.CU_A_SW_Version) == false) manufacture.CUA_SW_Version = Convert_SWVersion(data.CU_A_SW_Version);

                            if (String.IsNullOrEmpty(data.CU_B_Part_Number) == false) manufacture.CUB_Part_Number = data.CU_B_Part_Number;
                            if (String.IsNullOrEmpty(data.CU_B_Serial_Number) == false) manufacture.CUB_Serial = data.CU_B_Serial_Number;
                            if (String.IsNullOrEmpty(data.CU_B_SW_Version) == false) manufacture.CUB_SW_Version = Convert_SWVersion(data.CU_B_SW_Version);
                        }
                    }
                    if (manufacture.Date_Time_Created.HasValue)
                    {
                        key = String.Format("{0}/{1}", manufacture.Date_Time_Created.Value.ToString("yyyy-MM-dd"), manufacture.Type);
                    }
                    else
                    {
                        key = String.Format("{0}/{1}", DateTime.MinValue.ToString("yyyy-MM-dd"), manufacture.Type);
                    }
                    configurations[key] = manufacture;
                }
                catch (Authentication_Exception ae)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(ae));
                    throw;
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString("Error retrieving STAGE1_FUCNTIONAL_INFO", ex));
                    throw;
                }
                /// Stage 1 Functional Info Records
                //////////////////////////////////////////////////////////////////

                //////////////////////////////////////////////////////////////////
                /// Stage 2 Functional Info Records
                try
                {
                    logger.Debug("Retrieving STAGE2_FUNCTIONAL_INFO records.");
                    ptManufConfig2 = Manufacturing_S2FI_Web_Service_Client_REST.Select_Stage2_Functional_Info_UNIT_ID_SERIAL_NUMBER(InfrastructureModule.Token, PTSerial);
                    if (ptManufConfig2 == null)
                    {
                        String msg = String.Format("Request for STAGE2_FUNCTIONAL_INFO records returned a null for PT: {0}.", PTSerial);
                        logger.Error(msg);
                        if (IsError == true)
                        {
                            ErrorMsg += "\n" + msg;
                        }
                        else
                        {
                            ErrorMsg = msg;
                            IsError = true;
                        }
                    }
                    else
                    {
                        foreach (Stage2_Functional_Info data in ptManufConfig2)
                        {
                            if (manufacture.Date_Time_Created == null)
                            {
                                if (data.Date_Time_Entered.HasValue) manufacture.Date_Time_Created = data.Date_Time_Entered;
                                else if (data.Date_Time_Tested.HasValue) manufacture.Date_Time_Created = data.Date_Time_Tested;
                            }
                            if (String.IsNullOrEmpty(data.UI_Part_Number) == false) manufacture.UIC_Part_Number = Convert_UIC_PartNumber(data.UI_Part_Number);
                            if (String.IsNullOrEmpty(data.UI_Serial_Number) == false) manufacture.UIC_SID = Get_UIC_Serial(data.UI_Serial_Number);
                            if (String.IsNullOrEmpty(data.UI_SW_Version) == false) manufacture.UIC_SW_Version = data.UI_SW_Version;
                        }
                    }
                }
                catch (Authentication_Exception ae)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(ae));
                    throw;
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString("Error retrieving STAGE1_FUCNTIONAL_INFO", ex));
                    throw;
                }
                /// Stage 2 Functional Info Records
                //////////////////////////////////////////////////////////////////


                //////////////////////////////////////////////////////////////////
                /// Production Line Assembly
                try
                {
                    logger.Debug("Retrieving PRODUCTION_LINE_ASSEMBLY record for: {0}.", PTSerial);
                    ptAssembly = Manufacturing_PLA_Web_Service_Client_REST.Get_SubAssembly(InfrastructureModule.Token, PTSerial);
                }
                catch (Authentication_Exception ae)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(ae));
                    throw;
                }
                catch (Exception ex)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString("Error retrieving PRODUCTION_LINE_ASSEMBLY", ex));
                    throw;
                }


                if (ptAssembly != null)
                {
                    if (manufacture.Date_Time_Created == null) manufacture.Date_Time_Created = ptAssembly.Start_Date;

                    try
                    {
                        logger.Debug("Retrieving PRODUCTION_LINE_ASSEMBLY subassembly records for: {0}.", PTSerial);
                        ptSubAssemblies = Manufacturing_PLA_Web_Service_Client_REST.Select_Production_Line_Assembly_SERIAL_NUMBER(InfrastructureModule.Token, PTSerial);
                    }
                    catch (Authentication_Exception ae)
                    {
                        logger.Warn(Exception_Helper.FormatExceptionString(ae));
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(Exception_Helper.FormatExceptionString("Error retrieving PRODUCTION_LINE_ASSEMBLY - SubAssemblies", ex));
                        throw;
                    }
                    int motorCount = 0;
                    int lMotorID = 0;
                    int rMotorID = 0;

                    if (ptSubAssemblies != null)
                    {
                        logger.Debug("Checking each sub-assembly");
                        foreach (Production_Line_Assembly p in ptSubAssemblies)
                        {
                            if (p == null)
                            {
                                logger.Warn("Sub-Assembly list contained a null reference.");
                                continue;
                            }

                            if ((p.Part_Type == "MotorBlack") || (p.Part_Type == "MotorWhite"))
                            {
                                logger.Debug("Found a Motor: {0}", p);
                                if ((motorCount == 0) || ((motorCount >= 2) && (p.Replaces_ID == rMotorID)))
                                {
                                    p.Location = "Right";
                                    rMotorID = p.ID;
                                }
                                else if ((motorCount == 1) || ((motorCount >= 2) && (p.Replaces_ID == lMotorID)))
                                {
                                    p.Location = "Left";
                                    lMotorID = p.ID;
                                }
                                if (p.End_Date == null)
                                {
                                    if (p.Location == "Left")
                                    {
                                        manufacture.Motorl_Serial = p.Serial_Number;
                                        manufacture.Motorl_Date = p.Start_Date.Value;
                                        manufacture.Motorl_Type = p.Part_Type;
                                        manufacture.Motorl_User = p.Created_By;
                                    }
                                    else if (p.Location == "Right")
                                    {
                                        manufacture.Motorr_Serial = p.Serial_Number;
                                        manufacture.Motorr_Date = p.Start_Date.Value;
                                        manufacture.Motorr_Type = p.Part_Type;
                                        manufacture.Motorr_User = p.Created_By;
                                    }
                                }
                                motorCount++;
                            }
                            else if (p.End_Date == null)
                            {
                                logger.Debug("Found: {0}", p);

                                Dictionary<String, String> comp = new Dictionary<String, String>();
                                comp[Component_Labels.Serial_Number] = p.Serial_Number;
                                comp[Component_Labels.Model] = p.Model;
                                comp[Component_Labels.Installed] = p.Created_String;
                                comp[Component_Labels.Installed_By] = p.Created_By;
                                //components.Add(p.Part_Type, comp);
                                switch (p.Part_Type)
                                {
                                    case "UIC":
                                        if (manufacture.UIC_Serial != p.Serial_Number) manufacture.UIC_Serial = p.Serial_Number;
                                        if (manufacture.UIC_User != p.Created_By) manufacture.UIC_User = p.Created_By;
                                        if (manufacture.UIC_Date != p.Start_Date) manufacture.UIC_Date = p.Start_Date;
                                        break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    String msg = String.Format("Request for PRODUCTION_LINE_ASSEMBLY records returned a null for PT: {0}.", PTSerial);
                    logger.Error(msg);
                    if (IsError == true)
                    {
                        ErrorMsg += "\n" + msg;
                    }
                    else
                    {
                        ErrorMsg = msg;
                        IsError = true;
                    }
                }
                /// Production Line Assembly
                //////////////////////////////////////////////////////////////////

                //    configurations.AddRange(SART)

                //////////////////////////////////////////////////////////////////
                /// SART_PT_Configuration
                try
                {
                    logger.Debug("Retrieving SART_PT_Configuration records.");
                    List<SART_PT_Configuration> ptconfList = SART_PTCnf_Web_Service_Client_REST.Select_SART_PT_Configuration_SERIAL_NUMBER(InfrastructureModule.Token, PTSerial);
                    if (ptconfList != null)
                    {
                        foreach (SART_PT_Configuration ptc in ptconfList)
                        {
                            if (ptc != null)
                            {
                                if (ptc.Date_Time_Created.HasValue) key = String.Format("{0}/{1}", ptc.Date_Time_Created.Value.ToString("yyyy-MM-dd"), ptc.Type);
                                else if (ptc.Date_Time_Entered.HasValue) key = String.Format("{0}/{1}", ptc.Date_Time_Entered.Value.ToString("yyyy-MM-dd"), ptc.Type);
                                else key = String.Format("{0}/{1}", DateTime.MinValue.ToString("yyyy-MM-dd"), ptc.Type);
                                configurations[key] = ptc;
                                //if (configurations.ContainsKey(key) == false)
                                //{
                                //    configurations.Add(key, ptc);
                                //}
                            }
                        }
                    }
                }
                catch (Authentication_Exception ae)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(ae));
                    throw;
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString("Error retrieving SART_PT_Configuration", ex));
                    throw;
                }
                /// SART_PT_Configuration
                //////////////////////////////////////////////////////////////////

                //////////////////////////////////////////////////////////////////
                /// Service
                try
                {
                    logger.Debug("Retrieving SERVICE records.");
                    List<SART_Service> servList = Manufacturing_Service_Web_Service_Client_REST.Select_Service_UNIT_ID_SERIAL_NUMBER(InfrastructureModule.Token, PTSerial);
                    if (servList != null)
                    {

                        foreach (var serv in servList)
                        {
                            if (serv != null)
                            {
                                var ptc = new SART_PT_Configuration(serv);
                                key = Create_Key(ptc, "Service_Start");
                                if (configurations.ContainsKey(key) == true)
                                {
                                    ptc.Type = "Service_Start";
                                    configurations[key].Update(ptc);
                                }
                                else
                                {
                                    key = Create_Key(ptc, "Service_Final");
                                    if (configurations.ContainsKey(key) == true)
                                    {
                                        ptc.Type = "Service_Final";
                                        configurations[key].Update(ptc);
                                    }
                                    else
                                    {
                                        key = Create_Key(ptc, "Service");
                                        if (configurations.ContainsKey(key) == true) configurations[key].Update(ptc);
                                        else configurations[key] = ptc;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Authentication_Exception ae)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(ae));
                    throw;
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString("Error retrieving SERVICE records", ex));
                    throw;
                }
                /// Service
                //////////////////////////////////////////////////////////////////

#if DoNotDo
                //////////////////////////////////////////////////////////////////
                /// SART_Work_Order
                try
                {
                    logger.Debug("Retrieving SERVICE_TABLE records.");
                    ptServices = SART_2012_Web_Service_Client.Select_SART_Work_Order_PT_SERIAL(InfrastructureModule.Token, PTSerial);
                    //  ptServices = Manufacturing_Tables_Services_Client.Select_Service_Table_MACHINE_SN(ptserial);

                    if (ptServices != null)
                    {
                        foreach (SART_Work_Order swo in ptServices)
                        {
                            if (swo.Work_Order_ID != woID)
                            {
                                SART_2012_Web_Service_Client.Select_SART_WO_Components_WORK_ORDER_ID(InfrastructureModule.Token, swo.Work_Order_ID);
                            }
                        }
                    }
                }
                catch (Authentication_Exception ae)
                {
                    logger.Warn(Exception_Helper.FormatExceptionString(ae));
                    throw;
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString("Error retrieving SERVICE_TABLE", ex));
                    throw;
                }

                /// SART_Work_Order
                //////////////////////////////////////////////////////////////////
#endif
                var list = new List<SART_PT_Configuration>(configurations.Values);
                if (list.Count > 1) list.Sort(new SART_PT_Configuration_Created_Comparer());
                ((UnityContainer)InfrastructureModule.Container).RegisterInstance<List<SART_PT_Configuration>>("Configurations", list);
                return list;
            }
            catch (Authentication_Exception ae)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(ae));
                InfrastructureModule.Aggregator.GetEvent<Authentication_Failure_Event>().Publish(true);
                return new List<SART_PT_Configuration>();
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                InfrastructureModule.Aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Error occurred while retrieving configuration data.");
                return new List<SART_PT_Configuration>();
            }
        }

        private static String Convert_UIC_PartNumber(String pn)
        {
            if (String.IsNullOrEmpty(pn) == true) return pn;
            pn = Strings.HexDigits(pn);
            if (pn.Length == 8)
            {
                UInt16[] partnum = Converter.HexStringToUInt16Array(pn);
                return String.Format("{0:00000} {1:00000}", partnum[0], partnum[1]);
            }
            return pn;
        }

        private static Boolean GetServiceRecordContent(SART_Service serv, String field, ref String data)
        {
            PropertyInfo p = typeof(Service_Table).GetProperty(field);

            if (p == null)
            {
                Type t = typeof(Service_Table);
                PropertyInfo[] props = t.GetProperties();
                foreach (PropertyInfo prop in props)
                {
                    if (prop.Name.ToUpper() == field.ToUpper())
                    {
                        p = prop;
                        break;
                    }
                }
                if (p == null) return false;
            }
            data = (String)p.GetValue(serv, null);
            return !String.IsNullOrEmpty(data);
        }

        private static Dictionary<String, Dictionary<String, String>> CopyConfiguration(Dictionary<String, Dictionary<String, String>> config)
        {
            Dictionary<String, Dictionary<String, String>> copy = new Dictionary<String, Dictionary<String, String>>();
            foreach (String label in config.Keys)
            {
                copy.Add(label, CopyComponent(config[label]));
            }
            return copy;
        }

        private static Dictionary<String, String> CopyComponent(Dictionary<String, String> component)
        {
            Dictionary<String, String> newcomp = new Dictionary<string, string>();
            foreach (String label in component.Keys)
            {
                newcomp.Add(label, component[label]);
            }
            return newcomp;
        }


        public static List<Component_Info> Convert(Dictionary<String, String> ci)
        {
            List<Component_Info> ciList = new List<Component_Info>();
            foreach (String label in ci.Keys)
            {
                ciList.Add(new Component_Info(label, ci[label]));
            }
            return ciList;
        }

        private static String Convert_SWVersion(String ver)
        {
            if (String.IsNullOrEmpty(ver) == true) return ver;
            String[] parts = Strings.Split(ver, ' ');
            String retString = parts[0];
            for (int x = 1; x < parts.Length; x++)
            {
                retString += "  " + Converter.HexStringToUInt16(parts[x]).ToString();
            }
            return retString;
        }

        private static string Create_Key(SART_PT_Configuration ptc, String type)
        {
            if (ptc.Date_Time_Created.HasValue) return String.Format("{0}/{1}", ptc.Date_Time_Created.Value.ToString("yyyy-MM-dd"), type);
            if (ptc.Date_Time_Entered.HasValue) return String.Format("{0}/{1}", ptc.Date_Time_Entered.Value.ToString("yyyy-MM-dd"), type);
            return String.Format("{0}/{1}", DateTime.MinValue.ToString("yyyy-MM-dd"), type);
        }

        private static String Get_UIC_Serial(String data)
        {
            if (String.IsNullOrEmpty(data) == false)
            {
                int serial = 0;
                var parts = Strings.Split(data, ' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    try
                    {
                        serial = int.Parse(part, NumberStyles.HexNumber);
                        if (serial > 0) return String.Format("{0:X8}{0:X8}", serial);
                    }
                    catch
                    { }
                }
            }
            return "";
        }
    }
}
