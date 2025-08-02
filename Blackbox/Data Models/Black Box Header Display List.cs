using System;
using System.Collections.Generic;
using System.Text;

namespace Segway.Service.SART
{
    /// <summary>Public Class - Black_Box_Header_Display_List</summary>
    public class Black_Box_Header_Display_List
    {

        #region BBH_Data

        private List<Black_Box_Header_Display> _BBH_Data;

        /// <summary>Property BBH_Data of type List&lt;Black_Box_Header_Display&gt;</summary>
        public List<Black_Box_Header_Display> BBH_Data
        {
            get
            {
                if (_BBH_Data == null)
                {
                    _BBH_Data = new List<Black_Box_Header_Display>();
                }
                return _BBH_Data;
            }
            set
            {
                _BBH_Data = value;
            }
        }

        #endregion

    }
}
