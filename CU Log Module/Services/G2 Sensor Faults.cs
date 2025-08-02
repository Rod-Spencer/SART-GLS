using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Segway.Modules.CU_Log_Module
{
    public class G2_Sensor_Faults : G2_Fault_Base<G2_Sensor_Fault_Names>
    {
        public G2_Sensor_Faults()
        {
            Config();
        }

        private void Config()
        {
            FaultList.Add(G2_Sensor_Fault_Names.Yaw_A_Hard_Fault, new G2_Fault_Definition("Sensor", "Yaw A Hard Fault", 0x0001));
            FaultList.Add(G2_Sensor_Fault_Names.Yaw_B_Hard_Fault, new G2_Fault_Definition("Sensor", "Yaw B Hard Fault", 0x0002));
            FaultList.Add(G2_Sensor_Fault_Names.Yaw_Drift_Fault, new G2_Fault_Definition("Sensor", "Yaw Drift Fault", 0x0004));
            FaultList.Add(G2_Sensor_Fault_Names.Aux_A_Hard_Fault, new G2_Fault_Definition("Sensor", "Aux A Hard Fault", 0x0008));
            FaultList.Add(G2_Sensor_Fault_Names.Aux_B_Hard_Fault, new G2_Fault_Definition("Sensor", "Aux B Hard Fault", 0x0010));
            FaultList.Add(G2_Sensor_Fault_Names.Aux_Drift_Fault, new G2_Fault_Definition("Sensor", "Aux Drift Fault", 0x0020));

            FaultList.Add(G2_Sensor_Fault_Names.Rider_Detect_Fault, new G2_Fault_Definition("Sensor", "Rider Detect Fault", 0x40, SeverityColor.Red));
            FaultList.Add(G2_Sensor_Fault_Names.PSupply_Trans_Fault, new G2_Fault_Definition("Sensor", "PSupply Trans Fault", 0x80, SeverityColor.Red));
            FaultList.Add(G2_Sensor_Fault_Names.PSupply_12V_Fault, new G2_Fault_Definition("Sensor", "PSupply 12V Fault", 0x100, SeverityColor.Red));
            FaultList.Add(G2_Sensor_Fault_Names.PSupply_5V_Fault, new G2_Fault_Definition("Sensor", "PSupply 5V Fault", 0x200, SeverityColor.Red));
            FaultList.Add(G2_Sensor_Fault_Names.PSupply_3V_Fault, new G2_Fault_Definition("Sensor", "PSupply 3V Fault", 0x400, SeverityColor.Red));
            FaultList.Add(G2_Sensor_Fault_Names.BSA_Temperature_Fault, new G2_Fault_Definition("Sensor", "BSA Temperature Fault", 0x800, SeverityColor.Red));
            FaultList.Add(G2_Sensor_Fault_Names.BSA_PSupply_Trans_Fault, new G2_Fault_Definition("Sensor", "BSA PSupply Trans Fault", 0x1000, SeverityColor.Red));
            FaultList.Add(G2_Sensor_Fault_Names.BSA_Drift_Fault, new G2_Fault_Definition("Sensor", "BSA Drift Fault", 0x2000, SeverityColor.Red));
            FaultList.Add(G2_Sensor_Fault_Names.BSA_Internal_Fault, new G2_Fault_Definition("Sensor", "BSA Internal Fault", 0x4000, SeverityColor.Red));
        }

        public G2_Fault_Definition this[G2_Sensor_Fault_Names index]
        {
            get
            {
                if (!Enum.IsDefined(typeof(G2_Sensor_Fault_Names), index)) throw new ArgumentOutOfRangeException("Index index (SensorFaultNames) is not defined");
                return FaultList[index];
            }
        }
    }
}
