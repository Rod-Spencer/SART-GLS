using System;

namespace Segway.Modules.WorkOrder.Services
{
    public class Repair_Exception : Exception
    {
        public Repair_Exception() : base() { }

        public Repair_Exception(String msg) : base(msg) { }
    }
}
