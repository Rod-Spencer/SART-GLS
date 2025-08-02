namespace Segway.Modules.CU_Log_Module
{
    public enum Masks : ushort
    {
        MOTOR_DRIVE_FAULT_MASK = 0xFFFB,
        REMOTE_COMM_MASK = 0xFC3F,
    }

    public enum Fault_Extract_Status
    {
        Short = -1,
        Good = 0,
        Long = 1,
        Unknown,
    }

}
