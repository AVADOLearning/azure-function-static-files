using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace AzureFunctionStaticFiles.Tests
{
    /// <summary>
    /// List logger.
    /// </summary>
    /// <remarks>
    /// Logs messages to a list, allowing for testing against the content of log messages.
    /// </remarks>
    class ListLogger : ILogger
    {
        /// <summary>
        /// Log entries.
        /// </summary>
        public IList<string> Entries;

        /// <summary>
        /// Begin a logging scope.
        /// </summary>
        /// <remarks>
        /// No-op; logging scopes aren't supported with this logger.
        /// </remarks>
        /// <param name="state">
        /// Silently discarded; logging scopes aren't supported with this logger.
        /// </param>
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        /// <summary>
        /// Is the logger enabled at the specified level?
        /// </summary>
        public bool IsEnabled(LogLevel logLevel) => false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ListLogger()
        {
            this.Entries = new List<string>();
        }

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);
            this.Entries.Add(message);
        }
    }
}
