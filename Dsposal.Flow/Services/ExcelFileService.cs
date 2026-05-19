using Dsposal.Flow.Models.Cli;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using XenoAtom.Logging;

namespace Dsposal.Flow.Services;

public class ExcelFileService(ILogger<ExcelFileService> logger)
{
    public bool ResolveInputFiles(Options opts)
    {
        if (opts.FilesAndDirs.All(string.IsNullOrEmpty))
        {
            var selected = SelectFilesInteractively();
            if (selected.Count == 0)
            {
                logger.LogCritical("No files selected, nothing to process");
                return false;
            }

            opts.FilesAndDirs = selected;
        }

        DiscoverExcelFiles(opts);

        if (opts.ExcelFiles.Count == 0)
        {
            logger.LogCritical("No Excel files found to process");
            return false;
        }

        return true;
    }

    private static List<string> SelectFilesInteractively()
    {
        var excelFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.xlsx", SearchOption.AllDirectories);

        return AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
            .Title("Select the [green]Excel files[/] you want to process:")
            .NotRequired()
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more files)[/]")
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle a file, [green]<enter>[/] to accept)[/]")
            .AddChoices(excelFiles));
    }

    private static void DiscoverExcelFiles(Options opts)
    {
        foreach (var path in opts.FilesAndDirs)
        {
            if (File.Exists(path) && path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                var fileInfo = new FileInfo(path);
                if (!opts.ExcelFiles.Any(f => f.FullName.Equals(fileInfo.FullName, StringComparison.OrdinalIgnoreCase)))
                    opts.ExcelFiles.Add(new FileInfo(path));
            }
            else if (Directory.Exists(path))
            {
                var searchOption = opts.RecursiveDirSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (var file in Directory.GetFiles(path, "*.xlsx", searchOption))
                {
                    if (!opts.ExcelFiles.Any(f => f.FullName.Equals(file, StringComparison.OrdinalIgnoreCase)))
                        opts.ExcelFiles.Add(new FileInfo(file));
                }
            }
        }
    }
}
