using System.Data;
using Dsposal.Flow.Extensions;

namespace DsposalForgeTests.Extensions;

public class DatatableExtensionsTests
{
    [Test]
    public void ToSpectreTable_MapsColumnsAndRows()
    {
        using var table = new DataTable();
        table.Columns.Add("Name");
        table.Columns.Add("Value");
        table.Rows.Add("a", 1);
        table.Rows.Add("b", null);

        var spectreTable = DatatableExtensions.ToSpectreTable(table);

        Assert.That(spectreTable.Columns, Has.Count.EqualTo(2));
        Assert.That(spectreTable.Rows, Has.Count.EqualTo(2));
    }
}
