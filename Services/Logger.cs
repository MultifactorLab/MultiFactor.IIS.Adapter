using System;
using System.Diagnostics;

namespace MultiFactor.IIS.Adapter.Services
{
    /// <summary>
    /// EventLog
    /// </summary>
    public class Logger
    {
        private readonly string _source;

        public Logger(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public static Logger Owa => new Logger("Multifactor OWA");
        public static Logger API => new Logger("Multifactor API");
        public static Logger IIS => new Logger("Multifactor IIS");

        public void Info(string message)
        {
            WriteEvent(message, EventLogEntryType.Information);
        }

        public void Warn(string message)
        {
            WriteEvent(message, EventLogEntryType.Warning);
        }

        public void Error(string message)
        {
            WriteEvent(message, EventLogEntryType.Error);
        }

        private void WriteEvent(string message, EventLogEntryType type)
        {
            try
            {
                EventLog.WriteEntry(_source, message, type);
            }
            catch
            {
            }
        }
    }

    public static class ApplicationEvent
    {
        
    }
}