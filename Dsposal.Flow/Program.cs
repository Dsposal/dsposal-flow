using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using CommandLine;
using CommandLine.Text;
using Dsposal.Flow.Extensions;
using Dsposal.Flow.Models.Cli;
using Dsposal.Flow.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Style = Dsposal.Flow.Models.Style;

var parser = new Parser(with => { with.CaseInsensitiveEnumValues = true; });

var results = parser.ParseArguments<Options>(args);

return await results.MapResult(
    Run,
    errs =>
    {
        var errList = errs.ToList();
        var isVersion = errList.IsVersion();
        var isHelp = errList.IsHelp();

        HelpText? helpText = isVersion
            ? HelpText.AutoBuild(results)
            : HelpText.AutoBuild(results, h =>
            {
                h.AddEnumValuesToHelpText = true;
                h.AddNewLineBetweenHelpSections = true;
                
                return HelpText.DefaultParsingErrorsHandler(results, h);
            }, e => e);
        
        AnsiConsole.Write(helpText);
        AnsiConsole.WriteLine(
            "Alternatively you can run this program with no input to get a interactive prompt to select valid Excel files to process.");

        return Task.FromResult(isVersion || isHelp ? 0 : -1);
    }
);

async Task<int> Run(Options opts)
{
    var loggerFactory = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(opts.LogLevel);
        b.AddSpectreConsole(Style.Extended);
    });

    if (string.IsNullOrEmpty(opts.OutputDir))
    {
        opts.OutputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
    }

    if (!Directory.Exists(opts.OutputDir))
    {
        Directory.CreateDirectory(opts.OutputDir);
    }

    var logger = loggerFactory.CreateLogger("Dsposal.Flow");

    var fileService = new ExcelFileService(loggerFactory.CreateLogger<ExcelFileService>());
    if (!fileService.ResolveInputFiles(opts))
        return -1;

    logger.LogInformation("Starting processing...");
    logger.LogJson(LogLevel.Debug, JsonSerializer.Serialize(opts));

    var processor = new DataProcessor(loggerFactory.CreateLogger<DataProcessor>());
    await processor.ProcessFilesAsync(opts.ExcelFiles);

    var csvProcessor = new CsvFileService(opts.OutputDir, loggerFactory.CreateLogger<CsvFileService>());
    await csvProcessor.WriteNormalizedFiles(processor.NotQ100Data, processor.Q100Data);

    await csvProcessor.WriteQ100(processor.Q100Data);

    return 0;
}

[ExcludeFromCodeCoverage] // composition root / CLI entry point
internal abstract partial class Program
{
    [LoggerMessage(LogLevel.Information, "Found {Count} Excel files to process")]
    static partial void LogFoundCountExcelFilesToProcess(ILogger logger, int count);
}