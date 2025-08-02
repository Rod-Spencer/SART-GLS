using System;
using System.Runtime.Serialization;

namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Graph_Size</summary>
    [Serializable]
    [DataContract]
    public class Black_Box_Graph_Size
    {
        /// <summary>Public Property - Width</summary>
        [DataMember]
        public Double Width { get; set; }

        /// <summary>Public Property - Height</summary>
        [DataMember]
        public Double Height { get; set; }


        /// <summary>Contructor</summary>
        public Black_Box_Graph_Size() : this(0, 0) { }

        /// <summary>Contructor</summary>
        /// <param name="width">Double</param>
        /// <param name="height">Double</param>
        public Black_Box_Graph_Size(Double width, Double height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>Public Contructor - override</summary>
        public override string ToString()
        {
            return String.Format("W:{0}, H:{1}", Width, Height);
        }
    }
}
