using Segway.SART.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Data_Graph_Data</summary>
    public class Black_Box_Data_Graph_Data
    {

        #region Data

        private static Dictionary<String, List<Int16>> _Data;

        /// <summary>Property Data of type Dictionary&lt;String, List&lt;Int16&gt;&gt;</summary>
        public static Dictionary<String, List<Int16>> Data
        {
            get
            {
                if (_Data == null) _Data = new Dictionary<String, List<Int16>>();
                return _Data;
            }
        }

        #endregion

        /// <summary>Public Static Method - Load</summary>
        /// <param name="bbdData">List&lt;BSA_Black_Box_Data&gt;</param>
        public static void Load(List<BSA_Black_Box_Data> bbdData)
        {
            if (bbdData == null) return;
            if (bbdData.Count == 0) return;
            Data.Clear();

            Type t = typeof(BSA_Black_Box_Data);
            foreach (PropertyInfo pi in t.GetProperties())
            {
                switch (pi.Name)
                {
                    case "Is_CRC_Valid":
                    case "Header_Key":
                    case "Record":
                    case "Adjusted_Frame_Count":
                    case "Frame_Count":
                    case "ID":
                    case "Date_Time_Entered":
                    case "Date_Time_Updated":
                    //case "Yaw_Gyro_Sat":
                    //case "Wheel_Yaw_Velocity_Threshold_Crossed":
                    //case "BSA_Vs_DWV_Yaw_Disparity_Threshold_Crossed":
                    //case "Pitch_Acc_Saturated":
                    //case "Filter_Picth_Acc_Saturated":
                    //case "Roll_Acc_Saturated":
                    //case "Dizzy_Resp_Timer_Threshold_Crossed":
                    //case "Prolonged_Dizzy_Resp_Timer_Threshold_Crossed":
                    //case "Shock_Signal_Threshold_Crossed":
                    //case "Tildas_Zerped_For_Shock":
                    //case "Any_Dizzy_Timer_Threshold_Crossed":
                    //case "Side_A_ARS_Fault":
                    //case "Three_Axis_Mode":
                    //case "PSE_Initialized":
                    //case "PSE_Operational":
                    //case "PSE_Corner_Freq_Ramping_Down":
                        continue;
                }
                if (Data.ContainsKey(pi.Name) == false)
                {
                    Data[pi.Name] = new List<Int16>();
                }
                foreach (var bbd in bbdData)
                {
                    Object o = pi.GetValue(bbd, null);
                    if (o != null)
                    {
                        Data[pi.Name].Add((Int16)o);
                    }
                }
            }
        }
    }
}
