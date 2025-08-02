using NLog;
using Segway.COFF.Objects;
using Segway.Database.Objects;
using Segway.Modules.SART_Infrastructure;
using Segway.Service.CAN;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.SART.Client.REST;
using Segway.Service.Tools.CAN2;
using Segway.Service.Tools.CAN2.Messages;
using Segway.Service.Tools.COFF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;

namespace Segway.Modules.Diagnostics_Helper
{
    public static class WatchManager
    {
        private static CAN2 can;
        private static List<WatchVariable> varsAList;
        private static List<WatchVariable> varsBList;
        private static Boolean Watching;
        private static BackgroundWorker canWorker;
        private static Boolean StopRequested;
        private static CUFrame cuaFrame;
        private static CUFrame cubFrame;
        private static Boolean IsInitialized;
        private static Logger logger = Logger_Helper.GetCurrentLogger();
        //private static IUnityContainer container;

        public static event WatchDataReadyEventHandler WatchDataReady;



        //#region Token

        //private static AuthenticationToken _Token;

        ///// <summary>Property Token of type AuthenticationToken</summary>
        //public static AuthenticationToken Token
        //{
        //    get
        //    {
        //        if (_Token == null)
        //        {
        //            if (container == null) throw new Exception("No Unity Container has been set");
        //            if (container.IsRegistered<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName) == true)
        //            {
        //                _Token = container.Resolve<AuthenticationToken>(AuthenticationToken.ApplicationGlobalInstanceName);
        //            }
        //        }
        //        return _Token;
        //    }
        //}

        //#endregion

        //#region LoginContext

        //private static Login_Context _LoginContext = null;
        ///// <summary>Property LoginContext of type Login_Context</summary>
        //public static Login_Context LoginContext
        //{
        //    get
        //    {
        //        if (_LoginContext == null)
        //        {
        //            if (Token != null) _LoginContext = Token.LoginContext;
        //        }
        //        return _LoginContext;
        //    }
        //}
        //#endregion




        public static Dictionary<string, Int16> CreateWatchVarsData(List<string> watchList)
        {
            Dictionary<string, Int16> watch = new Dictionary<string, Int16>();
            foreach (string watchvar in watchList)
            {
                watch[watchvar] = 0;
            }
            return watch;
        }

        private static void FrameCompletedHandler(FrameCompletedEventArgs args)
        {
            if (args == null)
            {
                logger.Warn("FrameCompletedEventArgs is Null");
                return;
            }

#if false
            FrameCompletedHandler_Background(args);
#else
            logger.Trace("Processing: {0}", args);
            Thread back = new Thread(new ParameterizedThreadStart(FrameCompletedHandler_Background));
            back.IsBackground = true;
            back.Start(args.Copy());
#endif
        }

        private static void FrameCompletedHandler_Background(object o)
        {
            if (o == null)
            {
                logger.Warn("FrameCompletedEventArgs is Null");
                return;
            }
            if (Watching == false)
            {
                logger.Trace("Not watching");
                return;
            }
            if (WatchDataReady == null)
            {
                logger.Trace("Delagate WatchDataReady has not been set");
                return;
            }

            if (varsAList.Count == 0)
            {
                logger.Warn("No variable list data");
                return;
            }
            try
            {
                //logger.Trace("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                FrameCompletedEventArgs args = (FrameCompletedEventArgs)o;
                logger.Trace("Processing: {0}", args);

                CUFrameData cuFData = args.Data;
                if (cuFData == null)
                {
                    logger.Warn("No data");
                    return;
                }
                logger.Trace("Processing {0}-Side message", args.Side);

                Dictionary<String, Int16> variables = new Dictionary<String, Int16>();
                for (int nIndex = 0; (nIndex < varsAList.Count) && (nIndex < cuFData.Data.Length); nIndex++) // _nSlot; nIndex = (ushort)(nIndex + 1))
                {
                    String name = varsAList[nIndex].Name;
                    Int16 data = cuFData.Data[nIndex];
                    //logger.Trace("Variable Name: {0}, Data: {1}", name, data);
                    variables[name] = data;
                }
                WatchDataReadyEventArgs wd = new WatchDataReadyEventArgs(args.Side, variables);
                if (wd == null)
                {
                    logger.Warn("Watch Data is null");
                    return;
                }
                WatchDataReady(wd);
                return;
            }
            catch (Exception exception)
            {
                string msg = Exception_Helper.FormatExceptionString(exception);
                logger.Error(msg);
            }
            //finally
            //{
            //    //logger.Trace("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            //}
        }

        public static int GetWatchVariableAddress(string variable)
        {
            return (int)COFF_File.ResolveValue(variable);
        }

