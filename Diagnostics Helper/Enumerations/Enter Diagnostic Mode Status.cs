using System;

namespace Segway.Modules.Diagnostics_Helper
{
    public enum Enter_Diagnostic_Mode_Status
    {
        Success = 0,
        No_Heartbeat_A = 1,
        No_Heartbeat_B = 2,
        No_Heartbeat_AB = 3,
        Failed_Unlock_Diagnostic_A = 4,
        Failed_Unlock_Diagnostic_B = 8,
        Failed_Unlock_Diagnostic_AB = 12,
        Failed_Watch_Setup = 16,
        Failed_Enter_Diagnostic_Timeout = 32
    }
}
