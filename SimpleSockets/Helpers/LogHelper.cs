using System;
using System.Globalization;
using System.Text;

namespace SimpleSockets.Helpers {

    internal class LogHelper {

        internal static LogHelper InitializeLogger(bool client, bool ssl, bool tcp, Action<string> logAction, LogLevel level) => new LogHelper(client, ssl, tcp, logAction, level);

        private string _prefix;

        private Action<string> _logger;

        private LogLevel _logLevel;

        internal void ChangeLogLevel(LogLevel level) {
            _logLevel = level;
        }

        private LogHelper (bool client, bool ssl, bool tcp, Action<string> logAction, LogLevel level) {

            _logger = logAction;
            _logLevel = level;

            if (client)
                _prefix = "[SimpleClient]";
            else
                _prefix = "[SimpleServer]";

            if (ssl)
                _prefix += "[Ssl]";

            if (tcp)
                _prefix += "[TCP]";
        }

        internal void Log(string log, LogLevel level) {

            if (level >= _logLevel) {
                if (_logger == null)
                    return;

                var builder = new StringBuilder();
                builder.AppendFormat(CultureInfo.CurrentCulture,"{0} [{1}] {2} - {3}", DateTime.Now, Enum.GetName(typeof(LogLevel), level).ToUpper(), _prefix, log);
                _logger?.Invoke(builder.ToString());
            }
        }

        internal void Log(string log, Exception exception, LogLevel level) {
            if (level >= _logLevel) {
                if (_logger == null)
                    return;
                var builder = new StringBuilder();
                builder.AppendFormat(CultureInfo.CurrentCulture, "{0}\n{1}", log, exception.ToString());
                Log(builder.ToString(), level);
            }
        }

        internal void Log(Exception exception, LogLevel level) {
            Log(exception.ToString(), level);
        }

    }

}