using System.Data;
using Spectre.Console;

namespace Dsposal.Flow.Extensions;

public static class DatatableExtensions
{
    internal static Table ToSpectreTable(DataTable dataTable)
    {
        var table = new Table();

        foreach (DataColumn column in dataTable.Columns)
        {
            table.AddColumn(column.ColumnName);
        }

        foreach (DataRow row in dataTable.Rows)
        {
            // Convert row items to strings for Spectre
            var rowItems = row.ItemArray.Select(x => x?.ToString() ?? "").ToArray();
            table.AddRow(rowItems);
        }

        return table;
    }
}