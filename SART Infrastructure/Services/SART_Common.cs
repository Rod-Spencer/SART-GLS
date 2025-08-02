using Microsoft.Practices.Unity;
using NLog;
using Segway.Attributes;
using Segway.Manufacturing.Objects;
using Segway.SART.Objects;
using Segway.Service.Authentication.Objects;
using Segway.Service.CAN;
using Segway.Service.Common;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.Manufacturing.Client.REST;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using Segway.Syteline.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Segway.Modules.SART_Infrastructure
{
    public class SART_Common
    {
        private static Thread WatchQueueThread = null;
        private const String SavedInsertQueueName = "Insert Queued SART Event Logs.xml";
        private const String SavedUpdateQueueName = "Update Queued SART Event Logs.xml";

        private static Logger logger = Logger_Helper.GetCurrentLogger();
        private static Dictionary<String, SART_Event_Log_Entry> EntryLog = new Dictionary<string, SART_Event_Log_Entry>();
        private static Boolean Stop_Watch = false;


        #region Insert_Entry_Log_Queue

        private static Queue<SART_Event_Log_Entry> _Insert_Entry_Log_Queue;

        /// <summary>Property EntryLogQueue of type Queue<SART_Event_Log_Entry></summary>
        public static Queue<SART_Event_Log_Entry> Insert_Entry_Log_Queue
        {
            get
            {
                if (_Insert_Entry_Log_Queue == null)
                {
                    _Insert_Entry_Log_Queue = new Queue<SART_Event_Log_Entry>();
                    Start_Insert_Watch_Thread();
                }
                return _Insert_Entry_Log_Queue;
            }
        }

        #endregion


        #region Update_Entry_Log_Queue

        private static Queue<SART_Event_Log_Entry> _Update_Entry_Log_Queue;

        /// <summary>Property Update_Entry_Log_Queue of type Queue<SART_Event_Log_Entry></summary>
        public static Queue<SART_Event_Log_Entry> Update_Entry_Log_Queue
        {
            get
            {
                if (_Update_Entry_Log_Queue == null)
                {
                    _Update_Entry_Log_Queue = new Queue<SART_Event_Log_Entry>();
                    Start_Update_Watch_Thread();
                }
                return _Update_Entry_Log_Queue;
            }
        }

        #endregion


        public static CAN2 EstablishConnection(int objID)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                SART_Event_Log_Entry log = new SART_Event_Log_Entry();
                log.Timestamp_Start = DateTime.Now;
                log.Object_ID = objID;
                log.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                log.EventStatus = Event_Statuses.In_Progress;
                log.Message = "Establishing Connection to CAN Adapter";
                logger.Debug(log.Message);

                Insert_Entry_Log_Queue.Enqueue(log);
                //log = SART_2012_Web_Service_Client.Insert_SART_Event_Log_Entry_Key(InfrastructureModule.Token, log);
                InfrastructureModule.Aggregator.GetEvent<SART_EventLog_Add_Event>().Publish(log);


                CAN2 can = new CAN2_Systec();
                if (can.Initialize() == false)
                {
                    logger.Warn("CAN Initialization failed");
                    log.EventStatus = Event_Statuses.Failed;
                    log.Error_Description = String.Format("CAN Error: {0}", AttributeEnum.GetStringValue((Systec_Err_Codes)CAN2_Systec.LastError));
                    return null;
                }
                else
                {
                    log.EventStatus = Event_Statuses.Passed;
                }
                log.Timestamp_End = DateTime.Now;
                InfrastructureModule.Aggregator.GetEvent<SART_EventLog_Update_Event>().Publish(log);
                Update_Entry_Log_Queue.Enqueue(log);
                //SART_2012_Web_Service_Client.Update_SART_Event_Log_Entry_Key(InfrastructureModule.Token, log);

                return can;
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

        public static void ClosingConnection(int objID, CAN2 can)
        {
            try
            {
                logger.Debug("Entered");
                SART_Event_Log_Entry log = new SART_Event_Log_Entry();
                log.Message = "Closing Connection to CAN Adapter";
                log.Object_ID = objID;
                log.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                Insert_Entry_Log_Queue.Enqueue(log);
                //log = SART_2012_Web_Service_Client.Insert_SART_Event_Log_Entry_Key(InfrastructureModule.Token, log);
                InfrastructureModule.Aggregator.GetEvent<SART_EventLog_Add_Event>().Publish(log);

                if (can == null)
                {
                    log.EventStatus = Event_Statuses.Failed;
                }
                else if (can.Close() == true)
                {
                    log.EventStatus = Event_Statuses.Passed;
                }
                else
                {
                    log.EventStatus = Event_Statuses.Failed;
                }
                log.Timestamp_End = DateTime.Now;
                Update_Entry_Log_Queue.Enqueue(log);
                InfrastructureModule.Aggregator.GetEvent<SART_EventLog_Update_Event>().Publish(log);
                //SART_2012_Web_Service_Client.Update_SART_Event_Log_Entry_Key(InfrastructureModule.Token, log);
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
                logger.Debug("Leaving");
            }
        }

        public static void Start_Process(CAN_Processes canProc)
        {
            if (InfrastructureModule.Container.IsRegistered<int>("ObjectID") == false) throw new Exception("An instance of ObjectID has not been registered");
            int objID = InfrastructureModule.Container.Resolve<int>("ObjectID");
            SART_Event_Log_Entry log = Create_Entry_Log(canProc, objID);
            EntryLog[canProc.ToString()] = log;
        }

        public static SART_Event_Log_Entry Create_Entry_Log(CAN_Processes canProc, int objID)
        {
            try
            {
                logger.Debug("Creating Entry Log: {0}", CAN_StringEnum.GetStringValue(canProc));

                SART_Event_Log_Entry log = new SART_Event_Log_Entry();
                log.Timestamp_Start = DateTime.Now;
                log.Object_ID = objID;
                log.Work_Order = InfrastructureModule.Current_Work_Order.Work_Order_ID;
                log.EventStatus = Event_Statuses.In_Progress;
                log.Message = CAN_StringEnum.GetStringValue(canProc);
                Insert_Entry_Log_Queue.Enqueue(log);
                InfrastructureModule.Aggregator.GetEvent<SART_EventLog_Add_Event>().Publish(log);

                return log;
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
        }

        public static void End_Process(CAN_Processes canProc, Boolean pass, String msg = null)
        {
            if (EntryLog == null)
            {
                logger.Warn("Entry Log container is null - Creating a new one");
                EntryLog = new Dictionary<string, SART_Event_Log_Entry>();
                return;
            }
            if (EntryLog.ContainsKey(canProc.ToString()) == false) throw new Exception(String.Format("Missing process entry: {0}", canProc));

            SART_Event_Log_Entry log = EntryLog[canProc.ToString()];
            log.Error_Description = msg;
            Update_Entry_Log(pass, log);
            EntryLog.Remove(canProc.ToString());
        }

        public static void Update_Entry_Log(Boolean pass, SART_Event_Log_Entry log)
        {
            try
            {
                logger.Debug("Updating Entry Log: {0}", log.Message);

                if (pass == false)
                {
                    log.EventStatus = Event_Statuses.Failed;
                }
                else
                {
                    log.EventStatus = Event_Statuses.Passed;
                }
                log.Timestamp_End = DateTime.Now;
                InfrastructureModule.Aggregator.GetEvent<SART_EventLog_Update_Event>().Publish(log);

                Update_Entry_Log_Queue.Enqueue(log);
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
            //finally
            //{
            //    logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            //}
        }


        public static void Display_Process(Extraction_Components comp, String data)
        {
            switch (comp)
            {
                case Extraction_Components.BSAA:
                    InfrastructureModule.Aggregator.GetEvent<SART_Configuration_BSAA_Display_Event>().Publish(data);
                    break;
                case Extraction_Components.BSAB:
                    InfrastructureModule.Aggregator.GetEvent<SART_Configuration_BSAB_Display_Event>().Publish(data);
                    break;
                case Extraction_Components.CUA:
                    InfrastructureModule.Aggregator.GetEvent<SART_Configuration_CUA_Display_Event>().Publish(data);
                    break;
                case Extraction_Components.CUB:
                    InfrastructureModule.Aggregator.GetEvent<SART_Configuration_CUB_Display_Event>().Publish(data);
                    break;
                case Extraction_Components.UISID:
                    InfrastructureModule.Aggregator.GetEvent<SART_Configuration_UISID_Display_Event>().Publish(data);
                    break;
                case Extraction_Components.UICSerial:
                    InfrastructureModule.Aggregator.GetEvent<SART_Configuration_UICSerial_Display_Event>().Publish(data);
                    break;
            }
        }

        public static Boolean Calibration_Process(BSA_Calibration_Data calData)
        {
            if (calData == null) return false;
            logger.Debug("Inserting BSA Calibration Data for BSA: {0}", calData.BSA_Serial_Number);
            calData = SART_BSA_Web_Service_Client_REST.Insert_BSA_Calibration_Data_Key(InfrastructureModule.Token, calData);
            if (calData == null) return false;
            return true;
        }

        public static Boolean Create_Event(WorkOrder_Events woEvent, Event_Statuses status, int objectID = 0, String message = null, String WO = null)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                String workOrder = String.IsNullOrEmpty(WO) == true ? InfrastructureModule.Current_Work_Order.Work_Order_ID : WO;
                //Login_Context loginInfo = (Login_Context)InfrastructureModule.Container.Resolve<Login_Context_Interface>(Login_Context.Name);
                SART_Events logevent = new SART_Events();
                logevent.EventType = woEvent;
                logevent.Object_ID = objectID;
                logevent.Work_Order_ID = workOrder;
                logevent.User_Name = InfrastructureModule.Token.LoginContext.UserName;
                logevent.Timestamp = DateTime.Now;
                logevent.StatusType = status;
                logevent.Message = message;
                logevent = SART_Events_Web_Service_Client_REST.Insert_SART_Events_Key(InfrastructureModule.Token, logevent);
                if (logevent == null)
                {
                    logger.Error("Could not submit work order event: {0} for WO: {1} of status: {2}", woEvent, workOrder, status);
                    return false;
                }
                else
                {
                    logger.Info("Submitted work order event: {0} for WO: {1} of status: {2}", woEvent, workOrder, status);
                    return true;
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


        private static void Start_Insert_Watch_Thread()
        {
            if (WatchQueueThread == null)
            {
                WatchQueueThread = new Thread(new ThreadStart(WatchEntryLogQueues));
                WatchQueueThread.IsBackground = true;
                WatchQueueThread.Start();
            }

            FileInfo filequeue = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "Auto Save", SavedInsertQueueName));
            if (filequeue.Directory.Exists == false)
            {
                filequeue.Directory.Create();
            }
            else if (filequeue.Exists == true)
            {
                List<SART_Event_Log_Entry> entries = Serialization.DeserializeFromFile<List<SART_Event_Log_Entry>>(filequeue.FullName);
                if ((entries != null) && (entries.Count > 0))
                {
                    foreach (var entry in entries)
                    {
                        Insert_Entry_Log_Queue.Enqueue(entry);
                    }
                }
                filequeue.Delete();
            }
        }

        private static void Start_Update_Watch_Thread()
        {
            if (WatchQueueThread == null)
            {
                WatchQueueThread = new Thread(new ThreadStart(WatchEntryLogQueues));
                WatchQueueThread.IsBackground = true;
                WatchQueueThread.Start();
            }

            FileInfo filequeue = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "Auto Save", SavedUpdateQueueName));
            if (filequeue.Directory.Exists == false)
            {
                filequeue.Directory.Create();
            }
            else if (filequeue.Exists == true)
            {
                List<SART_Event_Log_Entry> entries = Serialization.DeserializeFromFile<List<SART_Event_Log_Entry>>(filequeue.FullName);
                if ((entries != null) && (entries.Count > 0))
                {
                    foreach (var entry in entries)
                    {
                        Update_Entry_Log_Queue.Enqueue(entry);
                    }
                }
                filequeue.Delete();
            }
        }

        public static void Stop_Watch_Thread()
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                Stop_Watch = true;
                Thread.Sleep(1000);
                if (WatchQueueThread == null) return;

                WatchQueueThread.Abort();
                WatchQueueThread.Join();

                FileInfo filequeue = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "Auto Save", SavedInsertQueueName));
                if (filequeue.Directory.Exists == false)
                {
                    filequeue.Directory.Create();
                }
                else if (filequeue.Exists == true)
                {
                    filequeue.Delete();
                }
                if ((Insert_Entry_Log_Queue != null) && (Insert_Entry_Log_Queue.Count > 0))
                {
                    logger.Debug("Saving \"Insert\" Queue data");
                    List<SART_Event_Log_Entry> entries = new List<SART_Event_Log_Entry>(Insert_Entry_Log_Queue.ToArray());
                    Serialization.SerializeToFile<List<SART_Event_Log_Entry>>(entries, filequeue);
                }


                filequeue = new FileInfo(Path.Combine(Application_Helper.Application_Folder_Name(), "Auto Save", SavedUpdateQueueName));
                if (filequeue.Directory.Exists == false)
                {
                    filequeue.Directory.Create();
                }
                else if (filequeue.Exists == true)
                {
                    filequeue.Delete();
                }
                if ((Update_Entry_Log_Queue != null) && (Update_Entry_Log_Queue.Count > 0))
                {
                    logger.Debug("Saving \"Update\" Queue data");
                    List<SART_Event_Log_Entry> entries = new List<SART_Event_Log_Entry>(Update_Entry_Log_Queue.ToArray());
                    Serialization.SerializeToFile<List<SART_Event_Log_Entry>>(entries, filequeue);
                }
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
            finally
            {
                WatchQueueThread = null;
                _Insert_Entry_Log_Queue = null;
                _Update_Entry_Log_Queue = null;
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }


        private static void WatchEntryLogQueues()
        {
            logger.Info("Entered - {0}", MethodBase.GetCurrentMethod().Name);
            Stop_Watch = false;
            while (Stop_Watch == false)
            {
                try
                {
                    // If anything is in the "Insert" queue, then send that to the server
                    if (Insert_Entry_Log_Queue.Count > 0)
                    {
                        // Get the next item from the queue, but don't remove it.
                        var log = Insert_Entry_Log_Queue.Peek();
                        logger.Debug("Got the next item from the \"Insert\" queue");

                        // Insert log to Segway database
                        var newlog = SART_EVLOG_Web_Service_Client_REST.Insert_SART_Event_Log_Entry_Key(InfrastructureModule.Token, log);
                        if (newlog != null)
                        {
                            logger.Debug("Successfully inserted to the Segway database");
                            // Successfully updated the database, therefore, it can be removed from the queue.
                            Insert_Entry_Log_Queue.Dequeue();
                            log.ID = newlog.ID;
                        }
                    }
                    // If the "Insert" queue is empty, then send anything in the "Update" queue
                    else if (Update_Entry_Log_Queue.Count > 0)
                    {
                        // Get the next item from the queue, but don't remove it.
                        var log = Update_Entry_Log_Queue.Peek();
                        logger.Debug("Got the next item from the \"Update\" queue");

                        // Update log to Segway database
                        if (SART_EVLOG_Web_Service_Client_REST.Update_SART_Event_Log_Entry_Key(InfrastructureModule.Token, log) != null)
                        {
                            // Successfully updated the database, therefore, it can be removed from the queue.
                            Update_Entry_Log_Queue.Dequeue();
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(Exception_Helper.FormatExceptionString(ex));
                }

                Thread.Sleep(200);
            }
            logger.Warn("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
        }

        public static void Initialize_Timeouts_and_Delays()
        {
            CAN2_Commands.Delay_Wake_Start = InfrastructureModule.Settings.Delay_Wake_Start;
            CAN2_Commands.Delay_Wake_Start_Wireless = InfrastructureModule.Settings.Delay_Wake_Start_Wireless;
            CAN2_Commands.Delay_Diagnostic_Wakeup = InfrastructureModule.Settings.Delay_Diagnostic_Wakeup;
            CAN2_Commands.Delay_Full_Stop = InfrastructureModule.Settings.Delay_Full_Stop;
            CAN2_Commands.Timeout_Heartbeat = InfrastructureModule.Settings.Timeout_Heartbeat;
            CAN2_Commands.Timeout_Start_Applet = InfrastructureModule.Settings.Timeout_Start_Applet;
            CAN2_Commands.Timeout_Start_Applet_Response = InfrastructureModule.Settings.Timeout_Start_Applet_Response;
            CAN2_Commands.Timeout_BSA_SPI_Enter_Boot = InfrastructureModule.Settings.Timeout_BSA_SPI_Enter_Boot;
            CAN2_Commands.Timeout_Enter_Diagnostic_Mode = InfrastructureModule.Settings.Timeout_Enter_Diagnostic_Mode;
            CAN2_Commands.Delay_LEDTest_Wakeup = InfrastructureModule.Settings.Delay_LEDTest_Wakeup;
        }


        public static Security Get_Security_Data(String serial)
        {
            var secList = Manufacturing_Security_Web_Service_Client_REST.Select_Security_UNIT_ID_SERIAL_NUMBER(InfrastructureModule.Token, serial);
            if ((secList == null) || (secList.Count == 0))
            {
                throw new Exception("Request for security information returned a null or empty list");
            }
            Security sec = new Security();
            foreach (Security secRec in secList)
            {
                sec.Update(secRec);
            }

            return sec;
        }


        public static void Write_Picture_To_Cache(FileInfo fi, Seg_SART_Pictures pic)
        {
            try
            {
                logger.Debug("Entered");
                if (fi == null) throw new Exception("Parameter fi (FileInfo) can not be null");
                if (pic == null) throw new Exception("Parameter pic (Seg_SART_Pictures) can not be null");
                if (pic.Picture_Data == null) return;

                if (fi.Exists == true) fi.Delete();
                FileStream fs = fi.OpenWrite();
                fs.Write(pic.Picture_Data, 0, pic.Picture_Data.Length);
                fs.Close();
                fi.Refresh();
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
            }
            finally
            {
                logger.Debug("Leaving");
            }
        }


        public static FileInfo Format_Cache_Filename(Seg_SART_Pictures_Nodata pic)
        {
            if (pic == null) throw new Exception("Parameter pic (Seg_SART_Pictures) can not be null");

            DirectoryInfo di = new DirectoryInfo(Application_Helper.Application_Folder_Name());
            di = new DirectoryInfo(Path.Combine(di.FullName, "Cache"));
            if (di.Exists == false) di.Create();
            if (Path.GetExtension(pic.Unique_Name) == String.Empty)
            {
                pic.Unique_Name += Path.GetExtension(pic.Name);
            }
            FileInfo fi = new FileInfo(Path.Combine(di.FullName, pic.Unique_Name));
            return fi;
        }

        public static List<Seg_SART_Pictures_Nodata> Add_Picture_To_List(List<Seg_SART_Pictures_Nodata> currList, Seg_SART_Pictures_Nodata select)
        {
            currList.Add(select);
            currList.Sort(new Seg_SART_Pictures_Nodata_CREATE_DATE_Comparer());
            return currList;
        }


        public static void Upload_Picture_Back(object obj)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);

                SART_Picture_Data pd = (SART_Picture_Data)obj;

                SART_Pictures_Web_Service_Client_REST.Upload_SART_Picture_Data(InfrastructureModule.Token, pd.RowPointer, pd.PictureData);
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


        public static String Format_Work_Order_ID(String sronum)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                if (String.IsNullOrEmpty(sronum)) return String.Empty;
                if (sronum.Contains("*") == true) return sronum;
                if (sronum.Contains("%") == true) return sronum;

                String alpha = Strings.Alpha(sronum).ToUpper();
                String numbers = Strings.Numeric(sronum);
                if (numbers.Length > 0) numbers = int.Parse(numbers).ToString();
                if (alpha.Length == 0) alpha = "S";
                while ((alpha.Length + numbers.Length) < 10) numbers = "0" + numbers;
                return alpha + numbers;
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
