using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Segway.Service.Tools.CAN2;
using Segway.Service.CAN;

namespace Segway.Modules.Diagnostics_Helper
{
    public class FaultsDecoder
    {
        public FaultsDecoder() { }

        private const Int32 ANY_MOTOR_DRIVE_FAULT = 0x00F8;

        private const Int32 MOTOR_DRIVE_FAULT_MASK = 0xFFFB;

        private const Int32 MOTOR_VOLTAGE_CONSISTENCY_FAULT = 0x0400;

        private const Int32 AMP1_TEST = 1;

        private const Int32 AMP2_TEST = 2;

        private const Int32 VOLTFB_FAULT = 0x0004;

        private const Int32 HALL_FAULT = 0x0100;

        private const Int32 OVER_CURRENT_FAULT = 0x0400;

        private const Int32 AMP_ENABLE_FAULT = 0x4000;

        private const Int32 AMP_FAULT = 0x8000;

        private const Int32 LEFT_AMP_FAULT = (VOLTFB_FAULT | HALL_FAULT | OVER_CURRENT_FAULT | AMP_FAULT);

        private const Int32 RIGHT_AMP_FAULT = (VOLTFB_FAULT | HALL_FAULT | OVER_CURRENT_FAULT | AMP_FAULT);

        private static UInt16 _critHazardsMaskA = 0x0000;

        private static UInt16 _critHazardsMaskB = 0x0000;

        private static UInt16 _transHazardsMaskA = 0x0000;

        private static UInt16 _transHazardsMaskB = 0x0000;

        private static UInt16 _commMaskA = 0x080C;

        private static UInt16 _commMaskB = 0x08C0;

        private static UInt16 _sensMaskA = 0x0007;

        private static UInt16 _sensMaskB = 0x0007;

        private static UInt16 _actMaskA = 0x0000;

        private static UInt16 _actMaskB = 0x0000;

        private static String TabChar
        {
            get
            {
                return "\t";
            }
        }

        public static Boolean DecodeFaultsIfAny(CAN_CU_Sides side, EmbeddedFaults faults, Boolean maskFaults = true)
        {
            if (faults == null) return false;

            UInt16 nBit = 0;
            StringBuilder strTransientHazards = new StringBuilder();
            StringBuilder strCriticalHazards = new StringBuilder();
            StringBuilder strSensorFaults = new StringBuilder();
            StringBuilder strCommFaults = new StringBuilder();
            StringBuilder strActuatorFaults = new StringBuilder();

            Boolean _bFaults = false;
            SetFaultsMask(maskFaults);

            for (Int32 nIndex = 0; nIndex < 16; nIndex++)
            {
                nBit = (UInt16)(1 << nIndex);

                if ((faults.TransientHazard & nBit) != 0)
                {
                    if ((side == CAN_CU_Sides.A && !((_transHazardsMaskA & nBit) == nBit)) || (side == CAN_CU_Sides.B && !((_transHazardsMaskB & nBit) == nBit)))
                    {
                        String flt = GetTransientHazardsMessage(nIndex);
                        if (String.IsNullOrEmpty(flt) == false)
                        {
                            strTransientHazards.AppendLine(TabChar + flt);
                            _bFaults = true;
                        }
                    }
                }

                if ((faults.CriticalHazard & nBit) != 0)
                {
                    if (((side == CAN_CU_Sides.A) && !((_critHazardsMaskA & nBit) == nBit)) || ((side == CAN_CU_Sides.B) && !((_critHazardsMaskB & nBit) == nBit)))
                    {
                        String flt = GetCriticalHazardsMessage(nIndex);
                        if (String.IsNullOrEmpty(flt) == false)
                        {
                            strCriticalHazards.AppendLine(TabChar + flt);
                            _bFaults = true;
                        }
                    }
                }

                if ((faults.CommunicationFaults & nBit) != 0)
                {
                    if ((side == CAN_CU_Sides.A && !((_commMaskA & nBit) == nBit)) || (side == CAN_CU_Sides.B && !((_commMaskB & nBit) == nBit)))
                    {
                        String flt = GetCommFaultsMessage(nIndex);
                        if (String.IsNullOrEmpty(flt) == false)
                        {
                            strCommFaults.AppendLine(TabChar + flt);
                            _bFaults = true;
                        }
                    }
                }

                if ((faults.LocalSensorsFaults & nBit) != 0)
                {
                    if ((side == CAN_CU_Sides.A && !((_sensMaskA & nBit) == nBit)) || (side == CAN_CU_Sides.B && !((_sensMaskB & nBit) == nBit)))
                    {
                        String flt = GetSensorsFaultMessage(nIndex);
                        if (String.IsNullOrEmpty(flt) == false)
                        {
                            strSensorFaults.AppendLine(TabChar + flt);
                            _bFaults = true;
                        }
                    }
                }

                if ((faults.ActuatorFaults & nBit) == nBit)
                {
                    if (((side == CAN_CU_Sides.A) && ((_actMaskA & nBit) != nBit)) || ((side == CAN_CU_Sides.B) && ((_actMaskB & nBit) != nBit)))
                    {
                        String flt = GetActuatorFaultMessage(nIndex);
                        if (String.IsNullOrEmpty(flt) == false)
                        {
                            strActuatorFaults.Append(TabChar);
                            strActuatorFaults.Append(flt);
                            strActuatorFaults.Append(TabChar);
                            strActuatorFaults.AppendLine(GetActuatorFaultExtentendedMessage(side, faults.ActuatorFaults, faults.GeneralPuposeData2, faults.GeneralPuposeData3));
                            _bFaults = true;
                        }
                    }
                }
            }

            //strFaults += (strTransientHazards == String.Empty) ? String.Format("Transient Hazards:{0}{1}None{2}", Environment.NewLine, TabChar, Environment.NewLine) : String.Format("Transient Hazards:{0}{1}", Environment.NewLine, strTransientHazards);
            //strFaults += (strCriticalHazards == String.Empty) ? String.Format("Critical Hazards:{0}{1}None{2}", Environment.NewLine, TabChar, Environment.NewLine) : String.Format("Critical Hazards:{0}{1}", Environment.NewLine, strCriticalHazards);
            //strFaults += (strCommFaults == String.Empty) ? String.Format("Communication Faults:{0}{1}None{2}", Environment.NewLine, TabChar, Environment.NewLine) : String.Format("Communication Faults:{0}{1}", Environment.NewLine, strCommFaults);
            //strFaults += (strSensorFaults == String.Empty) ? String.Format("Sensors Faults:{0}{1}None{2}", Environment.NewLine, TabChar, Environment.NewLine) : String.Format("Sensors Faults:{0}{1}", Environment.NewLine, strSensorFaults);
            //strFaults += (strActuatorFaults == String.Empty) ? String.Format("Actuator Faults:{0}{1}None{2}", Environment.NewLine, TabChar, Environment.NewLine) : String.Format("Actuator Faults:{0}{1}", Environment.NewLine, strActuatorFaults);

            if (strTransientHazards.Length == 0) faults.TransientHazardNames = strTransientHazards.ToString();
            if (strCriticalHazards.Length == 0) faults.CriticalHazardNames = strCriticalHazards.ToString();
            if (strCommFaults.Length == 0) faults.CommunicationFaultNames = strCommFaults.ToString();
            if (strSensorFaults.Length == 0) faults.SensorFaultNames = strSensorFaults.ToString();
            if (strActuatorFaults.Length == 0) faults.ActuatorFaultNames = strActuatorFaults.ToString();

            return _bFaults;
        }