        public static Boolean Initialize(CAN2 can)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                //container = c;
                if (IsInitialized == true)
                {
                    varsAList = new List<WatchVariable>();
                    varsBList = new List<WatchVariable>();
                    cuaFrame = new CUFrame(CAN_CU_Sides.A);
                    cubFrame = new CUFrame(CAN_CU_Sides.B);
                    return true;
                }
                WatchManager.can = can;
                COFF_Descriptor cd = new COFF_Descriptor(PT_Generations.Gen2, PT_Component.CU, PT_Models.I2, PT_Code_Type.Application);
                if (LoadCoffFile(cd) == false)
                {
                    logger.Error("Unable to load COFF File: {0}", cd);
                    IsInitialized = false;
                    return false;
                }
                varsAList = new List<WatchVariable>();
                varsBList = new List<WatchVariable>();
                cuaFrame = new CUFrame(CAN_CU_Sides.A);
                cubFrame = new CUFrame(CAN_CU_Sides.B);
                cuaFrame.FrameCompleted += new FrameCompletedEventHandler(FrameCompletedHandler);
                cubFrame.FrameCompleted += new FrameCompletedEventHandler(FrameCompletedHandler);
                IsInitialized = true;
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


#if false
        public static Boolean LoadCoffFile(Boolean ForceLoad = false)
        {
            try
            {
                logger.Debug("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                COFF_Descriptor key = new COFF_Descriptor(PTGeneration.Gen2, "CU", PT_Model_Types.I2, PT_Code_Type.Application);
                logger.Debug("Key: {0}", key);
                if (CAN2_Commands.Loaded_COFF_Files.ContainsKey(key.Description) == true)
                {
                    logger.Debug("Already have retrieved COFF: {0}", key);
                    return COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key);
                }
                else if ((ForceLoad == true) || (COFF_File.Test_Descriptors(key) == false))
                {
                    logger.Debug("Retrieving COFF: {0}", key);
                    SqlBooleanCriteria criteria = COFF_Descriptor.Create_Criteria(key);
                    //SqlBooleanList criteria = new SqlBooleanList();
                    //criteria.Add(new FieldData("Generation", PTGeneration.Gen2.ToString()));
                    //criteria.Add(new FieldData("Component", "CU"));
                    //criteria.Add(new FieldData("Model", "I2"));
                    //criteria.Add(new FieldData("Type", "Application"));
                    List<SART_COFF_Files> coffList = SART_2012_Web_Service_Client.Select_SART_COFF_Files_Criteria(Token, criteria);
                    if (coffList == null || coffList.Count == 0)
                    {
                        return false;
                    }
                    SART_COFF_Files sARTCOFFFile = coffList[coffList.Count - 1];
                    CAN2_Commands.Loaded_COFF_Files[key.Description] = sARTCOFFFile.Data;
                    return COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key);
                }
                return true;
            }
            catch (AuthenticationNull_Exception authenticationNullException)
            {
                logger.Warn(Exception_Helper.FormatExceptionString(authenticationNullException));
                throw;
            }
            catch (Authentication_Exception authenticationException)
            {
                logger.Error(Exception_Helper.FormatExceptionString(authenticationException));
                throw;
            }
            catch (Exception exception)
            {
                logger.Error(Exception_Helper.FormatExceptionString(exception));
                throw;
            }
            finally
            {
                logger.Debug("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }
#else
        public static Boolean LoadCoffFile(COFF_Descriptor key = null)
        {
            try
            {
                logger.Debug($"Entered - {MethodBase.GetCurrentMethod().Name}");

                if (key == null) key = new COFF_Descriptor(PT_Generations.Gen2, PT_Component.CU, PT_Models.I2, PT_Code_Type.Application);
                if (CAN2_Commands.Loaded_COFF_Files.ContainsKey(key.Description) == false)
                {
                    SqlBooleanCriteria criteria = SqlBooleanCriteria.Create_Criteria(key);

                    List<SART_COFF_Files> coffList = SART_COFF_Web_Service_Client_REST.Select_SART_COFF_Files_Criteria(InfrastructureModule.Token, criteria);
                    if ((coffList == null) || (coffList.Count == 0))
                    {
                        //aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to retrieve COFF file");
                        return false;
                    }
                    CAN2_Commands.CU_COFF = coffList[coffList.Count - 1];
                    CAN2_Commands.Loaded_COFF_Files[key.Description] = CAN2_Commands.CU_COFF.Data;
                    if (COFF_File.Load(CAN2_Commands.CU_COFF.Data, key) == false)
                    {
                        //aggregator.GetEvent<StatusBar_Region1_Event>().Publish("Unable to load COFF file");
                        return false;
                    }
                    return true;
                }
                return COFF_File.Load(CAN2_Commands.Loaded_COFF_Files[key.Description], key);
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
#endif

        public static void Flush_CAN()
        {
            logger.Debug("Start Flush");
            CAN2_Message msgA = null;
            CAN2_Message msgB = null;

            while (true)
            {
                msgA = can.Receive(CAN_CU_Sides.A);
                msgB = can.Receive(CAN_CU_Sides.B);
                if ((msgA == null) && (msgB == null)) break;
            }
            logger.Debug("End Flush");
        }

        private static void ProcessCANMessages(object sender, DoWorkEventArgs args)
        {
            logger.Info("Entering ProcessCANMessages Thread (Reading Messages)");
            CAN2_Message msgA;
            CAN2_Message msgB;
#if Count_Nulls
            int AReceived = 0;
            int BReceived = 0;
            int ANull = 0;
            int BNull = 0;
#endif

            while (!StopRequested)
            {
                msgA = can.Receive(CAN_CU_Sides.A);
#if Count_Nulls
                if (msgA != null)
                {
                    if (++AReceived == 500)
                    {
                        logger.Trace("Received 500 A-Messages");
                        AReceived = 0;
                    }
                    logger.Trace("Received: {0}", msgA);
                    cuaFrame.CompleteFrameFromCANMessage(msgA.Copy());
                }
                else if (++ANull == 50)
                {
                    logger.Trace("Received 50 A-Null");
                    ANull = 0;
                }
#else
                if (msgA != null)
                {
                    logger.Trace("Received: {0}", msgA);
                    cuaFrame.CompleteFrameFromCANMessage(msgA.Copy());
                }
#endif

                msgB = can.Receive(CAN_CU_Sides.B);
#if Count_Nulls
                if (msgB != null)
                {
                    if (++BReceived == 500)
                    {
                        logger.Trace("Received 500 B-Messages");
                        BReceived = 0;
                    }
                    logger.Trace("Received: {0}", msgB);
                    cubFrame.CompleteFrameFromCANMessage(msgB.Copy());
                }
                else if (++BNull == 50)
                {
                    logger.Trace("Received 50 B-Null");
                    BNull = 0;
                }
#else
                if (msgB != null)
                {
                    logger.Trace("Received: {0}", msgB);
                    cubFrame.CompleteFrameFromCANMessage(msgB.Copy());
                }
#endif

                //Thread.Sleep(1);
            }
            logger.Info("Leaving ProcessCANMessages Thread");
        }

        public static void ResetDataContainersForNewTest()
        {
        }

        private static Boolean ResetWatchSlots()
        {
            if (Watching)
            {
                return false;
            }
            logger.Debug("Resetting watch slot points....");
            //_nSlot = -1;
            varsAList.Clear();
            varsBList.Clear();
            return true;
        }

        public static void Setup_Watch_Variables(Dictionary<string, Int16> watchA, Dictionary<string, Int16> watchB, List<string> watchList)
        {
            watchA.Clear();
            watchB.Clear();
            if (watchList != null)
            {
                foreach (string watchvar in watchList)
                {
                    SetWatchForVariable(watchvar, 0);
                    watchA[watchvar] = 0;
                    watchB[watchvar] = 0;
                    Thread.Sleep(10);
                }
            }
        }

        public static Boolean SetWatchForVariable(string variable, int addr = 0)
        {
            //bool flag;
            if ((COFF_File.Symbols == null) || (COFF_File.Symbols.Count == 0))
            {
                throw new PTIException("WatchManager is not initialized.");
            }

            if (Watching)
            {
                throw new PTIException("Unable to set a watch. Watch is already in process.");
            }

            //if (_nSlot < -1)
            //{
            //    throw new PTIException("Unable to set a watch. Invalid slot number (Below the minimum limit).");
            //}

            //if (_nSlot >= 15)
            //{
            //    throw new PTIException("Unable to set a watch. Invalid slot number (Maximum limit (16) reached).");
            //}


            try
            {
                int address = (addr == 0 ? (int)COFF_File.ResolveValue(variable) : addr);
                if (address > 0)
                {
                    logger.Debug<string, int>("Symbol '{0}' resolved. Address: {1} (x{1:X4})", variable, address);
                    //_nSlot = _nSlot + 1;
                    if (CU_Set_Watch.Send_CU_Set_Watch(can, varsAList.Count, address))
                    {
                        varsAList.Add(new WatchVariable(variable, address));
                        varsBList.Add(new WatchVariable(variable, address));
                        Thread.Sleep(20);
                        return true;
                    }
                    else
                    {
                        logger.Error("Failed to set watch '{0}", variable);
                        return false;
                    }
                }
                else
                {
                    logger.Error("Failed to resolve symbol '{0}", variable);
                    return false;
                }
            }
            catch (Exception exception)
            {
                //_nSlot = _nSlot - 1;
                throw exception;
            }
            finally
            {
            }
            //return flag;
        }

        public static void Start()
        {
            if (IsInitialized == false)
            {
                throw new PTIException("WatchManager is not initialized.");
            }
            Watching = true;
            StopRequested = false;
            canWorker = new BackgroundWorker();
            canWorker.DoWork += new DoWorkEventHandler(ProcessCANMessages);
            canWorker.RunWorkerAsync();
        }

        public static void Stop()
        {
            if (Watching)
            {
                Watching = false;
                StopRequested = true;
                IsInitialized = false;
                cuaFrame.FrameCompleted -= new FrameCompletedEventHandler(FrameCompletedHandler);
                cubFrame.FrameCompleted -= new FrameCompletedEventHandler(FrameCompletedHandler);
                ResetWatchSlots();
                Thread.Sleep(1000);
            }
        }

    }
}




