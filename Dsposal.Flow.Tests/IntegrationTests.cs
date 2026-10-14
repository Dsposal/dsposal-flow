using System.IO;
using Dsposal.Flow.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DsposalForgeTests;

public class IntegrationTests
{
    [Test]
    public async Task DataProcessor_ProcessesGeneratedWorkbook()
    {
        var path = TestWorkbookFactory.CreateTempWorkbook();
        try
        {
            var processor = new DataProcessor(NullLogger<DataProcessor>.Instance);
            await processor.ProcessFilesAsync(new List<FileInfo> { new(path) });

            Assert.That(processor.NotQ100Data.Count, Is.EqualTo(1));
            Assert.That(processor.Q100Data.Count, Is.EqualTo(1));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
