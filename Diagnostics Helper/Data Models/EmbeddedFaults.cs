using Segway.Service.CAN;
using System;
using System.Collections.Generic;
using System.Text;

namespace Segway.Modules.Diagnostics_Helper
{
    public class EmbeddedFaults
    {
        public EmbeddedFaults(CAN_CU_Sides side)
        {
            Side = side;
        }

        CAN_CU_Sides Side;

        public const Int32 SumAndBoundaryErrorsLimit = 50;

        public const Int32 MSFEMErrorsLimit = 3;

        public const Int32 DegradedErrorsLimit = 500;

        public UInt16 TransientHazard { get; set; }

        public UInt16 CriticalHazard { get; set; }

        public UInt16 CommunicationFaults { get; set; }

        public UInt16 LocalSensorsFaults { get; set; }

        public UInt16 RemoteSensorsFaults { get; set; }

        public UInt16 ActuatorFaults { get; set; }

        public UInt16 GeneralPuposeData1 { get; set; }

        public UInt16 GeneralPuposeData2 { get; set; }

        public UInt16 GeneralPuposeData3 { get; set; }

        public UInt16 GeneralPuposeData4 { get; set; }


        public String TransientHazardNames { get; set; }

        public String CriticalHazardNames { get; set; }

        public String CommunicationFaultNames { get; set; }

        public String SensorFaultNames { get; set; }

        public String ActuatorFaultNames { get; set; }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            String header = String.Format("******* {0}-Side Faults *******", Side);
            Boolean usedHeader = false;
            //sb.AppendLine(String.Format("Transient Hazards: {0:X4}", TransientHazard));
            if (String.IsNullOrEmpty(TransientHazardNames) == false)
            {
                if (usedHeader == false)
                {
                    sb.AppendLine(header);
                    usedHeader = true;
                }
                sb.AppendLine(TransientHazardNames);
            }
            //sb.AppendLine(String.Format("Critical Hazards: {0:X4}", CriticalHazard));
            if (String.IsNullOrEmpty(CriticalHazardNames) == false)
            {
                if (usedHeader == false)
                {
                    sb.AppendLine(header);
                    usedHeader = true;
                }
                sb.AppendLine(CriticalHazardNames);
            }
            //sb.AppendLine(String.Format("Communication Faults: {0:X4}", CommunicationFaults));
            if (String.IsNullOrEmpty(CommunicationFaultNames) == false)
            {
                if (usedHeader == false)
                {
                    sb.AppendLine(header);
                    usedHeader = true;
                }
                sb.AppendLine(CommunicationFaultNames);
            }
            //sb.AppendLine(String.Format("Sensor Faults: {0:X4}", LocalSensorsFaults));
            if (String.IsNullOrEmpty(SensorFaultNames) == false)
            {
                if (usedHeader == false)
                {
                    sb.AppendLine(header);
                    usedHeader = true;
                }
                sb.AppendLine(SensorFaultNames);
            }
            //sb.AppendLine(String.Format("Actuator Faults: {0:X4}", ActuatorFaults));
            if (String.IsNullOrEmpty(ActuatorFaultNames) == false)
            {
                if (usedHeader == false)
                {
                    sb.AppendLine(header);
                    usedHeader = true;
                }
                sb.AppendLine(ActuatorFaultNames);
            }
            return sb.ToString().Trim();
        }

        public void Load_Fault_Data(Dictionary<String, Int16> watchVars)
        {
            if (watchVars == null) return;
            TransientHazard = (UInt16)(watchVars.ContainsKey("m_sk_sfit_hazards") == true ? (watchVars["m_sk_sfit_hazards"] & 0x00FF) : 0);
            CriticalHazard = (UInt16)(watchVars.ContainsKey("m_sk_sfit_hazards") == true ? (watchVars["m_sk_sfit_hazards"] >> 8) : 0);
            CommunicationFaults = (UInt16)(watchVars.ContainsKey("m_sk_sfit_comm_fault") == true ? watchVars["m_sk_sfit_comm_fault"] : 0);
            LocalSensorsFaults = (UInt16)(watchVars.ContainsKey("m_sk_sfit_sensor_local_fault") == true ? watchVars["m_sk_sfit_sensor_local_fault"] : 0);
            RemoteSensorsFaults = (UInt16)(watchVars.ContainsKey("m_sk_sfit_sensor_remote_fault") == true ? watchVars["m_sk_sfit_sensor_remote_fault"] : 0);
            ActuatorFaults = (UInt16)(watchVars.ContainsKey("m_sk_sfit_actuator_local_fault") == true ? watchVars["m_sk_sfit_actuator_local_fault"] : 0);
            GeneralPuposeData2 = (UInt16)(watchVars.ContainsKey("sk_eeprom_gp_data2") == true ? watchVars["sk_eeprom_gp_data2"] : 0);
            GeneralPuposeData3 = (UInt16)(watchVars.ContainsKey("sk_eeprom_gp_data3") == true ? watchVars["sk_eeprom_gp_data3"] : 0);
        }
    }
}
