namespace Segway.Modules.Diagnostics_Helper
{
    public enum WakeConfiguration
    {
        WakeAStartService = 1,
        WakeAStartNormal = 2,
        WakeAHigh = 4,
        WakeAOff = 8,
        WakeBStartService = 16,
        WakeBStartNormal = 32,
        WakeBHigh = 64,
        WakeBOff = 128
    }
}
