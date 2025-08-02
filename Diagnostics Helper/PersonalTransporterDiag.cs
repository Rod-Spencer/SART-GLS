using Segway.Service.CAN;
using Segway.Service.Tools.CAN2;
using System;
using System.Threading;
using System.Timers;

namespace Segway.Modules.Diagnostics_Helper
{
    public class PersonalTransporterDiag
    {
        public CU CUA = new CU(CAN_CU_Sides.A);

        public CU CUB = new CU(CAN_CU_Sides.B);

        private bool _bIsConnectedInternal;

        private bool _bFireConnected;

        private bool _bFireDisconnected;

        private System.Timers.Timer _connectionTimer;

        public bool InDiagnostic
        {
            get;
            private set;
        }

        public bool IsConnected
        {
            get
            {
                if (!CUA.IsConnected)
                {
                    return false;
                }
                return CUB.IsConnected;
            }
        }

        public bool IsStopTestsRequested
        {
            get;
            set;
        }

        public TestStatus LastTestResult
        {
            get;
            internal set;
        }

        public PersonalTransporterDiag()
        {
            LastTestResult = TestStatus.Passed;
            IsStopTestsRequested = false;
            _connectionTimer = new System.Timers.Timer(100);
        }

        private void ConnectionPingThread(object sender, ElapsedEventArgs args)
        {
            _bIsConnectedInternal = (!CUA.IsConnected ? false : CUB.IsConnected);
            if (_bIsConnectedInternal)
            {
                if (!_bFireConnected)
                {
                    if (Connected != null)
                    {
                        Connected(this);
                    }
                    _bFireConnected = true;
                    _bFireDisconnected = false;
                    return;
                }
            }
            else if (!_bFireDisconnected)
            {
                if (Disconnected != null)
                {
                    Disconnected(this);
                }
                _bFireDisconnected = true;
                _bFireConnected = false;
            }
        }

        public bool PowerDown()
        {
            _bIsConnectedInternal = false;
            _bFireConnected = false;
            _bFireDisconnected = false;
            InDiagnostic = false;
            IsStopTestsRequested = false;
            EventManager.UpdateStatus("Shutting down...");
            Thread.Sleep(500);
            return true;
        }

        public bool PowerUp(bool bEnterDiagnosticMode, PTModel model)
        {
            try
            {
                EventManager.UpdateStatus("Initializing CAN interface...");
                if (!(new CAN2_Systec()).Initialize())
                {
                    throw new PTIException("Failed to Initialize CAN Interface.");
                }
                EventManager.UpdateStatus("Loading COFF file...");
                if (model != PTModel.i2)
                {
                }
                Thread.Sleep(200);
                EventManager.UpdateStatus("Waiting for PT to settle down...");
                Thread.Sleep(5000);
                if (bEnterDiagnosticMode && !InDiagnostic)
                {
                    EventManager.UpdateStatus("Failed to enter diagnostic mode...");
                    throw new PTIException("Failed to enter diagnostic mode.");
                }
            }
            catch (Exception exception)
            {
                throw new PTIException(exception.Message);
            }
            EventManager.UpdateStatus("PT is ready after successful power up...");
            return true;
        }

        public event ConnectionEventHandler Connected;

        public event ConnectionEventHandler Disconnected;
    }
}
