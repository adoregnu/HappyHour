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

        public enum Tag
        {
            Info,
            Warn,
            Error
        }
        public static void Print(Tag tag, string format, params object[] args)
        {
            switch (tag)
            {
                case Tag.Info:
                    _log.Info(string.Format(format, args));
                    break;
                case Tag.Warn:
                    _log.Warn(string.Format(format, args));
                    break;
                case Tag.Error:
                    _log.Error(string.Format(format, args));
                    break;
            }
        }
        public static void Print(string format, params object[] args)
        {
            //Debug.Print(format, args);
            _log.Info(string.Format(format, args));
        }
        public static void Print(string message, Exception e)
        {
            _log.Error(message, e);
        }

        public static void MessageBox(Tag tag, string msg)
        {
            _log.Info(msg);
            System.Windows.MessageBox.Show(msg, tag.ToString());
        }
        public static void MessageBox(Tag tag, string msg, Exception e)
        {
            _log.Info(msg, e);
            System.Windows.MessageBox.Show(msg, tag.ToString());
        }
#if false
        public static void Modaless(Tag tag, string msg)
        {
            View.SimpleDialogBox msgbox = new View.SimpleDialogBox(tag.ToString(), msg);
            msgbox.Owner = Application.Current.MainWindow;
            msgbox.Show();
        }
#endif
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
