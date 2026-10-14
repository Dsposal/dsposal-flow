using Dsposal.Flow.Extensions;
using Dsposal.Flow.Logging;
using Dsposal.Flow.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Style = Dsposal.Flow.Models.Style;

namespace DsposalForgeTests.Logging;

public class LoggerTests
{
    private static readonly LogLevel[] AllLevels =
    [
        LogLevel.Trace, LogLevel.Debug, LogLevel.Information,
        LogLevel.Warning, LogLevel.Error, LogLevel.Critical,
    ];

    [Test]
    public void Log_EveryLevel_StandardStyle()
    {
        var logger = new SpectreConsoleLogger("Test.Category", Style.Standard);

        foreach (var level in AllLevels)
            logger.Log(level, new EventId(1), "message", null, (state, _) => state);
    }

    [Test]
    public void Log_EveryLevel_ExtendedStyle()
    {
        var logger = new SpectreConsoleLogger("Test.Category", Style.Extended);

        foreach (var level in AllLevels)
            logger.Log(level, new EventId(1), "message", null, (state, _) => state);
    }

    [Test]
    public void Log_WithException_AppendsExceptionRow()
    {
        var logger = new SpectreConsoleLogger("Test.Category", Style.Extended);

        logger.Log(LogLevel.Error, new EventId(1), "boom", new InvalidOperationException("kaboom"),
            (state, _) => state);
    }

    [Test]
    public void Log_WithRenderableState_UsesDefaultBranch()
    {
        var logger = new SpectreConsoleLogger("Test.Category", Style.Extended);

        logger.LogRenderable(LogLevel.Information, new Text("renderable"));
    }

    [Test]
    public void Log_WhenDisabled_DoesNothing()
    {
        var logger = new SpectreConsoleLogger("Test.Category", Style.Standard);

        logger.Log(LogLevel.None, new EventId(1), "ignored", null, (state, _) => state);
    }

    [Test]
    public void IsEnabled_IsTrueForEverythingExceptNone()
    {
        var logger = new SpectreConsoleLogger("Test.Category", Style.Standard);

        Assert.That(logger.IsEnabled(LogLevel.None), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Information), Is.True);
    }

    [Test]
    public void BeginScope_ReturnsDisposableScope()
    {
        var logger = new SpectreConsoleLogger("Test.Category", Style.Standard);

        using var scope = logger.BeginScope("state");
        Assert.That(scope, Is.Not.Null);
    }

    [Test]
    public void LogJson_ForwardsToRenderable()
    {
        var logger = new SpectreConsoleLogger("Test.Category", Style.Standard);

        logger.LogJson(LogLevel.Debug, "{\"foo\":\"bar\"}");
    }

    [Test]
    public void Provider_CachesLoggersAndExposesStyle()
    {
        using var provider = new SpectreConsoleLoggerProvider { Style = Style.Extended };

        var first = provider.CreateLogger("cat");
        var second = provider.CreateLogger("cat");

        Assert.That(second, Is.SameAs(first));
        Assert.That(provider.Style, Is.EqualTo(Style.Extended));
    }

    [Test]
    public void AddSpectreConsole_RegistersProviderAndCreatesLoggers()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSpectreConsole(Style.Extended));

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger("Dsposal.Flow");

        Assert.That(logger, Is.Not.Null);
        Assert.That(serviceProvider.GetServices<ILoggerProvider>(), Has.Some.InstanceOf<SpectreConsoleLoggerProvider>());
    }
}
