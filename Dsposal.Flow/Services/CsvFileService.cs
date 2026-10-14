using System.Collections.Concurrent;
using Dsposal.Flow.Extensions;
using Dsposal.Flow.Models.WDF;
using Microsoft.Extensions.Logging;
using Sylvan.Data;
using Sylvan.Data.Csv;

namespace Dsposal.Flow.Services;

public partial class CsvFileService
{
    private readonly string outputPath;
    private readonly ILogger<CsvFileService> logger;
    private readonly ICsvWriter _writer;

    private string NormalizedOutput => Path.Combine(outputPath, "NormalizedTables");

    public CsvFileService(string outputPath, ILogger<CsvFileService> logger, ICsvWriter? csvWriter = null)
    {
        this.outputPath = outputPath;
        this.logger = logger;
        _writer = csvWriter ?? new SystemCsvWriter();
    }

    public async Task WriteNormalizedFiles(ConcurrentBag<NotQ100Data> notQ100Data, ConcurrentBag<Q100Data> q100Data)
    {
        var authorityLookup = q100Data
            .Where(x => x.AuthorityId > 0 && !string.IsNullOrEmpty(x.Authority))
            .DistinctBy(x => x.Authority)
            .ToDictionary(x => x.Authority, x => x.AuthorityId);

        Task[] tasks =
        [
            // Write the files for q100 data that should be in it's own tables
            WriteNormalizedFiles(q100Data),
            // Write the files for the not q100 data that should be in it's own tables
            WriteNormalizedFiles(notQ100Data, authorityLookup),
            // Write any shared data from both sets that should be a single table
            WriteNormalizedFiles(q100Data, notQ100Data),
        ];

        await Task.WhenAll(tasks);
    }

    private async Task WriteNormalizedFiles(ConcurrentBag<NotQ100Data> data, Dictionary<string, long> authorityLookup)
    {
        await Task.WhenAll([
            WriteQuestions(data),
            WriteCollate(data),
            WritePivotedNotQ100Data(data, authorityLookup),
        ]);
    }

    private async Task WriteCsvFile<T>(
        string filePath,
        string displayName,
        IEnumerable<T> data,
        CsvDataWriterOptions? options = null) where T : class
    {
        await _writer.WriteAsync(filePath, data, options);
        LogCsvFileCreated(displayName, Path.GetDirectoryName(filePath) ?? "");
    }

    private async Task WriteNormalizedFiles(ConcurrentBag<Q100Data> data)
    {
        Task[] tasks =
        [
            WriteFacilityTypeFile(data),
            WriteWasteStreamTypeFile(data),
            WriteNationalFacilityNameFile(data),
            WriteOutputProcessTypeFile(data),
            WriteMaterialFile(data),
            WriteUsageFile(data),
        ];

        await Task.WhenAll(tasks);
    }

    private async Task WriteNormalizedFiles(ConcurrentBag<Q100Data> q100, ConcurrentBag<NotQ100Data> notQ100)
    {
        Task[] tasks =
        [
            WritePeriodFile(q100, notQ100),
            WriteAuthorities(q100, notQ100),
            WriteMaterialGroups(q100, notQ100),
        ];

        await Task.WhenAll(tasks);
    }

    private async Task WriteMaterialGroups(ConcurrentBag<Q100Data> q100Data, ConcurrentBag<NotQ100Data> notQ100Data)
    {
        var materialGroups1 = q100Data.GetData(
            x => new { x.MaterialGroup },
            x => x.MaterialGroup);

        var materialGroups2 = notQ100Data.GetData(
            x => new { x.MaterialGroup },
            x => x.MaterialGroup);

        // Yay. Another field without a proper id, I guess just use the group name as PK in db. We already do it for questions...
        var materialGroups = materialGroups1
            .Union(materialGroups2)
            .Where(x => !string.IsNullOrEmpty(x.MaterialGroup))
            .DistinctBy(x => x.MaterialGroup);

        await WriteCsvFile(Path.Combine(NormalizedOutput, "MaterialGroups.csv"), "MaterialGroups", materialGroups);
    }

