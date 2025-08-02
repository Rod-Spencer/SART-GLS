using System;
using System.ComponentModel;

namespace Segway.Modules.Administration
{
    public class appSetting
    {
        [ReadOnly(true)]
        public String Key { get; set; }
        public String Value { get; set; }
        public appSetting(String key, String value) { this.Key = key; this.Value = value; }

        public override string ToString()
        {
            return $"Key: {Key}, Value: {Value}";
        }
    }

}