        private static void SetFaultsMask(Boolean maskFaults)
        {
            if (maskFaults)
            {
                _critHazardsMaskA = 0x0000;
                _critHazardsMaskB = 0x0000;
                _transHazardsMaskA = 0x0000;
                _transHazardsMaskB = 0x0000;
                _commMaskA = 0x080C;
                _commMaskB = 0x08C0;
                _sensMaskA = 0x0007;
                _sensMaskB = 0x0007;
                _actMaskA = 0x0000;
                _actMaskB = 0x0000;
            }
            else
            {
                _critHazardsMaskA = 0x0000;
                _critHazardsMaskB = 0x0000;
                _transHazardsMaskA = 0x0000;
                _transHazardsMaskB = 0x0000;
                _commMaskA = 0x0000;
                _commMaskB = 0x0000;
                _sensMaskA = 0x0000;
                _sensMaskB = 0x0000;
                _actMaskA = 0x0000;
                _actMaskB = 0x0000;
            }
        }

        private static String GetTransientHazardsMessage(Int32 nMsgIndex)
        {
            switch (nMsgIndex)
            {
                case 0:
                    return "PSE Degrade";
                case 1:
                    return "Rider Detect Off";
                case 2:
                    return "Battery Temperature Warm or Cool";
                case 3:
                    return "Battery Cold Regeneration";
                case 4:
                    return "AC Charge Present";
                case 5:
                    return "Actuator Temperature Max";
                case 6:
                    return "Low Battery";
                case 7:
                    return "Rider Detect Partial";
            }

            return String.Empty;
        }

        private static String GetCriticalHazardsMessage(Int32 nMsgIndex)
        {
            switch (nMsgIndex)
            {
                case 0:
                    return "Forward Pitch Exceeded";
                case 1:
                    return "AFT Pitch Exceeded";
                case 2:
                    return "Forward Speed Limiter";
                case 3:
                    return "AFT Speed Limiter";
                case 4:
                    return "Battery Empty";
                case 5:
                    return "Battery Shutdown";
                case 6:
                    return "Roll Angle Exceeded";
                case 7:
                    return "Low Bus Voltage";
            }

            return String.Empty;
        }

