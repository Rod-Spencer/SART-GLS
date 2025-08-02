using NLog;
using Segway.Modules.SART_Infrastructure;
using Segway.SART.Objects;
using Segway.Service.CAN;
using Segway.Service.CAN.Objects;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Extract_Services</summary>
    public class Black_Box_Extract_Services
    {
        /// <summary>Protected Member - logger</summary>
        protected static Logger logger = Logger_Helper.GetCurrentLogger();
        /// <summary>Public Property - Error_Message</summary>
        public static String Error_Message { get; set; }

        private static Black_Box_Data_Display_List BBDList = new Black_Box_Data_Display_List();


        /// <summary>Public Static Method - Extract</summary>
        /// <param name="side">CAN_CU_Sides</param>
        /// <param name="jtags">JTag_Data</param>
        /// <param name="obj">SART_Event_Object</param>
        /// <returns>Guid?</returns>
        public static Guid? Extract(CAN_CU_Sides side, JTag jtags, SART_Event_Object obj)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                logger.Debug("Establishing CAN connection");
                CAN2 can = SART_Common.EstablishConnection(obj.ID);
                if (can == null)
                {
                    logger.Warn("Establishing CAN connection - Failed");
                    return null;
                }
                try
                {
                    if (CAN2_Commands.Continue_Processing == true)
                    {
                        logger.Debug("Loading BSA code");
                        /////////////////////////////////////////////////////////////////////////////////
                        // BSA Load Code
                        BSA_Black_Box bb = CAN2_Commands.BSA_Extract_BlackBox(can, jtags, side, SART_Common.Start_Process, SART_Common.End_Process, UploadBlackBoxProcess);
                        if (bb == null)
                        {
                            logger.Warn("Loading BSA code - Failed");
                            return null;
                        }
                        logger.Debug("Loading BSA code - Successful");

                        bb.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                        bb.Unit_ID_Serial_Number = InfrastructureModule.Current_Work_Order.PT_Serial;
                        bb.User_Name = InfrastructureModule.Token.LoginContext.UserName;

                        bb = SART_BBB_Web_Service_Client_REST.Insert_BSA_Black_Box_Key(InfrastructureModule.Token, bb);
                        if (bb == null) throw new Exception("Unable to save Black Box record");
                        //bb.Blackbox_Key = Guid.NewGuid();
                        return bb.Black_Box_Key;
                        // BSA Load Code
                        /////////////////////////////////////////////////////////////////////////////////
                    }

                    return null;
                }
                finally
                {
                    logger.Debug("Closing CAN connection");
                    SART_Common.ClosingConnection(obj.ID, can);
                }
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



        /// <summary>Public Static Method - UploadBlackBoxProcess</summary>
        /// <param name="bbHeader">BSA_Black_Box_Header</param>
        /// <param name="bbData">List&lt;BSA_Black_Box_Data&gt;</param>
        /// <returns>Boolean</returns>
        public static Boolean UploadBlackBoxProcess(BSA_Black_Box_Header bbHeader, List<BSA_Black_Box_Data> bbData)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                if (bbHeader == null) throw new ArgumentNullException("Parameter bbHeader (BSA_Black_Box_Header) can not be null.");
                if (bbData == null) throw new ArgumentNullException("Parameter bbData (List<BSA_Black_Box_Data>) can not be null.");

                logger.Debug("Inserting Black Box Header: {0}", bbHeader.Header_Key);
                if (SART_BBBH_Web_Service_Client_REST.Insert_BSA_Black_Box_Header_Key(InfrastructureModule.Token, bbHeader) == null)
                {
                    logger.Warn("Inserting Black Box Header: {0} - Failed", bbHeader.Header_Key);
                    return false;
                }

                logger.Debug("Inserting Black Box Data - count={0}", bbData.Count);
                var dataList = SART_BBBD_Web_Service_Client_REST.Insert_BSA_Black_Box_Data_ObjList(InfrastructureModule.Token, bbData);
                if (dataList == null)
                {
                    logger.Warn("Inserting Black Box Data - Failed");
                    return false;
                }

                foreach (var data in dataList)
                {
                    if (data == null) return false;
                    if (data.ID == 0) return false;
                }
                return true;
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
    }
}
