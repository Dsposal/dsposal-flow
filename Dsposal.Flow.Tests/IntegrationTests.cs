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

            var notQ = processor.NotQ100Data.Single();
            Assert.That(notQ.Authority, Is.EqualTo("Adur District Council"));
            Assert.That(notQ.Period, Is.EqualTo("Jan 25 - Mar 25"));
            Assert.That(notQ.QuestionNumber, Is.EqualTo("Q001"));
            Assert.That(notQ.Data, Is.EqualTo("64889"));

            var q = processor.Q100Data.Single();
            Assert.That(q.Authority, Is.EqualTo("Luton Borough Council"));
            Assert.That(q.Period, Is.EqualTo("Jan 25 - Mar 25"));
            Assert.That(q.FacilityName, Is.EqualTo("U R M ( U K ) Limited "));
            Assert.That(q.TotalTonnes, Is.EqualTo(519));
            Assert.That(q.Material, Is.EqualTo("Mixed glass"));
            Assert.That(q.TonnesByMaterial, Is.EqualTo(519));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task DataProcessor_FiltersBlankRows()
    {
        var path = TestWorkbookFactory.CreateTempWorkbookWithFilteredRows();
        try
        {
            var processor = new DataProcessor(NullLogger<DataProcessor>.Instance);
            await processor.ProcessFilesAsync(new List<FileInfo> { new(path) });

            Assert.That(processor.NotQ100Data.Count, Is.EqualTo(1));
            Assert.That(processor.Q100Data.Count, Is.EqualTo(1));

            var notQ = processor.NotQ100Data.Single();
            Assert.That(notQ.Authority, Is.EqualTo("Adur District Council"));
            Assert.That(notQ.QuestionNumber, Is.EqualTo("Q001"));

            var q = processor.Q100Data.Single();
            Assert.That(q.Authority, Is.EqualTo("Luton Borough Council"));
        }
        finally
        {
            File.Delete(path);
        }
    }
    [Test]
    public async Task DataProcessor_ProcessesNotQ100OnlyWorkbook()
    {
        var path = TestWorkbookFactory.CreateNotQ100OnlyWorkbook();
        try
        {
            var processor = new DataProcessor(NullLogger<DataProcessor>.Instance);
            await processor.ProcessFilesAsync(new List<FileInfo> { new(path) });

            Assert.That(processor.NotQ100Data.Count, Is.EqualTo(1));
            Assert.That(processor.Q100Data.Count, Is.EqualTo(0));
        }
        finally { File.Delete(path); }
    }

    [Test]
    public async Task DataProcessor_ProcessesQ100OnlyWorkbook()
    {
        var path = TestWorkbookFactory.CreateQ100OnlyWorkbook();
        try
        {
            var processor = new DataProcessor(NullLogger<DataProcessor>.Instance);
            await processor.ProcessFilesAsync(new List<FileInfo> { new(path) });

            Assert.That(processor.NotQ100Data.Count, Is.EqualTo(0));
            Assert.That(processor.Q100Data.Count, Is.EqualTo(1));
        }
        finally { File.Delete(path); }
    }

    [Test]
    public async Task DataProcessor_ProcessesEmptyWorkbook()
    {
        var path = TestWorkbookFactory.CreateEmptyWorkbook();
        try
        {
            var processor = new DataProcessor(NullLogger<DataProcessor>.Instance);
            await processor.ProcessFilesAsync(new List<FileInfo> { new(path) });

            Assert.That(processor.NotQ100Data.Count, Is.EqualTo(0));
            Assert.That(processor.Q100Data.Count, Is.EqualTo(0));
        }
        finally { File.Delete(path); }
    }

}
