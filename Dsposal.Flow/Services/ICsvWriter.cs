using System.Data.Common;
using Sylvan.Data.Csv;

namespace Dsposal.Flow.Services;

public interface ICsvWriter
{
    Task WriteAsync<T>(string path, IEnumerable<T> data, CsvDataWriterOptions? options = null) where T : class;
    Task WriteAsync(string path, DbDataReader reader);
}
