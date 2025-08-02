using System;

namespace Segway.Modules.Diagnostics_Helper
{
    public class PTIException : Exception
    {
        public int ErrorCode { get; private set; }

        public string ErrorMessage { get; private set; }

        public PTIException()
        {
        }

        public PTIException(string message) : base(message)
        {
        }

        public PTIException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public PTIException(string msg, string errMsg, int errCode) : this(msg)
        {
            this.ErrorMessage = errMsg;
            this.ErrorCode = errCode;
        }

        public PTIException(string msg, string errMsg, int errCode, Exception innerException) : this(msg, innerException)
        {
            this.ErrorMessage = errMsg;
            this.ErrorCode = errCode;
        }
    }
}
