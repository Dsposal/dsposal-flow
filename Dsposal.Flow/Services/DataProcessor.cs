using System.Collections.Concurrent;
using Dsposal.Flow.Extensions;
using Dsposal.Flow.Models.WDF;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sylvan.Data;
using Sylvan.Data.Excel;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Dsposal.Flow.Services;

public class DataProcessor(ILogger<DataProcessor> logger)
{
    public readonly ConcurrentBag<NotQ100Data> NotQ100Data = [];
    public readonly ConcurrentBag<Q100Data> Q100Data = [];

    public async Task ProcessFilesAsync(List<FileInfo> excelFiles)
    {
        await AnsiConsole
            .Progress()
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn(),
            ])
            .StartAsync(async ctx =>
            {
                using var semaphore = new SemaphoreSlim(4);

                var tasks = excelFiles.Select(fileInfo =>
                    Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();

                        var task = ctx.AddTask($"[yellow]{fileInfo.Name}[/]");
                        task.IsIndeterminate();

                        try
                        {
                            task.Description($"Opening {fileInfo.Name}");
                            task.Value = 0;

                            await using var edr = await ExcelDataReader.CreateAsync(fileInfo.FullName);
                            task.IsIndeterminate(false);
                            
                            do
                            {
                                task.MaxValue(edr.RowCount);
                                task.Value = 0;

                                task.Description($"Processing [yellow]{edr.WorksheetName ?? "Unknown Sheet"}[/] in [green]{fileInfo.Name}[/]");
                                if (string.IsNullOrEmpty(edr.WorksheetName))
                                    continue;

                                await Task.Delay(1000);
                                await edr.ReadAsync();
                                edr.Initialize();

                                if (edr.WorksheetName.Equals("NotQ100", StringComparison.OrdinalIgnoreCase))
                                {
                                    var data = edr.GetRecords<NotQ100Data>(new DataBinderOptions
                                    {
                                        BindingMode = DataBindingMode.AllProperties
                                    });

                                    foreach (var variable in data)
                                    {
                                        task.Increment(1);

                                        if (string.IsNullOrEmpty(variable.Period) ||
                                            string.IsNullOrEmpty(variable.QuestionNumber))
                                            continue;

                                        NotQ100Data.Add(variable);
                                    }
                                }else if (edr.WorksheetName.Equals("Q100", StringComparison.OrdinalIgnoreCase))
                                {
                                    var data = edr.GetRecords<Q100Data>(new DataBinderOptions
                                    {
                                        BindingMode = DataBindingMode.AllProperties
                                    });
                                    
                                    foreach (var variable in data)
                                    {
                                        task.Increment(1);

                                        if (string.IsNullOrEmpty(variable.Period))
                                            continue;
                                        
                                        Q100Data.Add(variable);
                                    }
                                    
                                }
                            } while (await edr.NextResultAsync());

                            task.Description($"Processed {fileInfo.Name}");
                            task.StopTask();
                        }
                        catch (Exception ex)
                        {
                            task.StopTask();
                            logger.LogError(ex, "Error processing file {FileInfoFullName}", fileInfo.FullName);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    })).ToList();

                await Task.WhenAll(tasks);
            });
        
        logger.LogInformation("Finished processing all files...");
        var table = new Table();
        table.AddColumns("Data type", "Count");
        table.AddRow("NotQ100Data", NotQ100Data.Count.ToString());
        table.AddRow("Q100Data", Q100Data.Count.ToString());
        logger.LogRenderable(LogLevel.Information, table);
    }

    
    
}
