using System.Text.Json.Serialization;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;

namespace Dsposal.Flow.Models.Cli;

public class Options
{
    [
        Option('i', "input",
            Required = false,
            HelpText = "The files and directories to process as inputs."
        ),
    ]
    public IEnumerable<string> FilesAndDirs { get; set; }

    [Option('o', "output",
        Required = false,
        HelpText = "The directory to write the output files to.",
        Default = "./output"
    )]
    public string OutputDir { get; set; }

    [
        Option('l', "log-level",
            Required = false,
            HelpText = "The log level to use.",
            Default = LogLevel.Information
        ),
        JsonConverter(typeof(JsonStringEnumConverter)),
    ]
    public LogLevel LogLevel { get; set; }

    [
        Option('r', "recursive-dir-search",
            Required = false,
            HelpText = "Whether to search for excel files recursively in the input directories.",
            Default = true
        )
    ]
    public bool RecursiveDirSearch { get; set; }

    [Usage]
    public static IEnumerable<Example> Examples => new[]
    {
        new Example("Basic usage", formatStyle: new UnParserSettings() { PreferShortName = true, SkipDefault = true },
            new Options { FilesAndDirs = new[] { "WDF.xlxs" } })
    };

    internal List<FileInfo> ExcelFiles = new();
}
