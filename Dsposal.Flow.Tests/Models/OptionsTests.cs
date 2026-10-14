using Dsposal.Flow.Models.Cli;
using Microsoft.Extensions.Logging;

namespace DsposalForgeTests.Models;

public class OptionsTests
{
    [Test]
    public void Properties_RoundTrip()
    {
        var options = new Options
        {
            FilesAndDirs = new[] { "a.xlsx" },
            OutputDir = "./out",
            LogLevel = LogLevel.Debug,
            RecursiveDirSearch = false,
        };

        Assert.Multiple(() =>
        {
            Assert.That(options.FilesAndDirs, Is.EqualTo(new[] { "a.xlsx" }));
            Assert.That(options.OutputDir, Is.EqualTo("./out"));
            Assert.That(options.LogLevel, Is.EqualTo(LogLevel.Debug));
            Assert.That(options.RecursiveDirSearch, Is.False);
            Assert.That(options.ExcelFiles, Is.Not.Null);
        });
    }

    [Test]
    public void Examples_AreAvailable()
    {
        Assert.That(Options.Examples, Is.Not.Empty);
    }
}
