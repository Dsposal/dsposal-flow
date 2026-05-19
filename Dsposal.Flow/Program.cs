using System.Text.Json;
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
        HelpText? helpText = null;

        var errList = errs.ToList();
        var isVersion = errList.IsVersion();
        var isHelp = errList.IsHelp();

        if (isVersion)
            helpText = HelpText.AutoBuild(results);
        else
        {
            helpText = HelpText.AutoBuild(results, h =>
            {
                h.AddEnumValuesToHelpText = true;
                h.AutoHelp = true;
                h.AutoVersion = true;

                return HelpText.DefaultParsingErrorsHandler(results, h);
            }, e => e);
        }

        AnsiConsole.Write(helpText);
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
    
    var logger = loggerFactory.CreateLogger("Dsposal.Flow");
    
    var fileService = new ExcelFileService(loggerFactory.CreateLogger<ExcelFileService>());
    if (!fileService.ResolveInputFiles(opts))
        return -1;

    logger.LogInformation("Starting processing...");
    logger.LogJson(LogLevel.Debug, JsonSerializer.Serialize(opts));
    // if (logger.IsEnabled(LogLevel.Debug))
    // {
    //     AnsiConsole.Write(new JsonText(JsonSerializer.Serialize(opts)));
    //     AnsiConsole.WriteLine();
    // }

    var processor = new NotQ100DataProcessor(loggerFactory.CreateLogger<NotQ100DataProcessor>());
    var results = await processor.ProcessFilesAsync(opts.ExcelFiles);

    logger.LogInformation("Finished processing all files. Total NotQ100 entries: [blue]{ResultsCount}[/]", results.Count);

    // var duplicateEntriesBasedOnRowIdent = results
    //     .GroupBy(x => x.RowIdent)
    //     .OrderByDescending(x => x.Count())
    //     .Take(3)
    //     .Select(x => x.First());
    // AnsiConsole.Write(new JsonText(JsonSerializer.Serialize(duplicateEntriesBasedOnRowIdent)));
    // AnsiConsole.WriteLine();
    //
    // var topThree = results.Take(3).ToList();
    // AnsiConsole.Write(new JsonText(JsonSerializer.Serialize(topThree)));
    // AnsiConsole.WriteLine();

    return 0;
}

internal abstract partial class Program
{
    [LoggerMessage(LogLevel.Information, "Found {Count} Excel files to process")]
    static partial void LogFoundCountExcelFilesToProcess(ILogger logger, int count);
}
