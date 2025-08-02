using NLog;
using Segway.Service.CAN;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using Segway.Service.Tools.CAN2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Segway.Modules.Diagnostics_Helper
{

    public class DiagDataManager : INotifyPropertyChanged
    {
        private Dictionary<String, Int16> _watchVarsA = null;
        private Dictionary<String, Int16> _watchVarsB = null;
        //private Thread dataThread;
        private Boolean _bStopRequested = false;
        //private WatchManager _WatchManager = null;

        private static Logger logger = Logger_Helper.GetCurrentLogger();

        // INotifyPropertyChanged related members
        public event PropertyChangedEventHandler PropertyChanged;

        //private QueueSyncObj _queSyncObj = null;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        //

        public event DataRecievedEventHandler DataRecieved = null;

        // TO DO:  Bring in this class and convert:
        public DiagDataManager()
        {
            CUAData = new CUTestsData();
            CUBData = new CUTestsData();
        }

        public CUTestsData CUAData { get; private set; }

        public CUTestsData CUBData { get; private set; }

        public Boolean Initialize(CAN2 can, List<String> watchVariables = null)
        {
            try
            {
                logger.Debug("Entered");
                _watchVarsA = new Dictionary<String, Int16>();
                _watchVarsB = new Dictionary<String, Int16>();

                // Initialize the WatchManager and set up watch variables
                if (WatchManager.Initialize(can) == false) return false;
                // Setup Watch-Variables
                SetupWatchVariables(watchVariables);

                // Attach the event handler to the WatchManager's WatchDataReady event
                WatchManager.WatchDataReady += WatchDataReadyHandler;

                // Start watching the variables
                WatchManager.Start();

                _bProcessingDataReadyEvent = false;

                // Initialize the thread object
                //dataThread = new Thread(new ThreadStart(DataInitializer));
                //dataThread.Name = "DataPackageThread";
                //dataThread.IsBackground = true;
                _bStopRequested = false;

                // Kick off the data capturing thread
                //dataThread.Start();

                //_queSyncObj = new QueueSyncObj();

            }
            catch (PTIException exp)
            {
                logger.Error(Exception_Helper.FormatExceptionString(exp));
                throw exp;
            }
            finally
            {
                logger.Debug("Leaving");
            }
            return true;
        }

        private void SetupWatchVariables(List<String> vars)
        {
            try
            {
                logger.Debug("Entered");
                var finalList = new List<String>
                {
                    "m_sk_sfit_hazards",
                    "m_sk_sfit_comm_fault",
                    "m_sk_sfit_sensor_local_fault",
                    "m_sk_sfit_sensor_remote_fault",
                    "m_sk_sfit_actuator_local_fault",
                    "sk_eeprom_gp_data2",
                    "sk_eeprom_gp_data3"
                };

                if (vars != null && vars.Count != 0) finalList.AddRange(vars);
                foreach (var wvar in finalList)
                {
                    WatchManager.SetWatchForVariable(wvar);
                    _watchVarsA[wvar] = 0;
                    _watchVarsB[wvar] = 0;
                    Thread.Sleep(10);
                }
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

        //private void SetFaultsWatchVariables()
        //{
        //    //// Variable 1
        //    //_WatchManager.SetWatchForVariable("m_sk_sfit_hazards");
        //    //_watchVarsA.Add("m_sk_sfit_hazards", 0);
        //    //_watchVarsB.Add("m_sk_sfit_hazards", 0);
        //    //Thread.Sleep(10);
        //    //// Variable 2
        //    //_WatchManager.SetWatchForVariable("m_sk_sfit_comm_fault");
        //    //_watchVarsA.Add("m_sk_sfit_comm_fault", 0);
        //    //_watchVarsB.Add("m_sk_sfit_comm_fault", 0);
        //    //Thread.Sleep(10);
        //    //// Variable 3
        //    //_WatchManager.SetWatchForVariable("m_sk_sfit_sensor_local_fault");
        //    //_watchVarsA.Add("m_sk_sfit_sensor_local_fault", 0);
        //    //_watchVarsB.Add("m_sk_sfit_sensor_local_fault", 0);
        //    //Thread.Sleep(10);
        //    //// Variable 4
        //    //_WatchManager.SetWatchForVariable("m_sk_sfit_sensor_remote_fault");
        //    //_watchVarsA.Add("m_sk_sfit_sensor_remote_fault", 0);
        //    //_watchVarsB.Add("m_sk_sfit_sensor_remote_fault", 0);
        //    //Thread.Sleep(10);
        //    //// Variable 5
        //    //_WatchManager.SetWatchForVariable("m_sk_sfit_actuator_local_fault");
        //    //_watchVarsA.Add("m_sk_sfit_actuator_local_fault", 0);
        //    //_watchVarsB.Add("m_sk_sfit_actuator_local_fault", 0);
        //    //Thread.Sleep(10);
        //    //// Variable 6
        //    //_WatchManager.SetWatchForVariable("sk_eeprom_gp_data2");
        //    //_watchVarsA.Add("sk_eeprom_gp_data2", 0);
        //    //_watchVarsB.Add("sk_eeprom_gp_data2", 0);
        //    //Thread.Sleep(10);
        //    //// Variable 7
        //    //_WatchManager.SetWatchForVariable("sk_eeprom_gp_data3");
        //    //_watchVarsA.Add("sk_eeprom_gp_data3", 0);
        //    //_watchVarsB.Add("sk_eeprom_gp_data3", 0);
        //    //Thread.Sleep(10);
        //}

        public void Unload()
        {
            WatchManager.Stop();
            _bStopRequested = true;
            if (_watchVarsA != null) _watchVarsA.Clear();
            if (_watchVarsB != null) _watchVarsB.Clear();
            WatchManager.WatchDataReady -= WatchDataReadyHandler;
            Thread.Sleep(1000);
        }

        public void ResetDataContainersForNewTest()
        {
            logger.Debug("Resetting data containers...");
            WatchManager.ResetDataContainersForNewTest();
            ClearWatchVarsData();
            logger.Debug("Resetting data containers: DONE...");
        }

        public void ClearWatchVarsData()
        {
            foreach (var key in _watchVarsA.Keys)
            {
                _watchVarsA[key] = 0;
                _watchVarsB[key] = 0;
            }
            //_watchVarsA["m_sk_sfit_hazards"] = 0;
            //_watchVarsB["m_sk_sfit_hazards"] = 0;
            //_watchVarsA["m_sk_sfit_comm_fault"] = 0;
            //_watchVarsB["m_sk_sfit_comm_fault"] = 0;
            //_watchVarsA["m_sk_sfit_sensor_local_fault"] = 0;
            //_watchVarsB["m_sk_sfit_sensor_local_fault"] = 0;
            //_watchVarsA["m_sk_sfit_sensor_remote_fault"] = 0;
            //_watchVarsB["m_sk_sfit_sensor_remote_fault"] = 0;
            //_watchVarsA["m_sk_sfit_actuator_local_fault"] = 0;
            //_watchVarsB["m_sk_sfit_actuator_local_fault"] = 0;
            //_watchVarsA["sk_eeprom_gp_data2"] = 0;
            //_watchVarsB["sk_eeprom_gp_data2"] = 0;
            //_watchVarsA["sk_eeprom_gp_data3"] = 0;
            //_watchVarsB["sk_eeprom_gp_data3"] = 0;
            //_watchVarsA["amp.sensors.iq"] = 0;
            //_watchVarsB["amp.sensors.iq"] = 0;
            //_watchVarsA["amp.sensors.v_q"] = 0;
            //_watchVarsB["amp.sensors.v_q"] = 0;
            //_watchVarsA["amp2.sensors.iq"] = 0;
            //_watchVarsB["amp2.sensors.iq"] = 0;
            //_watchVarsA["amp2.sensors.v_q"] = 0;
            //_watchVarsB["amp2.sensors.v_q"] = 0;
            //_watchVarsA["amp.speed_pos.speed"] = 0;
            //_watchVarsB["amp.speed_pos.speed"] = 0;
            //_watchVarsA["amp2.speed_pos.speed"] = 0;
            //_watchVarsB["amp2.speed_pos.speed"] = 0;
            //_watchVarsA["amp.cal.curr_s_filtered"] = 0;
            //_watchVarsB["amp.cal.curr_s_filtered"] = 0;
            //_watchVarsA["amp2.cal.curr_s_filtered"] = 0;
            //_watchVarsB["amp2.cal.curr_s_filtered"] = 0;
        }

        public EmbeddedFaults GetEmbeddedFaults(CAN_CU_Sides cu)
        {
            EmbeddedFaults faults = new EmbeddedFaults(cu);

            try
            {
                if (_bStopRequested == false)
                {
                    faults.Load_Fault_Data(cu == CAN_CU_Sides.A ? _watchVarsA : _watchVarsB);
                }
            }
            catch (Exception ex)
            {
                logger.Debug(ex);
            }

            return faults;
        }

        private SafeQueue<WatchDataReadyEventArgs> dataQue = new SafeQueue<WatchDataReadyEventArgs>();

        private Boolean _bProcessingDataReadyEvent = false;

        /// <summary>
        /// WatchManager's WatchDataReady event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void WatchDataReadyHandler(WatchDataReadyEventArgs args)
        {
            if (DataRecieved == null)
            {
                logger.Warn("DataReceived handler has not been set");
                return;
            }


            if (_bProcessingDataReadyEvent == false)
            {
                _bProcessingDataReadyEvent = true;
                //_watchVarsA = args.WatchVariablesA;
                //_watchVarsB = args.WatchVariablesB;
                // InitializeCUTestData();
                if (DataRecieved != null)
                {
                    DataRecieved(args.Side, args.WatchVariables);
                }
                _bProcessingDataReadyEvent = false;
            }
        }

        /// <summary>
        /// Thread method to populate the internal data structures.
        /// </summary>
        private void DataInitializer()
        {
            // logger.debug("Entering DataIntializer Thread method...");
            // content previously commented out in Adv. Motor Diagnostics
            /*while (_bStopRequested == false)
            {
                Int32 nCount = dataQue.Count;
                AMDApp.WriteLogMessage(String.Format("DIAGDATAMANAGER QUEUE SIZE: {0}", nCount));
                Monitor.Enter(_queSyncObj);
                
                if (_queSyncObj.CanChangeQueue == true)
                {
                    if (nCount != 0)
                    {
                        WatchDataReadyEventArgs args = dataQue.Dequeue();
                        //AMDApp.WriteLogMessage(String.Format("ListA Count: {0}, ListB Count: {1}", args.WatchVariablesA.Count, args.WatchVariablesB.Count));
                        _watchVarsA = args.WatchVariablesA;
                        _watchVarsB = args.WatchVariablesB;
                        InitializeCUTestData();
                    }
                }

                Monitor.Exit(_queSyncObj);
                //Thread.Sleep(1);
            }*/

            // logger.debug("Exiting DataIntializer Thread method...");
        }
    }
}

