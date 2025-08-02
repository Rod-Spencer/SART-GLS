using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Segway.Service.BlackBox.Reporter;
using Segway.Service.Objects;


namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Data_Graph</summary>
    [Serializable]
    [DataContract]
    public class Black_Box_Data_Graph
    {
        /// <summary>Public Property - Graph_Name</summary>
        [DataMember]
        public String Graph_Name { get; set; }

        /// <summary>Public Property - Legend</summary>
        [DataMember]
        public Black_Box_Graph_Legend_Types Legend { get; set; }


        #region XAxis

        private Black_Box_Graph_Axis _XAxis;

        /// <summary>Property XAxis of type Black_Box_Graph_Axis</summary>
        [DataMember]
        public Black_Box_Graph_Axis XAxis
        {
            get
            {
                if (_XAxis == null)
                {
                    _XAxis = new Black_Box_Graph_Axis();
                    _XAxis.Axis = Black_Box_Graph_Axis_Types.X;
                    _XAxis.Label = "Time";
                    _XAxis.Increment = 1;
                    _XAxis.Min = 0;
                    _XAxis.Max = 12;
                    _XAxis.Ticks = 300;
                }
                return _XAxis;
            }
            set { _XAxis = value; }
        }

        #endregion


        #region YAxis

        private Black_Box_Graph_Axis _YAxis;

        /// <summary>Property YAxis of type Black_Box_Graph_Axis</summary>
        [DataMember]
        public Black_Box_Graph_Axis YAxis
        {
            get
            {
                if (_YAxis == null)
                {
                    _YAxis = new Black_Box_Graph_Axis();
                    _YAxis.Axis = Black_Box_Graph_Axis_Types.Y;
                }
                return _YAxis;
            }
            set { _YAxis = value; }
        }

        #endregion


        #region AreaSize

        private Black_Box_Graph_Size _AreaSize;

        /// <summary>Property AreaSize of type Black_Box_Graph_Size</summary>
        [DataMember]
        public Black_Box_Graph_Size AreaSize
        {
            get
            {
                if (_AreaSize == null) _AreaSize = new Black_Box_Graph_Size();
                return _AreaSize;
            }
            set
            {
                _AreaSize = value;
            }
        }

        #endregion


        #region GraphSize

        private Black_Box_Graph_Size _GraphSize;

        /// <summary>Property Size of type Black_Box_Graph_Size</summary>
        [DataMember]
        public Black_Box_Graph_Size GraphSize
        {
            get
            {
                if (_GraphSize == null) _GraphSize = new Black_Box_Graph_Size();
                return _GraphSize;
            }
            set { _GraphSize = value; }
        }

        #endregion


        #region Column_Data

        private List<Black_Box_Data_Graph_Column> _Column_Data;

        /// <summary>Property Column_Data of type List&lt;Black_Box_Data_Graph_Column&gt;</summary>
        [DataMember]
        public List<Black_Box_Data_Graph_Column> Column_Data
        {
            get
            {
                if (_Column_Data == null) _Column_Data = new List<Black_Box_Data_Graph_Column>();
                return _Column_Data;
            }
            set
            {
                _Column_Data = value;
            }
        }

        #endregion

        /// <summary>Public Contructor - Black_Box_Data_Graph</summary>
        public Black_Box_Data_Graph()
        {
            Legend = Black_Box_Graph_Legend_Types.Below;
        }

        /// <summary>Public Method - Data</summary>
        /// <param name="columnName">String</param>
        /// <param name="model">Manufacturing_Models</param>
        /// <returns>List&lt;Double&gt;</returns>
        public List<Double> Data(String columnName, Manufacturing_Models model)
        {
            if (String.IsNullOrEmpty(columnName) == true) return null;
            if (Black_Box_Data_Graph_Data.Data.ContainsKey(columnName) == false) return null;
            Black_Box_Data_Graph_Column gc = Find(columnName);
            if (gc == null) return null;
            gc.Set_Data(Black_Box_Data_Graph_Data.Data[columnName]);
            return gc.Get_Data(model);
        }


        /// <summary>Public Method - Find</summary>
        /// <param name="columnName">String</param>
        /// <returns>Black_Box_Data_Graph_Column</returns>
        public Black_Box_Data_Graph_Column Find(String columnName)
        {
            foreach (var col in Column_Data)
            {
                if (col.Column_Name == columnName) return col;
            }
            return null;
        }

        /// <summary>Public Contructor - override</summary>
        public override string ToString()
        {
            return Graph_Name;
        }
    }
}
