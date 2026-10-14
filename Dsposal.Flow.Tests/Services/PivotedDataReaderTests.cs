using System.Collections;
using Dsposal.Flow.Services;

namespace DsposalForgeTests.Services;

public class PivotedDataReaderTests
{
    private static PivotedDataReader CreateReader() => new(
        new[] { "A", "B" },
        new List<IDictionary<string, object>>
        {
            new Dictionary<string, object> { ["A"] = "a1", ["B"] = 1 },
            new Dictionary<string, object> { ["A"] = "a2", ["B"] = DBNull.Value },
        });

    [Test]
    public void Metadata_ReportsExpectedValues()
    {
        var reader = CreateReader();

        Assert.That(reader.FieldCount, Is.EqualTo(2));
        Assert.That(reader.Depth, Is.EqualTo(0));
        Assert.That(reader.IsClosed, Is.False);
        Assert.That(reader.HasRows, Is.True);
        Assert.That(reader.RecordsAffected, Is.EqualTo(2));
        Assert.That(reader.GetFieldType(0), Is.EqualTo(typeof(object)));
        Assert.That(reader.GetDataTypeName(0), Is.EqualTo("System.Object"));
        Assert.That(reader.GetName(1), Is.EqualTo("B"));
        Assert.That(reader.GetOrdinal("A"), Is.EqualTo(0));
        Assert.That(reader.GetOrdinal("missing"), Is.EqualTo(-1));
        Assert.That(reader.GetSchemaTable(), Is.Null);
        Assert.That(reader.NextResult(), Is.False);

        reader.Close();
    }

    [Test]
    public void EmptyRows_HasNoRowsAndReadReturnsFalse()
    {
        var reader = new PivotedDataReader(new[] { "A" }, new List<IDictionary<string, object>>());
        Assert.That(reader.HasRows, Is.False);
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_AdvancesAndIndexersReturnValues()
    {
        var reader = CreateReader();

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader[0], Is.EqualTo("a1"));
        Assert.That(reader["B"], Is.EqualTo(1));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Accessors_HandleMissingAndNullValues()
    {
        var reader = new PivotedDataReader(
            new[] { "A", "B" },
            new List<IDictionary<string, object>>
            {
                new Dictionary<string, object> { ["A"] = "a1" },              // B missing
                new Dictionary<string, object> { ["A"] = null!, ["B"] = DBNull.Value },
            });

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.IsDBNull(0), Is.False);
        Assert.That(reader.IsDBNull(1), Is.True);
        Assert.That(reader.GetString(0), Is.EqualTo("a1"));
        Assert.That(reader.GetString(1), Is.EqualTo(string.Empty));
        Assert.That(reader.GetValue(0), Is.EqualTo("a1"));
        Assert.That(reader.GetValue(1), Is.EqualTo(DBNull.Value));

        var values = new object[2];
        Assert.That(reader.GetValues(values), Is.EqualTo(2));
        Assert.That(values[0], Is.EqualTo("a1"));
        Assert.That(values[1], Is.EqualTo(DBNull.Value));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.IsDBNull(0), Is.True);
        Assert.That(reader.GetString(0), Is.EqualTo(string.Empty));
        Assert.That(reader.GetValue(0), Is.EqualTo(DBNull.Value));

        // Shorter buffer than columns exercises the Math.Min branch.
        Assert.That(reader.GetValues(new object[1]), Is.EqualTo(1));
    }

    [Test]
    public void GetEnumerator_YieldsOneEntryPerRow()
    {
        var count = 0;
        var enumerator = ((IEnumerable)CreateReader()).GetEnumerator();
        while (enumerator.MoveNext()) count++;
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void UnsupportedAccessors_Throw()
    {
        var reader = CreateReader();
        reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(() => reader.GetBoolean(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetByte(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetBytes(0, 0, null!, 0, 0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetChar(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetChars(0, 0, null!, 0, 0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetDateTime(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetDecimal(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetDouble(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetFloat(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetGuid(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetInt16(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetInt32(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => reader.GetInt64(0), Throws.TypeOf<NotSupportedException>());
        });
    }
}
