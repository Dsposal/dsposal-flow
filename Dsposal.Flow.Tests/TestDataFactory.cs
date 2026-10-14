using Dsposal.Flow.Models.WDF;

namespace DsposalForgeTests;

public static class TestDataFactory
{
    public static Q100Data CreateQ100() => new()
    {
        WasteProcessorId = 6265112,
        WasteStreamId = 6263691,
        Authority = "Luton Borough Council",
        AuthorityId = 1,
        Period = "Jan 25 - Mar 25",
        WasteStreamTypeId = 1,
        WasteStreamType = "Source segregated recyclate",
        FacilityTypeId = 16,
        FacilityType = "Reprocessor - recycling (qu19)",
        NationalFacilityId = 3232,
        FacilityName = "U R M ( U K ) Limited ",
        FacilityAddress = "49 Lidgate Crescent",
        FacilityPostCode = "WF9 3NR",
        FacilityLicence = "61833 / FP3198ZN",
        TotalTonnes = 519,
        MaterialId = 6,
        Material = "Mixed glass",
        TonnesByMaterial = 519,
    };

    public static NotQ100Data CreateNotQ100(string? questionNumber = "Q001", string data = "64889") => new()
    {
        Authority = "Adur District Council",
        Period = "Jan 25 - Mar 25",
        QuestionNumber = questionNumber ?? "Q001",
        QuestionText = "Authority Data",
        Data = data,
    };
}
