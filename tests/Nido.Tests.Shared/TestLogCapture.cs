using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Nido.Tests.Shared;

public sealed class TestLogCapture : ILoggerProvider
{
    public ConcurrentBag<LogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

    public void Dispose() { }

    public IEnumerable<LogEntry> EntriesForCategory(string categoryName)
        => Entries.Where(e => e.Category == categoryName);

    public IEnumerable<LogEntry> EntriesForCategoryContaining(string fragment)
        => Entries.Where(e => e.Category.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    public void Clear()
    {
        while (Entries.TryTake(out _)) { }
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentBag<LogEntry> _sink;

        public CapturingLogger(string category, ConcurrentBag<LogEntry> sink)
        {
            _category = category;
            _sink = sink;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _sink.Add(new LogEntry(_category, logLevel, formatter(state, exception), exception));
        }
    }
}

public sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception);
