using System;
using System.Text;

namespace Segway.Modules.Diagnostics_Helper
{
    public class CUFrameData
    {
        private Int16[] _data = null;
        public Int16[] Data
        {
            get
            {
                if (_data == null) _data = new Int16[16];
                return _data;
            }
        }


        public CUFrameData() { }

        public CUFrameData(Int16[] data)
        {
            for (int x = 0; (x < Data.Length) && (x < data.Length); x++) Data[x] = data[x];
        }

        public Int16 this[Int32 index]
        {
            get
            {
                if (index >= Data.Length) throw new IndexOutOfRangeException(String.Format("Index: {0} is greater than array Data: {1}", index, Data.Length));
                return Data[index];
            }
            set
            {
                if (index >= Data.Length) throw new IndexOutOfRangeException(String.Format("Index: {0} is greater than array Data: {1}", index, Data.Length));
                Data[index] = value;
            }
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int x = 0; x < Data.Length; x++)
            {
                sb.AppendFormat("{0:X4} ", Data[x]);
            }
            return sb.ToString().TrimEnd();
        }
    }
}