        private static String GetCommFaultsMessage(Int32 nMsgIndex)
        {
            switch (nMsgIndex)
            {
                case 0: // 0x0001
                    return "CU-CU Fault";
                case 1: // 0x0002
                    return "BSA-BSA Fault";
                case 2: // 0x0004
                    return "Local CU-BSA Fault";
                case 3: // 0x0008
                    return "Local CU-UI Fault";
                case 4: // 0x0010
                    return "Local CU-BCU Fault";
                case 5: // 0x0020
                    return "Local TDM Slot Fault";
                case 6: // 0x0040
                    return "Remote CU-BSA Fault";
                case 7: // 0x0080
                    return "Remote CU-UI Fault";
                case 8: // 0x0100
                    return "Remote CU-BCU Fault";
                case 9: // 0x0200
                    return "Remote TDM Slot Fault";
                case 10: // 0x0400
                    return "Mode Sync";
                case 11: // 0x0800
                    return "Communication Initialization Fault";
                case 12: // 0x1000
                    return "CU EEPROM Calibration Fault";
                case 13: // 0x2000
                    return "Training Safety Shutdown Fault";
                case 14: // 0x4000
                case 15: // 0x8000
                    break;
            }

            return String.Empty;
        }

        private static String GetSensorsFaultMessage(Int32 nMsgIndex)
        {
            switch (nMsgIndex)
            {
                case 0:
                    return "Yaw A Hard Fault";
                case 1:
                    return "Yaw B Hard Fault";
                case 2:
                    return "Yaw Drift Fault";
                case 3:
                    return "Aux A Hard Fault";
                case 4:
                    return "Aux B Hard Fault";
                case 5:
                    return "Aux Drift Fault";
                case 6:
                    return "Rider Detect Fault";
                case 7:
                    return "CU Power Supply Transient Fault";
                case 8:
                    return "CU Power Supply 12V Fault";
                case 9:
                    return "CU Power Supply 5V Fault";
                case 10:
                    return "CU Power Supply 3V Fault";
                case 11:
                    return "BSA Temperature Fault";
                case 12:
                    return "BSA Power Supply Transient Fault";
                case 13:
                    return "BSA Drift Fault";
                case 14:
                    return "BSA Internal Fault";
                case 15:
                    break;
            }

            return String.Empty;
        }

        private static String GetActuatorFaultMessage(Int32 nMsgIndex)
        {
            switch (nMsgIndex)
            {
                case 0:
                    return "FET Junction Temperature Fault";
                case 1:
                    return "Motor Winding Temperature Fault";
                case 2:
                    return "Motor Drive Fault";
                case 3:
                    return "Motor Drive Hall Fault";
                case 4:
                    return "Motor Drive Amp Fault";
                case 5:
                    return "Motor Drive Amp Enable Fault";
                case 6:
                    return "Motor Drive Amp Over Current Fault";
                case 7:
                    return "Motor Drive Voltage Feedback Fault";
                case 8:
                    return "Frame Fault";
                case 9:
                    return "Battery Fault";
                case 10:
                    return "Motor Voltage Consistency Fault";
                case 11:
                    return "Motor Stuck Relay Fault";
                case 12:
                    return "Actuator Power Consistency Fault";
                case 13:
                    return "Halt Processor Fault";
                case 14:
                    return "Actuator Degraded Fault";
                case 15:
                    break;
            }

            return String.Empty;
        }

        private static String GetActuatorFaultExtentendedMessage(CAN_CU_Sides cuId, UInt16 faults, UInt16 gpData2, UInt16 gpData3)
        {
            String strMessage = String.Empty;

            if ((faults & ANY_MOTOR_DRIVE_FAULT) != 0)
            {
                if ((gpData2 & LEFT_AMP_FAULT) != 0)
                {
                    strMessage += " (Left Side Fault)";
                }

                if ((gpData3 & RIGHT_AMP_FAULT) != 0)
                {
                    strMessage += " (Right Side Fault)";
                }
            }
            else if ((faults & MOTOR_VOLTAGE_CONSISTENCY_FAULT) != 0)
            {
                if (cuId == CAN_CU_Sides.A)
                {
                    if ((gpData2 & AMP1_TEST) != 0)
                    {
                        strMessage += " (Left Side Fault)";
                    }

                    if ((gpData2 & AMP2_TEST) != 0)
                    {
                        strMessage += " (Right Side Fault)";
                    }
                }
                else
                {
                    if ((gpData2 & AMP1_TEST) != 0)
                    {
                        strMessage += " (Right Side Fault)";
                    }

                    if ((gpData2 & AMP2_TEST) != 0)
                    {
                        strMessage += " (Left Side Fault)";
                    }
                }
            }

            return strMessage;
        }
    }
}
