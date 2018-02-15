using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildHelper
{
    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(string logMessage)
        {
            LogMessage = logMessage;
        }

        public string LogMessage { get; }
    }

    public delegate void LogEventHandler(object sender, LogEventArgs eventArgs);

    public abstract class EventLogger
    {
        public event LogEventHandler OnLogEvent;

        protected void LogEvent(string logMessage)
        {
            OnLogEvent?.Invoke(this, new LogEventArgs(logMessage));
        }
    }
}
