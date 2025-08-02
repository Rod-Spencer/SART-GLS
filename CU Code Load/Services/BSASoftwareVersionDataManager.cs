using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using NLog;
using Segway.Modules.Diagnostics_Helper;
using Segway.Service.Common.LoggerHelp;

namespace Segway.Modules.SART.CodeLoad
{
    /// <summary>Public Member</summary>
    public delegate void DataRecievedEventHandler(Dictionary<String, UInt16> varsA, Dictionary<String, UInt16> varsB);

    /// <summary>Public Class</summary>
    public class BSASoftwareVersionDataManager : INotifyPropertyChanged
    {
        private Dictionary<String, UInt16> _watchVarsA = null;
        private Dictionary<String, UInt16> _watchVarsB = null;
        private WatchManager _WatchManager = null;

        private static Logger logger = LoggerHelper.GetCurrentLogger();

        // INotifyPropertyChanged related members
        /// <summary>Public Member</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        //private QueueSyncObj _queSyncObj = null;

        /// <summary>Public Method - NotifyPropertyChanged</summary>
        /// <param name="propertyName">string</param>
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        //

        /// <summary>Public Member</summary>
        public event DataRecievedEventHandler DataRecieved = null;

        // TO DO:  Bring in this class and convert:
        /// <summary>Contructor</summary>
        /// <param name="wm">WatchManager</param>
        public BSASoftwareVersionDataManager(WatchManager wm)
        {
            _WatchManager = wm;
        }

        /// <summary>Public Method - Initialize</summary>
        /// <returns>Boolean</returns>
        public Boolean Initialize()
        {
            try
            {
                _watchVarsA = new Dictionary<String, UInt16>();
                _watchVarsB = new Dictionary<String, UInt16>();

                // Initialize the WatchManager and set up watch variables
                if (_WatchManager.Initialize() == false) return false;

                // Setup Watch-Variables
                SetupWatchVariables();

                // Attach the event handler to the WatchManager's WatchDataReady event
                _WatchManager.WatchDataReady += WatchDataReadyHandler;

                // Start watching the variables
                _WatchManager.Start();

                _bProcessingDataReadyEvent = false;
            }
            catch (Exception exp)
            {
                throw exp;
            }

            return true;
        }

        private void SetupWatchVariables()
        {
            _WatchManager.SetWatchForVariable("bsa_software_version");
            _watchVarsA.Add("bsa_software_version", 0);
            _watchVarsB.Add("bsa_software_version", 0);
            Thread.Sleep(10);
            _WatchManager.SetWatchForVariable("bsa_software_build_count", _WatchManager.GetWatchVariableAddress("bsa_software_version") + 1);
            _watchVarsA.Add("bsa_software_build_count", 0);
            _watchVarsB.Add("bsa_software_build_count", 0);
        }

        /// <summary>Public Method - Unload</summary>
        /// <returns>void</returns>
        public void Unload()
        {
            _WatchManager.Stop();
            if (_watchVarsA != null) _watchVarsA.Clear();
            if (_watchVarsB != null) _watchVarsB.Clear();
            _WatchManager.WatchDataReady -= WatchDataReadyHandler;
            Thread.Sleep(1000);
        }

        /// <summary>Public Method - ResetDataContainersForNewTest</summary>
        /// <returns>void</returns>
        public void ResetDataContainersForNewTest()
        {
            logger.Debug("Resetting data containers...");
            _WatchManager.ResetDataContainersForNewTest();
            ClearWatchVarsData();
            logger.Debug("Resetting data containers: DONE...");
        }

        /// <summary>Public Method - ClearWatchVarsData</summary>
        /// <returns>void</returns>
        public void ClearWatchVarsData()
        {
            _watchVarsA["bsa_software_version"] = 0;
            _watchVarsB["bsa_software_version"] = 0;

            _watchVarsA["bsa_software_build_count"] = 0;
            _watchVarsB["bsa_software_build_count"] = 0;
        }

        private Boolean _bProcessingDataReadyEvent = false;

        /// <summary>
        /// WatchManager's WatchDataReady event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void WatchDataReadyHandler(Object sender, WatchDataReadyEventArgs args)
        {
            if (_bProcessingDataReadyEvent == false)
            {
                _bProcessingDataReadyEvent = true;
                _watchVarsA = args.WatchVariablesA;
                _watchVarsB = args.WatchVariablesB;
                // InitializeCUTestData();
                if (DataRecieved != null)
                {
                    DataRecieved(_watchVarsA, _watchVarsB);
                }
                _bProcessingDataReadyEvent = false;
            }
        }

        /// <summary>
        /// Thread method to populate the internal data structures.
        /// </summary>
        private void DataInitializer()
        {

        }
    }
}

