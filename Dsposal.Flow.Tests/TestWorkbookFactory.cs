using ClosedXML.Excel;

namespace DsposalForgeTests;

public static class TestWorkbookFactory
{
    private static readonly string[] NotQ100Headers =
    [
        "Authority", "Period", "QuestionNumber", "QuText", "CollateText", "RowText",
        "ColText", "Data", "RowOrder", "ColOrder", "RowIdent", "ColIdent",
        "CollateID", "columngroup", "MaterialGroup"
    ];

    private static readonly string[] Q100Headers =
    [
        "WasteProcessorId", "WasteStreamId", "WasteProcessorOutputId",
        "SenderWasteProcessorOutputId", "Authority", "AuthorityId", "Period", "PeriodId",
        "WasteStreamTypeId", "WasteStreamType", "FacilityTypeId", "FacilityType",
        "NationalFacilityId", "FacilityName", "FacilityAddress", "FacilityPostCode",
        "FacilityLicence", "FacilityCode", "OutputProcessTypeId", "OutputProcessType",
        "TotalTonnes", "MaterialId", "Material", "TonnesByMaterial",
        "TonnesFromHHSources", "TonnesFromCommercialSources",
        "TonnesFromIndustrialSources", "TonnesFromNonHHSources",
        "TonnesFromWfHSources", "TonnesFromWnfHSources", "UsageId", "Usage",
        "QuarterlyComments", "MonthlyComments", "MaterialGroup"
    ];

    public static string CreateTempWorkbook()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dsposal-test-{Guid.NewGuid():N}.xlsx");

        using var workbook = new XLWorkbook();

        var notQ = workbook.Worksheets.Add("NotQ100");
        notQ.Cell(1, 1).Value = "note row";
        notQ.Cell(2, 1).InsertData(new[] { NotQ100Headers });
        notQ.Cell(3, 1).Value = "Adur District Council";
        notQ.Cell(3, 2).Value = "Jan 25 - Mar 25";
        notQ.Cell(3, 3).Value = "Q001";
        notQ.Cell(3, 8).Value = "64889";

        var q = workbook.Worksheets.Add("Q100");
        q.Cell(1, 1).Value = "note row";
        q.Cell(2, 1).InsertData(new[] { Q100Headers });
        q.Cell(3, 1).Value = 6265112;
        q.Cell(3, 5).Value = "Luton Borough Council";
        q.Cell(3, 6).Value = 1;
        q.Cell(3, 7).Value = "Jan 25 - Mar 25";
        q.Cell(3, 9).Value = 1;
        q.Cell(3, 10).Value = "Source segregated recyclate";
        q.Cell(3, 11).Value = 16;
        q.Cell(3, 12).Value = "Reprocessor - recycling (qu19)";
        q.Cell(3, 13).Value = 3232;
        q.Cell(3, 14).Value = "U R M ( U K ) Limited ";
        q.Cell(3, 15).Value = "49 Lidgate Crescent";
        q.Cell(3, 16).Value = "WF9 3NR";
        q.Cell(3, 21).Value = 519;
        q.Cell(3, 23).Value = "Mixed glass";
        q.Cell(3, 24).Value = 519;

        workbook.SaveAs(path);
        return path;
    }
}
