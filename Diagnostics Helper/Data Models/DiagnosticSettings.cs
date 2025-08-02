using System;
using System.Runtime.CompilerServices;

namespace Segway.Modules.Diagnostics_Helper
{
    public class DiagnosticSettings
    {
        public static PowerUpModes PowerMode { get; set; }

        static DiagnosticSettings()
        {
            PowerMode = PowerUpModes.POWER_UP_DEFAULT;
        }
    }
}
