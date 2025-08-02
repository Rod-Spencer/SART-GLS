using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Segway.Service.LoggerHelper;
using NLog;

namespace Segway.Modules.Diagnostics_Helper
{
    public class SafeQueue<T>
    {
        private Logger logger = Logger_Helper.GetCurrentLogger();
        private Queue<T> inputQueue = new Queue<T>();

        public void Enqueue(T value)
        {
            //logger.Debug("Entering {0}", MethodInfo.GetCurrentMethod().Name);
            Monitor.Enter(inputQueue);
            try
            {
                inputQueue.Enqueue(value);
            }
            finally
            {
                Monitor.Exit(inputQueue);
            }
            //logger.Debug("Exiting {0}", MethodInfo.GetCurrentMethod().Name);
        }

        public bool TryEnqueue(T value)
        {
            if (Monitor.TryEnter(inputQueue))
            {
                try
                {
                    inputQueue.Enqueue(value);
                }
                finally
                {
                    Monitor.Exit(inputQueue);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryEnqueue(T value, int waitTime)
        {
            if (Monitor.TryEnter(inputQueue, waitTime))
            {
                try
                {
                    inputQueue.Enqueue(value);
                }
                finally
                {
                    Monitor.Exit(inputQueue);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public T Dequeue()
        {
            //logger.Debug("Entering {0}", MethodInfo.GetCurrentMethod().Name);
            //T retval;
            Monitor.Enter(inputQueue);

            try
            {
                if (inputQueue.Count > 0) return inputQueue.Dequeue();
                return default(T);
            }
            finally
            {
                Monitor.Exit(inputQueue);
            }
            //logger.Debug("Exiting {0}", MethodInfo.GetCurrentMethod().Name);
        }

        public T LastDequeue()
        {
            T retval;
            Monitor.Enter(inputQueue);

            try
            {
                retval = inputQueue.Last<T>();
            }
            finally
            {
                Monitor.Exit(inputQueue);
            }

            return retval;
        }

        private Double CalculateAverage(T t)
        {
            return Convert.ToDouble(t);
        }

        public void Clear()
        {
            Monitor.Enter(inputQueue);

            try
            {
                inputQueue.Clear();
            }
            finally
            {
                Monitor.Exit(inputQueue);
            }
        }

        public int Remove(T value)
        {
            int removedCt = 0;
            Monitor.Enter(inputQueue);

            try
            {
                int counter = inputQueue.Count;

                while (counter > 0)
                {
                    T elem = inputQueue.Dequeue();

                    if (!elem.Equals(value))
                    {
                        inputQueue.Enqueue(elem);
                    }
                    else
                    {
                        removedCt += 1;
                    }

                    counter = counter - 1;
                }
            }
            finally
            {
                Monitor.Exit(inputQueue);
            }

            return removedCt;
        }

        public int Count
        {
            get
            {
                int retval = 0;
                Monitor.Enter(inputQueue);

                try
                {
                    retval = inputQueue.Count;
                }
                finally
                {
                    Monitor.Exit(inputQueue);
                }

                return retval;
            }
        }
    }
}
