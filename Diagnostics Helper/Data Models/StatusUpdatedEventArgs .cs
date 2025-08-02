using System;

namespace Segway.Modules.Diagnostics_Helper
{
    public class StatusUpdatedEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public StatusUpdatedEventArgs(string msg)
        {
            Message = msg;
        }
    }
}
