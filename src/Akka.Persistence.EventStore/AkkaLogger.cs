using System;
using Akka.Event;
using EventStore.ClientAPI;

namespace Akka.Persistence.EventStore
{
    public sealed class AkkaLogger : ILogger
    {
        private readonly ILoggingAdapter _log;

        public AkkaLogger(ILoggingAdapter log)
        {
            _log = log;
        }

        public void Error(string format, params object[] args)
        {
            _log.Error(format, args);
        }

        public void Error(Exception ex, string format, params object[] args)
        {
            _log.Error(ex, format, args);
        }

        public void Info(string format, params object[] args)
        {
            _log.Info(format, args);
        }

        public void Info(Exception ex, string format, params object[] args)
        {
            _log.Info(format, args);
            _log.Warning(ex.Message);
        }

        public void Debug(string format, params object[] args)
        {
            _log.Info(format, args);
        }

        public void Debug(Exception ex, string format, params object[] args)
        {
            _log.Debug(format, args);
            _log.Warning(ex.Message);
        }
    }
}