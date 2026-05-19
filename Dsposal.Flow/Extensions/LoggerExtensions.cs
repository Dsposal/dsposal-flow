using Microsoft.Extensions.Logging;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace Dsposal.Flow.Extensions;

public static class LoggerExtensions
{
    public static void LogRenderable(this ILogger logger, LogLevel logLevel, IRenderable renderable, Exception? exception = null)
        => logger.Log(logLevel, 0, renderable, exception, static (_, _) => string.Empty);

    public static void LogJson(this ILogger logger, LogLevel logLevel, string json, Exception? exception = null)
        => logger.LogRenderable(logLevel, new JsonText(json), exception);
}