    private async Task WritePeriodFile(ConcurrentBag<Q100Data> data, ConcurrentBag<NotQ100Data> notQ100Data)
    {
        var periods1 = data.GetData(
            x => new
            {
                x.Period, x.PeriodStart, x.PeriodEnd, x.Quarter, x.Year
            },
            x => x.Period);
        var periods2 = notQ100Data.GetData(x => new
        {
            x.Period, x.PeriodStart, x.PeriodEnd, x.Quarter, x.Year
        }, x => x.Period);

        // Period should be the PK here (it's a string so some databases might not like that)
        var periodsUnique = periods1.Union(periods2).DistinctBy(x => x.Period);


        await WriteCsvFile(Path.Combine(NormalizedOutput, "Periods.csv"), "Periods", periodsUnique);
    }

    public async Task WriteQ100(ConcurrentBag<Q100Data> data)
    {
        var what = data.GetData(x =>
            {
                var exempt = x.SkipFacilityAddressNormalization;

                var permitsAndRegistrations = x.GetPermitsAndRegistrations();
                var permits = permitsAndRegistrations.Item1;
                var registrations = permitsAndRegistrations.Item2;

                var singleLicense = permits.Length > 0 ? permits[0] : "";
                var singleRegistration = registrations.Length > 0 ? registrations[0] : "";

                var facilityAddress = x.SkipFacilityAddressNormalization ? "" : x.FacilityAddress;

                if (x.IsOutsideUKFacility)
                {
                    facilityAddress = "Facility Outside The UK But Within the EU";
                    if (x.FacilityName.Contains("nonEU", StringComparison.OrdinalIgnoreCase))
                    {
                        facilityAddress = "Facility Outside The UK And Not Within the EU";
                    }
                }


                return new
                {
                    // PK
                    x.WasteProcessorId,
                    x.WasteStreamId,
                    x.WasteProcessorOutputId,
                    x.SenderWasteProcessorOutputId,

                    // data
                    x.Period,
                    x.AuthorityId,
                    x.WasteStreamTypeId,
                    x.FacilityTypeId,
                    x.NationalFacilityId,
                    x.OutputProcessTypeId,
                    x.TotalTonnes,
                    x.MaterialId,
                    x.TonnesByMaterial,
                    x.TonnesFromHHSources,
                    x.TonnesFromCommercialSources,
                    x.TonnesFromIndustrialSources,
                    x.TonnesFromNonHHSources,
                    x.TonnesFromWfHSources,
                    x.TonnesFromWnfHSources,
                    x.UsageId,
                    x.QuarterlyComments,
                    x.MonthlyComments,

                    // Ugh. Some facilities are exempt (have a national ID of 0) but we still want the facility details... So, I guess we gotta do this again :'(
                    FacilityAddress = !exempt ? "" : facilityAddress,
                    FacilityPostCode = !exempt ? "" : x.FacilityPostCode.CleanupPostcode(),

                    FacilityLicenceExtracted = !exempt
                        ? ""
                        : singleLicense,
                    FacilityLicenceRaw = !exempt ? "" : x.FacilityLicence,

                    FacilityRegistrationExtracted = !exempt
                        ? ""
                        : singleRegistration,
                    FacilityCodeRaw = !exempt ? "" : x.FacilityCode,
                };
            },
            x => (x.WasteProcessorId, x.WasteStreamId, x.WasteProcessorOutputId, x.SenderWasteProcessorOutputId)
        );

        await WriteCsvFile(Path.Combine(outputPath, "Q100.csv"), "Q100", what, new CsvDataWriterOptions()
        {
            MaxBufferSize = int.MaxValue,
        });
    }

