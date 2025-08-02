using Segway.Service.CAN;

namespace Segway.Modules.Diagnostics_Helper
{
    public interface TestDataManager_Interface
    {
        EmbeddedFaults GetEmbeddedFaults(CAN_CU_Sides side);
    }
}
