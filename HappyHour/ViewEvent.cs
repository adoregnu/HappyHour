using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace HappyHour
{
    public class ViewEventArgs : EventArgs
    {
        public ViewEventArgs(string msg, object data)
        {
            Message = msg;
            Data = data;
        }

        public string Message { get; }

        public object Data { get; }
    }
}
