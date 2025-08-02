using System;

namespace Segway.Modules.Diagnostics_Helper
{
    public class WatchVariable
    {
        public String Name { get; set; }
        public Int16 Value { get; set; }
        public Int32 Address { get; set; }

        public WatchVariable() { }

        public WatchVariable(String name, Int32 address)
        {
            Name = name;
            Address = address;
        }

        public WatchVariable(WatchVariable wv)
        {
            Name = wv.Name;
            Address = wv.Address;
            Value = wv.Value;
        }

        public override String ToString()
        {
            return String.Format("N:{0}, A:{1:X4}, V:{2:X4}", Name, Address, Value);
        }
    }
}
