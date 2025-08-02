using System;

namespace Segway.Modules.Diagnostics_Helper
{
    public class Key64
    {
        public ushort[] ID { get; set; }

        public ushort this[int ndx]
        {
            get
            {
                if (ndx < 0 || ndx >= (int)ID.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return ID[ndx];
            }
            set
            {
                if (ndx < 0 || ndx >= (int)ID.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }
                ID[ndx] = value;
            }
        }

        public Key64()
        {
            ID = new ushort[4];
        }
    }
}
