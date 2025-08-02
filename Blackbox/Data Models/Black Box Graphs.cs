using Segway.Service.Common;
using Segway.Service.LoggerHelper;
using System;
using System.Collections.Generic;

namespace Segway.Service.SART
{
    /// <summary></summary>
    public class Black_Box_Graphs : Persistence
    {
        private static readonly String File_Name = "Black Box Graphs Definitions.xml";


        #region Graphs

        private List<Black_Box_Data_Graph> _Graphs;

        /// <summary>Property Graphs of type List&lt;Black_Box_Data_Graph&gt;</summary>
        public List<Black_Box_Data_Graph> Graphs
        {
            get
            {
                if (_Graphs == null) _Graphs = new List<Black_Box_Data_Graph>();
                return _Graphs;
            }
            set
            {
                _Graphs = value;
            }
        }

        #endregion

        /// <summary>Public Constructor - Black_Box_Graphs</summary>
        public Black_Box_Graphs() : base(File_Name) { }


        /// <summary>Public Method - Save</summary>
        public void Save()
        {
            Save<List<Black_Box_Data_Graph>>(Graphs);
        }

        /// <summary>Public Method - Load</summary>
        public void Load()
        {
            Graphs = Load<List<Black_Box_Data_Graph>>();
            if ((Graphs == null)||(Graphs.Count == 0)) 
            {
                Logger_Helper.GetCurrentLogger().Debug("Graphs Definitions is null");
                String defs = Embedded_Helper.GetEmbeddedContentString(".Black Box Graphs Definitions.xml");
                Graphs = Serialization.Deserialize<List<Black_Box_Data_Graph>>(defs);
                Save();
            }

            var graph = Find("Pitch and Roll Angle, Desired Pitch Offset and Steering Sensor Angle");
            if (graph == null)
            {
                graph = new Black_Box_Data_Graph();
                graph.Graph_Name = "Pitch and Roll Angle, Desired Pitch Offset and Steering Sensor Angle";
                graph.YAxis.Max = 10;
                graph.YAxis.Min = -70;
                graph.YAxis.Increment = 10;
                Black_Box_Data_Graph_Column col = new Black_Box_Data_Graph_Column();
                col.Color = "Blue";
                col.Column_Name = "Pitch_Angle";
                col.ConversionI2 = 0.12853470437017994858611825192802;
                col.ConversionX2 = 0.12853470437017994858611825192802;
                col.Display_Name = "Pitch";
                graph.Column_Data.Add(col);

                col = new Black_Box_Data_Graph_Column();
                col.Color = "Green";
                col.Column_Name = "Roll_Angle";
                col.ConversionI2 = 0.12853470437017994858611825192802;
                col.ConversionX2 = 0.12853470437017994858611825192802;
                col.Display_Name = "Roll";
                graph.Column_Data.Add(col);

                col = new Black_Box_Data_Graph_Column();
                col.Color = "Red";
                col.Column_Name = "Desired_Pitch_Offset";
                col.ConversionI2 = 0.12853470437017994858611825192802;
                col.ConversionX2 = 0.12853470437017994858611825192802;
                col.Display_Name = "Des. Pitch Off";
                graph.Column_Data.Add(col);

                col = new Black_Box_Data_Graph_Column();
                col.Color = "Orange";
                col.Column_Name = "Local_Steering_Sensor";
                col.ConversionI2 = 0.015625;
                col.ConversionX2 = 0.015625;
                col.Display_Name = "Steering Sensor";
                graph.Column_Data.Add(col);
                Graphs.Add(graph);
                Save<List<Black_Box_Data_Graph>>(Graphs);
            }
        }

        /// <summary>Public Method - Find</summary>
        /// <param name="graphName">String</param>
        /// <returns>Black_Box_Data_Graph</returns>
        public Black_Box_Data_Graph Find(String graphName)
        {
            foreach (var graph in Graphs)
            {
                if (graph.Graph_Name == graphName) return graph;
            }
            return null;
        }
    }
}
