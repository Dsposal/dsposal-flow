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
    
    private string NormalizedOutput => Path.Combine(outputPath, "NormalizedTables");

    public CsvFileService(string outputPath, ILogger<CsvFileService> logger)
    {
        this.outputPath = outputPath;
        this.logger = logger;
        
        if(!Directory.Exists(NormalizedOutput))
        {
            Directory.CreateDirectory(NormalizedOutput);
        }
    }
    
    public async Task WriteNormalizedFiles(ConcurrentBag<NotQ100Data> notQ100Data, ConcurrentBag<Q100Data> q100Data)
    {
        await Task.WhenAll([
            // Write the files for q100 data that should be in it's own tables
            WriteNormalizedFiles(q100Data),
            // Write the files for the not q100 data that should be in it's own tables
            WriteNormalizedFiles(notQ100Data),
            
            // Other data from both sets that should be a separate table
            WritePeriodFile(q100Data, notQ100Data),
            WriteAuthorities(notQ100Data, q100Data),
        ]);
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

        await using CsvDataWriter cdw = CsvDataWriter.Create(Path.Combine(NormalizedOutput, "Periods.csv"));
        await cdw.WriteAsync(periodsUnique.AsDataReader());

        LogCsvFileCreated("Periods", NormalizedOutput);
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
                    FacilityPostCode = !exempt ? "" : x.FacilityPostCode,

                    FacilityLicence = !exempt
                        ? ""
                        : singleLicense,
                    FacilityLicences = !exempt ? "" : string.Join(", ", permits),
                    FacilityLicenceRaw = !exempt ? "" : x.FacilityLicence,

                    FacilityRegistration = !exempt
                        ? ""
                        : singleRegistration,
                    FacilityRegistrations = !exempt ? "" : string.Join(", ", registrations),
                    FacilityRegistrationRaw = !exempt ? "" : x.FacilityCode,
                };
            },
            x => (x.WasteProcessorId, x.WasteStreamId, x.WasteProcessorOutputId, x.SenderWasteProcessorOutputId)
            
        );
        
        await using CsvDataWriter cdw = CsvDataWriter.Create(Path.Combine(outputPath, "Q100.csv"));
        await cdw.WriteAsync(what.AsDataReader());
        
    }

    private async Task WriteNormalizedFiles(ConcurrentBag<NotQ100Data> data)
    {
        await Task.WhenAll([WriteQuestions(data)]);
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
    
    private async Task WriteFacilityTypeFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x => new { x.FacilityTypeId, x.FacilityType },
            x => x.FacilityTypeId);

        await using CsvDataWriter cdw = CsvDataWriter.Create(Path.Combine(NormalizedOutput, "FacilityTypes.csv"));
        await cdw.WriteAsync(whatToWrite.AsDataReader());

        LogCsvFileCreated("Facility types", NormalizedOutput);
    }

    private async Task WriteWasteStreamTypeFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x => new { x.WasteStreamTypeId, x.WasteStreamType },
            x => x.WasteStreamTypeId);

        await using CsvDataWriter cdw = CsvDataWriter.Create(Path.Combine(NormalizedOutput, "WasteStreamTypes.csv"));
        await cdw.WriteAsync(whatToWrite.AsDataReader());

        LogCsvFileCreated("Waste stream types", NormalizedOutput);
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
                    FacilityPostCode = x.SkipFacilityAddressNormalization ? "" : x.FacilityPostCode,

                    FacilityLicence = x.SkipFacilityAddressNormalization
                        ? ""
                        : singleLicense,
                    FacilityLicences = x.SkipFacilityAddressNormalization ? "" : string.Join(", ", permits),
                    FacilityLicenceRaw = x.SkipFacilityAddressNormalization ? "" : x.FacilityLicence,

                    FacilityRegistration = x.SkipFacilityAddressNormalization
                        ? ""
                        : singleRegistration,
                    FacilityRegistrations = x.SkipFacilityAddressNormalization ? "" : string.Join(", ", registrations),
                    FacilityRegistrationRaw = x.SkipFacilityAddressNormalization ? "" : x.FacilityCode,
                };
            },
            x => x.NationalFacilityId);

        await using CsvDataWriter cdw = CsvDataWriter.Create(Path.Combine(NormalizedOutput, "NationalFacilities.csv"));
        await cdw.WriteAsync(whatToWrite.AsDataReader());

        LogCsvFileCreated("National facility names", NormalizedOutput);
    }

    private async Task WriteOutputProcessTypeFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x => new { x.OutputProcessTypeId, x.OutputProcessType },
            x => x.OutputProcessTypeId,
            x => x.OutputProcessType != null);

        await using CsvDataWriter cdw = CsvDataWriter.Create(Path.Combine(NormalizedOutput, "OutputProcessTypes.csv"));
        await cdw.WriteAsync(whatToWrite.AsDataReader());

        LogCsvFileCreated("Output process types", NormalizedOutput);
    }

    private async Task WriteMaterialFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x => new { x.MaterialId, x.Material },
            x => x.MaterialId);

        await using CsvDataWriter cdw = CsvDataWriter.Create(Path.Combine(NormalizedOutput, "Materials.csv"));
        await cdw.WriteAsync(whatToWrite.AsDataReader());

        LogCsvFileCreated("Materials", NormalizedOutput);
    }

    private async Task WriteUsageFile(ConcurrentBag<Q100Data> data)
    {
        var whatToWrite = data.GetData(
            x => new { x.UsageId, x.Usage },
            x => x.UsageId);

        await using CsvDataWriter cdw = CsvDataWriter.Create(Path.Combine(NormalizedOutput, "Usages.csv"));
        await cdw.WriteAsync(whatToWrite.AsDataReader());

        LogCsvFileCreated("Usages", NormalizedOutput);
    }

    private async Task WriteAuthorities(ConcurrentBag<NotQ100Data> data, ConcurrentBag<Q100Data> q100Data)
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
            await using CsvDataWriter cdw2 = CsvDataWriter.Create(Path.Combine(NormalizedOutput, "AuthoritiesWithoutId.csv"));
            await cdw2.WriteAsync(authoritiesWithoutAnId.AsDataReader());
            LogCsvFileCreated("AuthoritiesWithoutId", NormalizedOutput);
        }

        await using CsvDataWriter cdw = CsvDataWriter.Create(Path.Combine(NormalizedOutput, "Authorities.csv"));
        await cdw.WriteAsync(authorizesWithIds.AsDataReader());

        LogCsvFileCreated("Authorities", NormalizedOutput);
    }

    private async Task WriteQuestions(ConcurrentBag<NotQ100Data> data)
    {
        // Again, question number is the PK but it's a string
        var what = data.GetData(x => new
        {
            x.QuestionNumber,
            x.QuestionText,
        }, x => x.QuestionNumber);

        await using CsvDataWriter cdw = CsvDataWriter.Create(Path.Combine(NormalizedOutput, "Questions.csv"));
        await cdw.WriteAsync(what.AsDataReader());

        LogCsvFileCreated("Questions", NormalizedOutput);
    }

    [LoggerMessage(LogLevel.Information, "{Type} CSV file written to {OutputPath}")]
    partial void LogCsvFileCreated(string type, string outputPath);
}