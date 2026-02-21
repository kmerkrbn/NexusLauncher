using System;
using System.Windows;

namespace MClauncher.Services
{
    public interface ILogService
    {
        void Log(string message);
        event EventHandler<LogEventArgs>? LogMessageAdded;
    }

    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime Timestamp { get; }

        public LogEventArgs(string message)
        {
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    public class LogService : ILogService
    {
        public event EventHandler<LogEventArgs>? LogMessageAdded;

        public void Log(string message)
        {
            var args = new LogEventArgs(message);
            LogMessageAdded?.Invoke(this, args);
        }
    }
}
