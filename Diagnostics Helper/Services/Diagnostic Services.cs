using NLog;
using Segway.Service.CAN;
using Segway.Service.ExceptionHelper;
using Segway.Service.LoggerHelper;
using System;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Segway.Modules.Diagnostics_Helper
{
    public class Diagnostic_Services
    {
        public delegate void PublishEventAggregate(String str);

        protected static Logger logger = Logger_Helper.GetCurrentLogger();


        public static string Display_Faults(EmbeddedFaults FaultsA, EmbeddedFaults FaultsB)
        {
            StringBuilder sb = new StringBuilder(1000);
            //sb.AppendLine("--------A-Side Faults--------");
            sb.AppendLine(FaultsA.ToString());
            sb.AppendLine();
            //sb.AppendLine("--------B-Side Faults--------");
            sb.AppendLine(FaultsB.ToString());
            return sb.ToString();
        }



        public static void Fault_Scanner(ref Boolean StopRequested, ref Boolean FaultsExist, TestDataManager_Interface _DataManager, PowerUpModes _Mode, PublishEventAggregate publish, ManualResetEvent faultEvent = null)
        {
            try
            {
                logger.Info("Entered - {0}", MethodBase.GetCurrentMethod().Name);
                String prevMsg = null;
                String message = null;
                Boolean bFaultsA = false, bFaultsB = false;

                while (StopRequested == false)
                {
                    if (faultEvent != null) faultEvent.Reset();

                    var FaultsA = _DataManager.GetEmbeddedFaults(CAN_CU_Sides.A);
                    if ((bFaultsA = FaultsDecoder.DecodeFaultsIfAny(CAN_CU_Sides.A, FaultsA, _Mode == PowerUpModes.POWER_UP_WIRELESS_WID ? false : true)) == true)
                    {
                        //message += "******* A-Side Faults *******" + Environment.NewLine;
                        message += FaultsA.ToString();
                    }

                    if (StopRequested == true) break;

                    var FaultsB = _DataManager.GetEmbeddedFaults(CAN_CU_Sides.B);
                    if ((bFaultsB = FaultsDecoder.DecodeFaultsIfAny(CAN_CU_Sides.B, FaultsB, _Mode == PowerUpModes.POWER_UP_WIRELESS_WID ? false : true)) == true)
                    {
                        //message += Environment.NewLine + Environment.NewLine + "******* B-Side Faults *******" + Environment.NewLine;
                        message += FaultsB.ToString();
                    }

                    if (bFaultsA == true || bFaultsB == true)
                    {
                        if (String.IsNullOrEmpty(message) == false)
                        {
                            if (message != prevMsg)
                            {
                                FaultsExist = true;
                                StopRequested = true;
                                prevMsg = message;
                                publish?.Invoke(Display_Faults(FaultsA, FaultsB));
                            }
                            logger.Debug("######################################### Real embedded faults occurred #########################################\n\n");
                            logger.Debug(message);
                            logger.Debug("#################################################################################################################\n\n");
                        }
                    }

                    if (faultEvent != null) faultEvent.Set();
                }
                if (faultEvent != null) faultEvent.Set();

                //logger.Debug("BSATest: FaultsScannerThreadMethod exiting...");
            }
            catch (Exception ex)
            {
                logger.Error(Exception_Helper.FormatExceptionString(ex));
                //throw;
            }
            finally
            {
                logger.Info("Leaving - {0}", MethodBase.GetCurrentMethod().Name);
            }
        }
    }
}
