using System.Runtime.Serialization;

namespace Segway.Service.BlackBox.Reporter
{
    /// <summary></summary>
    public enum Black_Box_Graph_Legend_Types
    {
        /// <summary></summary>
        [EnumMember]
        UnDefined = 0,

        /// <summary></summary>
        [EnumMember]
        Below = 1,

        /// <summary></summary>
        [EnumMember]
        Right = 2,

        /// <summary></summary>
        [EnumMember]
        Left = 3,

        /// <summary></summary>
        [EnumMember]
        Top = 4,

        /// <summary></summary>
        [EnumMember]
        InLine = 5,
    }
}
