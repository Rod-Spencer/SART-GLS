using System;
using Segway.Modules.Controls.MultiLevelToolBar;
using Segway.Service.Objects;

namespace Segway.Modules.SART_Infrastructure
{
    public class SART_ToolBar_Group_Manager
    {
        public delegate String GroupNameDelegate();


        #region Level

        private static UserLevels _Level;

        /// <summary>Property Level of type UserLevels</summary>
        public static UserLevels Level
        {
            get { return _Level; }
            set
            {
                _Level = value;
                InfrastructureModule.Aggregator.GetEvent<ToolBar_Activate_Permission_Event>().Publish(SART_ToolBar_Group_Manager.GetGroupName());
            }
        }

        #endregion

        #region IsOpen

        private static Boolean _IsOpen;

        /// <summary>Property IsOpen of type Boolean</summary>
        public static Boolean IsOpen
        {
            get { return _IsOpen; }
            set
            {
                _IsOpen = value;
                InfrastructureModule.Aggregator.GetEvent<ToolBar_Activate_Permission_Event>().Publish(SART_ToolBar_Group_Manager.GetGroupName());
            }
        }

        #endregion


        #region IsStartConfig

        private static Boolean _IsStartConfig;

        /// <summary>Property IsStartConfig of type Boolean</summary>
        public static Boolean IsStartConfig
        {
            get { return _IsStartConfig; }
            set
            {
                _IsStartConfig = value;
                InfrastructureModule.Aggregator.GetEvent<ToolBar_Activate_Permission_Event>().Publish(SART_ToolBar_Group_Manager.GetGroupName());
            }
        }

        #endregion

        public static String GetGroupName()
        {
            return $"{Level}|{IsOpen}|{IsStartConfig}";
        }
    }
}
