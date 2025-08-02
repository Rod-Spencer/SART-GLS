namespace Segway.Modules.Diagnostics_Helper
{
    public class QueueSyncObj
    {
        public bool CanChangeQueue
        {
            get;
            set;
        }

        public QueueSyncObj()
        {
            CanChangeQueue = true;
        }
    }
}
