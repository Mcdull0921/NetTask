using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskManager
{
    public class Logger
    {
        NLog.Logger logger;
        public string assembly { get; private set; }
        public Logger(string name, string assembly)
        {
            logger = LogManager.GetLogger(name);
            this.assembly = assembly;
        }

        public void Debug(string message)
        {
            var logInfo = new LogEventInfo();
            logInfo.Level = LogLevel.Debug;
            logInfo.Properties["assembly"] = assembly;
            logInfo.Message = message;
            logger.Log(logInfo);
        }

        public void Error(string message, Exception ex)
        {
            var logInfo = new LogEventInfo();
            logInfo.Level = LogLevel.Error;
            logInfo.Properties["assembly"] = assembly;
            logInfo.Exception = ex;
            logInfo.Message = message;
            logger.Log(logInfo);
        }

        public void Info(string message)
        {
            var logInfo = new LogEventInfo();
            logInfo.Level = LogLevel.Info;
            logInfo.Properties["assembly"] = assembly;
            logInfo.Message = message;
            logger.Log(logInfo);
        }
    }
}
