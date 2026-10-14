using System.IO;
using Dsposal.Flow.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DsposalForgeTests;

public class CsvOutputTests
{
    [Test]
    public async Task CsvFileService_WritesExpectedFiles()
    {
        var input = TestWorkbookFactory.CreateTempWorkbook();
        var output = Path.Combine(Path.GetTempPath(), $"dsposal-out-{Guid.NewGuid():N}");
        Directory.CreateDirectory(output);

        try
        {
            var processor = new DataProcessor(NullLogger<DataProcessor>.Instance);
            await processor.ProcessFilesAsync(new List<FileInfo> { new(input) });

            var csv = new CsvFileService(output, NullLogger<CsvFileService>.Instance);
            await csv.WriteNormalizedFiles(processor.NotQ100Data, processor.Q100Data);
            await csv.WriteQ100(processor.Q100Data);

            var expected = new[]
            {
                "Q100.csv",
                "NormalizedTables/Authorities.csv",
                "NormalizedTables/AuthoritiesWithoutId.csv",
                "NormalizedTables/Collate.csv",
                "NormalizedTables/FacilityTypes.csv",
                "NormalizedTables/MaterialGroups.csv",
                "NormalizedTables/Materials.csv",
                "NormalizedTables/NationalFacilities.csv",
                "NormalizedTables/OutputProcessTypes.csv",
                "NormalizedTables/Periods.csv",
                "NormalizedTables/Questions.csv",
                "NormalizedTables/Usages.csv",
                "NormalizedTables/WasteStreamTypes.csv",
            };

            foreach (var relativePath in expected)
            {
                var fullPath = Path.Combine(output, relativePath.Replace('/', Path.DirectorySeparatorChar));
                Assert.That(File.Exists(fullPath), Is.True, $"{relativePath} was not created");
                Assert.That(new FileInfo(fullPath).Length, Is.GreaterThan(0), $"{relativePath} is empty");
            }
        }
        finally
        {
            File.Delete(input);
            Directory.Delete(output, true);
        }
    }
}