    private async Task WriteFacilityTypeFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x => new { x.FacilityTypeId, x.FacilityType },
            x => x.FacilityTypeId);

        await WriteCsvFile(Path.Combine(NormalizedOutput, "FacilityTypes.csv"), "Facility types", whatToWrite);
    }

    private async Task WriteWasteStreamTypeFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x => new { x.WasteStreamTypeId, x.WasteStreamType },
            x => x.WasteStreamTypeId);

        await WriteCsvFile(Path.Combine(NormalizedOutput, "WasteStreamTypes.csv"), "Waste stream types", whatToWrite);
    }

    private async Task WriteNationalFacilityNameFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x =>
            {
                var permitsAndRegistrations = x.GetPermitsAndRegistrations();
                var permits = permitsAndRegistrations.Item1;
                var registrations = permitsAndRegistrations.Item2;

                var singleLicense = permits.Length > 0 ? permits[0] : "";
                var singleRegistration = registrations.Length > 0 ? registrations[0] : "";

                var facilityAddress = x.SkipFacilityAddressNormalization ? "" : x.FacilityAddress;

                if (x.IsOutsideUKFacility)
                {
                    facilityAddress = "Facility Outside The UK But Within the EU";
                    if (x.FacilityName.Contains("nonEU", StringComparison.OrdinalIgnoreCase))
                    {
                        facilityAddress = "Facility Outside The UK And Not Within the EU";
                    }
                }

                return new
                {
                    x.NationalFacilityId,
                    x.FacilityName,

                    FacilityAddress = facilityAddress,
                    FacilityPostCode = x.SkipFacilityAddressNormalization ? "" : x.FacilityPostCode.CleanupPostcode(),

                    FacilityLicenceExtracted = x.SkipFacilityAddressNormalization
                        ? ""
                        : singleLicense,
                    FacilityLicenceRaw = x.SkipFacilityAddressNormalization ? "" : x.FacilityLicence,

                    FacilityRegistrationExtracted = x.SkipFacilityAddressNormalization
                        ? ""
                        : singleRegistration,
                    FacilityCodeRaw = x.SkipFacilityAddressNormalization ? "" : x.FacilityCode,
                };
            },
            x => x.NationalFacilityId);

        await WriteCsvFile(Path.Combine(NormalizedOutput, "NationalFacilities.csv"), "National facility names",
            whatToWrite);
    }

    private async Task WriteOutputProcessTypeFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x => new { x.OutputProcessTypeId, x.OutputProcessType },
            x => x.OutputProcessTypeId,
            x => x.OutputProcessType != null);

        await WriteCsvFile(Path.Combine(NormalizedOutput, "OutputProcessTypes.csv"), "Output process types",
            whatToWrite);
    }

    private async Task WriteMaterialFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x => new { x.MaterialId, x.Material },
            x => x.MaterialId);
        
        await WriteCsvFile(Path.Combine(NormalizedOutput, "Materials.csv"), "Materials", whatToWrite);
    }

    private async Task WriteUsageFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x => new { x.UsageId, x.Usage },
            x => x.UsageId);
        
        await WriteCsvFile(Path.Combine(NormalizedOutput, "Usages.csv"), "Usages", whatToWrite);

    }

    private async Task WriteAuthorities(ConcurrentBag<Q100Data> q100Data, ConcurrentBag<NotQ100Data> data)
    {
        var authorizesWithIds =
            q100Data.GetData(x => new { x.AuthorityId, x.Authority }, x => x.AuthorityId)
                .ToList();

        var authoritiesFromNq100 = data.GetData(x => x.Authority, x => x.Authority)
            .Distinct();

        var authoritiesWithoutAnId = authoritiesFromNq100.Except(authorizesWithIds.Select(x => x.Authority))
            .Select(x => new { Authority = x })
            .ToList();

        // This should _neve_ happen but, I guess it's possible with the data quality i've seen.
        // So, in case this _does_ happen, just write them to a separate file and let a human deal with it.
        if (authoritiesWithoutAnId.Count > 0)
        {
            await WriteCsvFile(Path.Combine(NormalizedOutput, "AuthoritiesWithoutId.csv"), "AuthoritiesWithoutId", authoritiesWithoutAnId);
        }

        await WriteCsvFile(Path.Combine(NormalizedOutput, "Authorities.csv"), "Authorities", authorizesWithIds);
    }

    private async Task WriteQuestions(ConcurrentBag<NotQ100Data> data)
    {
        // Again, question number is the PK but it's a string
        var what = data.GetData(x => new
        {
            x.QuestionNumber,
            x.QuestionText,
        }, x => x.QuestionNumber);
        
        await WriteCsvFile(Path.Combine(NormalizedOutput, "Questions.csv"), "Questions", what);
    }

    private Task WriteCollate(ConcurrentBag<NotQ100Data> data)
    {
        var whatToWrite = data.GetData(x => new { x.CollateId, x.CollateText },
            x => x.CollateId);

        return WriteCsvFile(Path.Combine(NormalizedOutput, "Collate.csv"), "Collate", whatToWrite);
    }
    
    private async Task WritePivotedNotQ100Data(ConcurrentBag<NotQ100Data> data, Dictionary<string, long> authorityLookup)
    {
        var byQuestion = data
            .Where(x => !string.IsNullOrEmpty(x.QuestionNumber))
            .GroupBy(x => x.QuestionNumber);

        var tasks = byQuestion.Select(qg =>
            WritePivotedQuestionFile(qg.Key, qg.ToList(), authorityLookup));

        await Task.WhenAll(tasks);
    }

    private async Task WritePivotedQuestionFile(string questionNumber, List<NotQ100Data> data, Dictionary<string, long> authorityLookup)
    {
        var contextColumns = new[] { "Period", "Authority", "AuthorityId", "MaterialGroup", "QuestionNumber" };

        var colColumns = data
            .OrderBy(x => x.ColOrder)
            .Select(x => new { x.ColText, x.ColOrder })
            .DistinctBy(x => x.ColText)
            .ToList();

        var hasColText = colColumns.Any(c => !string.IsNullOrEmpty(c.ColText));

        if (!hasColText)
        {
            var rowTextColumns = data
                .OrderBy(x => x.RowOrder)
                .Select(x => new { x.RowText, x.RowOrder })
                .DistinctBy(x => x.RowText)
                .ToList();

            var groupedByContext = data
                .GroupBy(x => new { x.Period, x.Authority, x.MaterialGroup, x.QuestionNumber })
                .ToList();

            var pivotedRows = groupedByContext
                .Select(g =>
                {
                    var row = new Dictionary<string, object>
                    {
                        ["Period"] = g.Key.Period,
                        ["Authority"] = g.Key.Authority,
                        ["AuthorityId"] = authorityLookup.GetValueOrDefault(g.Key.Authority, 0),
                        ["MaterialGroup"] = g.Key.MaterialGroup ?? "",
                        ["QuestionNumber"] = g.Key.QuestionNumber
                    };

                    foreach (var rtCol in rowTextColumns)
                    {
                        var values = g.Where(x => x.RowText == rtCol.RowText)
                            .Select(x => x.Data)
                            .Where(d => d is not null)
                            .ToList();

                        row[rtCol.RowText ?? string.Empty] = values.Count switch
                        {
                            0 => "",
                            1 => values[0]!,
                            _ => string.Join(", ", values)
                        };
                    }

                    return (IDictionary<string, object>)row;
                })
                .ToList();

            var schema = contextColumns.Concat(rowTextColumns.Select(c => c.RowText ?? string.Empty));
            var reader = new PivotedDataReader(schema, pivotedRows);

            await _writer.WriteAsync(
                Path.Combine(outputPath, $"{questionNumber}.csv"), reader);

            LogCsvFileCreated($"NotQ100 {questionNumber}", NormalizedOutput);
            return;
        }

        var pivotedColRows = data
            .GroupBy(x => new
            {
                x.Period,
                x.Authority,
                x.MaterialGroup,
                x.QuestionNumber,
                RowText = x.RowText ?? string.Empty
            })
            .OrderBy(g => g.Min(x => x.RowOrder))
            .Select(g =>
            {
                var row = new Dictionary<string, object>
                {
                    ["Period"] = g.Key.Period,
                    ["Authority"] = g.Key.Authority,
                    ["AuthorityId"] = authorityLookup.GetValueOrDefault(g.Key.Authority, 0),
                    ["MaterialGroup"] = g.Key.MaterialGroup ?? "",
                    ["QuestionNumber"] = g.Key.QuestionNumber,
                    ["RowText"] = g.Key.RowText
                };

                foreach (var col in colColumns.Where(c => !string.IsNullOrEmpty(c.ColText)))
                {
                    var values = g.Where(x => x.ColText == col.ColText)
                        .Select(x => x.Data)
                        .Where(d => d is not null)
                        .ToList();

                    row[col.ColText!] = values.Count switch
                    {
                        0 => "",
                        1 => values[0]!,
                        _ => string.Join(", ", values)
                    };
                }

                return (IDictionary<string, object>)row;
            })
            .ToList();

        var colSchema = contextColumns.Append("RowText")
            .Concat(colColumns.Where(c => !string.IsNullOrEmpty(c.ColText)).Select(c => c.ColText!));
        var colReader = new PivotedDataReader(colSchema, pivotedColRows);

        await _writer.WriteAsync(
            Path.Combine(outputPath, $"{questionNumber}.csv"), colReader);

        LogCsvFileCreated($"NotQ100 {questionNumber}", NormalizedOutput);
    }

    internal async Task WritePivotedQuestionFileDataTable(string questionNumber, List<NotQ100Data> data, Dictionary<string, long> authorityLookup)
    {
        var contextColumns = new[] { "Period", "Authority", "AuthorityId", "MaterialGroup", "QuestionNumber" };

        var colColumns = data
            .OrderBy(x => x.ColOrder)
            .Select(x => new { x.ColText, x.ColOrder })
            .DistinctBy(x => x.ColText)
            .ToList();

        var hasColText = colColumns.Any(c => !string.IsNullOrEmpty(c.ColText));

        if (!hasColText)
        {
            var rowTextColumns = data
                .OrderBy(x => x.RowOrder)
                .Select(x => new { x.RowText, x.RowOrder })
                .DistinctBy(x => x.RowText)
                .ToList();

            var schemaColumnNames = contextColumns
                .Concat(rowTextColumns.Select(c => c.RowText ?? string.Empty))
                .ToList();

            var dt = new System.Data.DataTable();
            foreach (var name in schemaColumnNames)
                dt.Columns.Add(name);

            var groupedByContext = data
                .GroupBy(x => new { x.Period, x.Authority, x.MaterialGroup, x.QuestionNumber });

            foreach (var g in groupedByContext)
            {
                var row = dt.NewRow();
                row["Period"] = g.Key.Period;
                row["Authority"] = g.Key.Authority;
                row["AuthorityId"] = authorityLookup.GetValueOrDefault(g.Key.Authority, 0);
                row["MaterialGroup"] = g.Key.MaterialGroup ?? "";
                row["QuestionNumber"] = g.Key.QuestionNumber;

                foreach (var rtCol in rowTextColumns)
                {
                    var values = g.Where(x => x.RowText == rtCol.RowText)
                        .Select(x => x.Data)
                        .Where(d => d is not null)
                        .ToList();

                    row[rtCol.RowText ?? string.Empty] = values.Count switch
                    {
                        0 => "",
                        1 => values[0]!,
                        _ => string.Join(", ", values)
                    };
                }

                dt.Rows.Add(row);
            }

            await using var cdw = CsvDataWriter.Create(
                Path.Combine(outputPath, $"{questionNumber}.csv"));
            await cdw.WriteAsync(dt.CreateDataReader());

            LogCsvFileCreated($"NotQ100 {questionNumber}", NormalizedOutput);
            return;
        }

        var pivotedColSchema = contextColumns.Append("RowText")
            .Concat(colColumns.Where(c => !string.IsNullOrEmpty(c.ColText)).Select(c => c.ColText!))
            .ToList();

        var dt2 = new System.Data.DataTable();
        foreach (var name in pivotedColSchema)
            dt2.Columns.Add(name);

        var pivotedRows = data
            .GroupBy(x => new
            {
                x.Period,
                x.Authority,
                x.MaterialGroup,
                x.QuestionNumber,
                RowText = x.RowText ?? string.Empty
            })
            .OrderBy(g => g.Min(x => x.RowOrder));

        foreach (var g in pivotedRows)
        {
            var row = dt2.NewRow();
            row["Period"] = g.Key.Period;
            row["Authority"] = g.Key.Authority;
            row["AuthorityId"] = authorityLookup.GetValueOrDefault(g.Key.Authority, 0);
            row["MaterialGroup"] = g.Key.MaterialGroup ?? "";
            row["QuestionNumber"] = g.Key.QuestionNumber;
            row["RowText"] = g.Key.RowText;

            foreach (var col in colColumns.Where(c => !string.IsNullOrEmpty(c.ColText)))
            {
                var values = g.Where(x => x.ColText == col.ColText)
                    .Select(x => x.Data)
                    .Where(d => d is not null)
                    .ToList();

                row[col.ColText!] = values.Count switch
                {
                    0 => "",
                    1 => values[0]!,
                    _ => string.Join(", ", values)
                };
            }

            dt2.Rows.Add(row);
        }

        await using var cdw2 = CsvDataWriter.Create(
            Path.Combine(outputPath, $"{questionNumber}.csv"));
        await cdw2.WriteAsync(dt2.CreateDataReader());

        LogCsvFileCreated($"NotQ100 {questionNumber}", NormalizedOutput);
    }

    internal Task WriteRows(ConcurrentBag<NotQ100Data> data)
    {
        var whatToWrite = data.GetData(x => new { x.RowIdent, x.RowText },
            x => x.RowIdent);

        return WriteCsvFile(Path.Combine(NormalizedOutput, "Rows.csv"), "Rows", whatToWrite);
    }
    internal Task WriteCols(ConcurrentBag<NotQ100Data> data)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Information, "{Type} CSV file written to {OutputPath}")]
    partial void LogCsvFileCreated(string type, string outputPath);
}