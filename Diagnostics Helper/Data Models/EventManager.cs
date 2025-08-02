using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Segway.Modules.Diagnostics_Helper
{
    public class EventManager
    {
        public static event StatusUpdatedEventHandler StatusUpdated;

        public EventManager()
        {
        }

        public static void UpdateStatus(string status)
        {
            if (EventManager.StatusUpdated != null)
            {
                EventManager.StatusUpdated(new EventManager(), new StatusUpdatedEventArgs(status));
            }
        }
    }
}
