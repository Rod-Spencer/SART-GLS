using System;
using System.Runtime.InteropServices;

namespace Segway.Modules.Diagnostics_Helper
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KEY64
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] key;
    }


    public class Key_64
    {
        public KEY64 Base;

        public ushort this[int ndx]
        {
            get
            {
                if (ndx < 0 || ndx >= (int)Key.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return Key[ndx];
            }
        }

        public ushort[] Key
        {
            get
            {
                return Base.key;
            }
        }

        public Key_64()
        {
            Base.key = new ushort[4];
        }
    }
}
