using Segway.Service.CAN;

namespace Segway.Modules.Diagnostics_Helper
{
    public class CU
    {
        public CAN_CU_Sides ID
        {
            get;
            private set;
        }

        public bool IsConnected
        {
            get
            {
                return true;
            }
        }

        public CU(CAN_CU_Sides id)
        {
            this.ID = id;
        }
    }
}
