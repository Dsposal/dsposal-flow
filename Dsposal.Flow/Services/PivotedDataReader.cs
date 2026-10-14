using System.Collections;
using System.Data;
using System.Data.Common;

namespace Dsposal.Flow.Services;

public sealed class PivotedDataReader : DbDataReader
{
    private readonly string[] columns;
    private readonly IList<IDictionary<string, object>> rows;
    private int ordinal = -1;

    public PivotedDataReader(IEnumerable<string> columns, IList<IDictionary<string, object>> rows)
    {
        this.columns = columns.ToArray();
        this.rows = rows;
    }

    public override int FieldCount => columns.Length;

    public override object this[int ordinal] => rows[ordinal][this.columns[ordinal]];

    public override object this[string name] => rows[ordinal][name];

    public override int Depth => 0;
    public override bool IsClosed => false;
    public override bool HasRows => rows.Count > 0;
    public override int RecordsAffected => rows.Count;

    public override bool Read()
    {
        ordinal++;
        return ordinal < rows.Count;
    }

    public override bool NextResult() => false;

    public override void Close() { }

    public override bool IsDBNull(int ordinal) =>
        !rows[this.ordinal].TryGetValue(columns[ordinal], out var val) || val is null or DBNull;

    public override string GetString(int ordinal) =>
        rows[this.ordinal].TryGetValue(columns[ordinal], out var val) ? val?.ToString() ?? string.Empty : string.Empty;

    public override object GetValue(int ordinal) =>
        rows[this.ordinal].TryGetValue(columns[ordinal], out var val) ? val ?? DBNull.Value : DBNull.Value;

    public override int GetValues(object[] values)
    {
        var row = rows[this.ordinal];
        var count = Math.Min(columns.Length, values.Length);
        for (var i = 0; i < count; i++)
        {
            values[i] = row.TryGetValue(columns[i], out var val) ? val ?? DBNull.Value : DBNull.Value;
        }
        return count;
    }

    public override string GetName(int ordinal) => columns[ordinal];

    public override int GetOrdinal(string name) => Array.IndexOf(columns, name);

    public override Type GetFieldType(int ordinal) => typeof(object);

    public override string GetDataTypeName(int ordinal) => "System.Object";

    public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
    public override byte GetByte(int ordinal) => throw new NotSupportedException();
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
    public override char GetChar(int ordinal) => throw new NotSupportedException();
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
    public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
    public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();
    public override double GetDouble(int ordinal) => throw new NotSupportedException();
    public override float GetFloat(int ordinal) => throw new NotSupportedException();
    public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
    public override short GetInt16(int ordinal) => throw new NotSupportedException();
    public override int GetInt32(int ordinal) => throw new NotSupportedException();
    public override long GetInt64(int ordinal) => throw new NotSupportedException();

    public override DataTable? GetSchemaTable() => null;

    public override IEnumerator GetEnumerator() => rows.Select(r => r.Values).GetEnumerator();
}
