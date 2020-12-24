using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace HappyHour.ViewModel
{
    class DebugLogViewModel : TextViewModel
    {
        public DebugLogViewModel()
        {
            Title = "Debug";
            var appenders = LogManager.GetRepository().GetAppenders();
            var appender = appenders.First(a => a.Name == "InAppAppender");
            if (appender is InAppAppender)
            {
                (appender as InAppAppender).LogAppended += OnLog4netEvent;
            }
        }

        public void OnLog4netEvent(object sender, ViewEventArgs args)
        {
            UiServices.Invoke(delegate ()
            {
                if (System.Windows.Application.Current != null)
                    AppendText((string)args.Data);
            }, true);
        }
    }
}
