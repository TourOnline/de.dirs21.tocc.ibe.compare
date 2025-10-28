using System;
using Microsoft.Extensions.Logging;

namespace TOCC.IBE.Compare.Tests.Helpers
{
    /// <summary>
    /// Test implementation of ILogger for integration tests.
    /// Logs to console for test visibility.
    /// </summary>
    public class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            Console.WriteLine($"[{logLevel}] {message}");
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception}");
            }
        }
    }
}
