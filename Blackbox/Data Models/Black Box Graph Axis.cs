using System;
using System.Runtime.Serialization;

namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Graph_Axis</summary>
    [Serializable]
    [DataContract]
    public class Black_Box_Graph_Axis
    {
        /// <summary>Public Property - Axis</summary>
        [DataMember]
        public Black_Box_Graph_Axis_Types Axis { get; set; }

        /// <summary>Public Property - Generation</summary>
        [DataMember]
        public Black_Box_Axis_Generation Generation { get; set; }

        /// <summary>Public Property - Label</summary>
        [DataMember]
        public String Label { get; set; }

        /// <summary>Public Property - Min</summary>
        [DataMember]
        public Double Min { get; set; }

        /// <summary>Public Property - Max</summary>
        [DataMember]
        public Double Max { get; set; }

        /// <summary>Public Property - Increment</summary>
        [DataMember]
        public Double Increment { get; set; }

        /// <summary>Public Property - Ticks</summary>
        [DataMember]
        public Double Ticks { get; set; }

        /// <summary>Public Property - MajorTicks</summary>
        [DataMember]
        public Double MajorTicks { get; set; }

        /// <summary>Public Property - Decimals</summary>
        [DataMember]
        public int Decimals { get; set; }

        /// <summary>Public Property - Power</summary>
        [DataMember]
        public Double Power { get; set; }
    }
}
