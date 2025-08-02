using Segway.Service.Objects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Data_Graph_Column</summary>
    [DataContract]
    [Serializable]
    public class Black_Box_Data_Graph_Column
    {
        /// <summary>Public Property - Column_Name</summary>
        [DataMember]
        public String Column_Name { get; set; }

        /// <summary>Public Property - Display_Name</summary>
        [DataMember]
        public String Display_Name { get; set; }

        /// <summary>Public Property - Color</summary>
        [DataMember]
        public String Color { get; set; }

        /// <summary>Public Property - ConversionI2</summary>
        [DataMember]
        public Double ConversionI2 { get; set; }

        /// <summary>Public Property - ConversionX2</summary>
        [DataMember]
        public Double ConversionX2 { get; set; }

        /// <summary>Public Method - Set_Data</summary>
        /// <param name="data">List&lt;Int16&gt;</param>
        public void Set_Data(List<Int16> data)
        {
            _Data = data;
        }


        #region Data

        private List<Int16> _Data;

        /// <summary>Property Data of type List&lt;Double&gt;</summary>
        public List<Double> Get_Data(Manufacturing_Models model)
        {
            if (_Data != null)
            {
                List<Double> d = new List<Double>();
                foreach (int i in _Data)
                {
                    if (model == Manufacturing_Models.G2_I2) d.Add(i * ConversionI2);
                    else if (model == Manufacturing_Models.G2_X2) d.Add(i * ConversionX2);
                    else throw new Exception(String.Format("Unknown model: {0}", model));
                }
                return d;
            }
            return null;
        }

        #endregion

    }
}
