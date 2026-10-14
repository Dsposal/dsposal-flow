using System.Data.Common;
using Sylvan.Data;
using Sylvan.Data.Csv;

namespace Dsposal.Flow.Services;

public sealed class SystemCsvWriter : ICsvWriter
{
    public async Task WriteAsync<T>(string path, IEnumerable<T> data, CsvDataWriterOptions? options = null) where T : class
    {
        EnsureDirectory(path);
        await using var writer = options is null
            ? CsvDataWriter.Create(path)
            : CsvDataWriter.Create(path, options);
        await writer.WriteAsync(data.AsDataReader());
    }

    public async Task WriteAsync(string path, DbDataReader reader)
    {
        EnsureDirectory(path);
        await using var writer = CsvDataWriter.Create(path);
        await writer.WriteAsync(reader);
    }

    private static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }
}
