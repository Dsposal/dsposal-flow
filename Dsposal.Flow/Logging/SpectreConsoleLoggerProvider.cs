using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Style = Dsposal.Flow.Models.Style;

namespace Dsposal.Flow.Logging;

public class SpectreConsoleLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, SpectreConsoleLogger> _loggers = new();

    /// <summary>
    /// Determines the logging style that should be used by the loggers.
    /// </summary>
    public Style Style { get; set; } = Style.Standard;

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, new SpectreConsoleLogger(categoryName, Style));

    /// <summary>
    /// Releases all resources used by the <see cref="SpectreConsoleLoggerProvider"/> object.
    /// </summary>
    public void Dispose() => _loggers.Clear();
}