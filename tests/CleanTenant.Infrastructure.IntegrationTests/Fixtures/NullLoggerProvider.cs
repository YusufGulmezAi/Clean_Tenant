using Microsoft.Extensions.Logging;

namespace CleanTenant.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Testlerde gürültüyü azaltmak için kullanılan no-op logger sağlayıcısı.
/// </summary>
internal sealed class NullLoggerProvider : ILoggerProvider
{
    public static readonly NullLoggerProvider Instance = new();

    public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

    public void Dispose() { }

    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        { }
    }
}
