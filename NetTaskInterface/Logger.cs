using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskInterface
{
    public class Logger
    {
        public event Action<string> onInfo;
        public event Action<string, Exception> onError;

        public void Info(string message)
        {
            onInfo?.Invoke(message);
        }

        public void Error(Exception ex, string message = "")
        {
            onError?.Invoke(message, ex);
        }
    }
}
