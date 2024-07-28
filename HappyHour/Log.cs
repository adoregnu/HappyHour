using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using log4net;
using log4net.Appender;
using log4net.Core;

namespace HappyHour
{
    static class Log
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(App));

        public static void Print(string message)
        {
            //Debug.Print(format, args);
            _log.Info(message);
        }
        public static void Print(string message, Exception e)
        {
            _log.Error(message, e);
        }
    }
    class InAppAppender : AppenderSkeleton
    {
        public event EventHandler<ViewEventArgs> LogAppended;

        override protected void Append(LoggingEvent loggingEvent)
        {
            LogAppended?.Invoke(this, new ViewEventArgs("log4net",
                RenderLoggingEvent(loggingEvent)));
        }
    }
}
