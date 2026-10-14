using System.Data.Common;
using System.Reflection;
using Dsposal.Flow.Services;
using Sylvan.Data.Csv;

namespace DsposalForgeTests.Helpers;

public sealed class FakeCsvWriter : ICsvWriter
{
    private readonly Dictionary<string, List<Dictionary<string, object?>>> _files =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, List<Dictionary<string, object?>>> Files => _files;

    public Task WriteAsync<T>(string path, IEnumerable<T> data, CsvDataWriterOptions? options = null) where T : class
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var rows = data.Select(item => properties.ToDictionary(
            p => p.Name,
            p => p.GetValue(item))).ToList();
        _files[path] = rows;
        return Task.CompletedTask;
    }

    public Task WriteAsync(string path, DbDataReader reader)
    {
        var rows = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.GetValue(i);
            rows.Add(row);
        }
        _files[path] = rows;
        return Task.CompletedTask;
    }
}
