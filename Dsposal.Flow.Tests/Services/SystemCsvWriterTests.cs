using System.Data;
using Dsposal.Flow.Services;
using Sylvan.Data.Csv;

namespace DsposalForgeTests.Services;

public class SystemCsvWriterTests
{
    private sealed class Row
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    [Test]
    public async Task WriteAsync_Generic_CreatesDirectoryAndWritesRows()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dsposal-csv-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "nested", "out.csv");
        try
        {
            var writer = new SystemCsvWriter();
            await writer.WriteAsync(path, new[] { new Row { Name = "x", Value = 1 } });

            Assert.That(File.Exists(path), Is.True);
            var text = await File.ReadAllTextAsync(path);
            Assert.That(text, Does.Contain("Name"));
            Assert.That(text, Does.Contain("x"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Test]
    public async Task WriteAsync_Generic_WithOptions_WritesRows()
    {
        var path = Path.Combine(Path.GetTempPath(), "dsposal-csv-" + Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            var writer = new SystemCsvWriter();
            await writer.WriteAsync(path, new[] { new Row { Name = "y", Value = 2 } },
                new CsvDataWriterOptions { MaxBufferSize = int.MaxValue });

            Assert.That(File.Exists(path), Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task WriteAsync_Reader_WritesRows()
    {
        var path = Path.Combine(Path.GetTempPath(), "dsposal-csv-" + Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            using var table = new DataTable();
            table.Columns.Add("A", typeof(string));
            table.Rows.Add("hello");

            var writer = new SystemCsvWriter();
            await writer.WriteAsync(path, table.CreateDataReader());

            var text = await File.ReadAllTextAsync(path);
            Assert.That(text, Does.Contain("hello"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task WriteAsync_PathWithoutDirectory_DoesNotThrow()
    {
        var fileName = "dsposal-csv-" + Guid.NewGuid().ToString("N") + ".csv";
        try
        {
            var writer = new SystemCsvWriter();
            await writer.WriteAsync(fileName, new[] { new Row { Name = "z" } });
            Assert.That(File.Exists(fileName), Is.True);
        }
        finally
        {
            File.Delete(fileName);
        }
    }
}
