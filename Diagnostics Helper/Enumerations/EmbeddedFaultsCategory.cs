using System;

namespace Segway.Modules.Diagnostics_Helper
{
    public enum EmbeddedFaultsCategory
    {
        TransientHazard = 1,
        CriticalHazard = 2,
        CommunicationFaults = 3,
        SensorLocalFaults = 4,
        SensorRemoteFaults = 5,
        ActuatorLocalFaults = 6,
        GeneralPuposeWord1 = 7,
        GeneralPuposeWord2 = 8,
        GeneralPuposeWord3 = 9,
        GeneralPuposeWord4 = 10
    }
}
