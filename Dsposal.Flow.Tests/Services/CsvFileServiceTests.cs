using System.Collections.Concurrent;
using Dsposal.Flow.Models.WDF;
using Dsposal.Flow.Services;
using DsposalForgeTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace DsposalForgeTests.Services;

public class CsvFileServiceTests
{
    private static CsvFileService CreateService(ICsvWriter writer) =>
        new("C:/output", NullLogger<CsvFileService>.Instance, writer);

    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dsposal-pivot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static Q100Data Q100(long id, string facilityName) => new()
    {
        WasteProcessorId = id,
        WasteStreamId = id,
        WasteProcessorOutputId = id,
        SenderWasteProcessorOutputId = 0,
        Authority = "Luton Borough Council",
        AuthorityId = 1,
        Period = "Jan 25 - Mar 25",
        FacilityName = facilityName,
        NationalFacilityId = id,
        FacilityAddress = "Somewhere",
        FacilityPostCode = "WF9 3NR",
        FacilityLicence = "EPR/AB1234CD",
        FacilityCode = "WCR/R/1234567",
        TotalTonnes = 10,
    };

    private static NotQ100Data CreateNotQ100WithRowText() => new()
    {
        Authority = "Adur District Council",
        Period = "Jan 25 - Mar 25",
        QuestionNumber = "Q001",
        QuestionText = "Authority Data",
        RowText = "Population of Authority",
        Data = "64889",
        RowOrder = 1,
    };

    // ── Q100 output ────────────────────────────────────────────────

    [Test]
    public void Q100_Writer_RecordsExpectedContent()
    {
        var writer = new FakeCsvWriter();
        var csv = CreateService(writer);
        var data = new ConcurrentBag<Q100Data> { TestDataFactory.CreateQ100() };

        csv.WriteQ100(data).GetAwaiter().GetResult();

        var path = Path.Combine("C:/output", "Q100.csv");
        Assert.That(writer.Files.ContainsKey(path), Is.True);
        var row = writer.Files[path].Single();
        Assert.That(row["Period"], Is.EqualTo("Jan 25 - Mar 25"));
        Assert.That(row["AuthorityId"], Is.EqualTo(1));
        Assert.That(row["TotalTonnes"], Is.EqualTo(519));
        Assert.That(row["TonnesByMaterial"], Is.EqualTo(519));
    }

    [Test]
    public void WriteQ100_HandlesExemptAndOutsideUkFacilities()
    {
        var writer = new FakeCsvWriter();
        var service = CreateService(writer);
        var data = new ConcurrentBag<Q100Data>
        {
            Q100(1, "Normal facility"),
            Q100(2, "Exempt facility"),
            Q100(3, "Outside UK facility"),
            Q100(4, "Outside UK nonEU facility"),
        };

        service.WriteQ100(data).GetAwaiter().GetResult();

        var rows = writer.Files[Path.Combine("C:/output", "Q100.csv")];
        Assert.That(rows, Has.Count.EqualTo(4));

        var outsideUk = rows.Single(r => Convert.ToInt64(r["WasteProcessorId"]) == 3);
        Assert.That(outsideUk["FacilityAddress"], Is.EqualTo("Facility Outside The UK But Within the EU"));

        var outsideEu = rows.Single(r => Convert.ToInt64(r["WasteProcessorId"]) == 4);
        Assert.That(outsideEu["FacilityAddress"], Is.EqualTo("Facility Outside The UK And Not Within the EU"));
    }

    [Test]
    public void WriteQ100_HandlesNullFacilityFieldsAndZeroAuthorityId()
    {
        var writer = new FakeCsvWriter();
        var service = CreateService(writer);
        var data = new ConcurrentBag<Q100Data>
        {
            new()
            {
                WasteProcessorId = 1, WasteStreamId = 1, WasteProcessorOutputId = 1,
                SenderWasteProcessorOutputId = 0,
                Authority = string.Empty,
                AuthorityId = 0,
                Period = "Jan 25 - Mar 25",
                FacilityName = null!,
                FacilityLicence = null,
                FacilityCode = null,
                NationalFacilityId = 1,
                TotalTonnes = 5,
            },
        };

        service.WriteQ100(data).GetAwaiter().GetResult();
        service.WriteNormalizedFiles(new ConcurrentBag<NotQ100Data>(), data).GetAwaiter().GetResult();

        Assert.That(writer.Files[Path.Combine("C:/output", "Q100.csv")], Has.Count.EqualTo(1));
        Assert.That(data.Single().GetPermitsAndRegistrations().Item1, Is.Empty);
        Assert.That(data.Single().GetPermitsAndRegistrations().Item2, Is.Empty);
    }

    [Test]
    public void Constructor_WithoutWriter_DefaultsToSystemCsvWriter()
    {
        var dir = NewTempDir();
        try
        {
            var service = new CsvFileService(dir, NullLogger<CsvFileService>.Instance);

            service.WriteQ100(new ConcurrentBag<Q100Data> { Q100(1, "Normal facility") }).GetAwaiter().GetResult();

            Assert.That(File.Exists(Path.Combine(dir, "Q100.csv")), Is.True);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // ── Normalized + pivoted output ────────────────────────────────

    [Test]
    public void Normalized_Writer_RecordsPivotedQuestionAndQuestions()
    {
        var writer = new FakeCsvWriter();
        var csv = CreateService(writer);
        var q = new ConcurrentBag<Q100Data> { TestDataFactory.CreateQ100() };
        var n = new ConcurrentBag<NotQ100Data> { CreateNotQ100WithRowText() };

        csv.WriteNormalizedFiles(n, q).GetAwaiter().GetResult();

        Assert.That(writer.Files.Keys, Does.Contain(Path.Combine("C:/output", "NormalizedTables", "Questions.csv")));
        Assert.That(writer.Files.Keys, Does.Contain(Path.Combine("C:/output", "Q001.csv")));
        Assert.That(writer.Files.Keys, Does.Contain(Path.Combine("C:/output", "NormalizedTables", "Periods.csv")));

        var questions = writer.Files[Path.Combine("C:/output", "NormalizedTables", "Questions.csv")];
        Assert.That(questions, Has.Count.EqualTo(1));
        Assert.That(questions[0]["QuestionNumber"], Is.EqualTo("Q001"));

        var pivoted = writer.Files[Path.Combine("C:/output", "Q001.csv")].Single();
        Assert.That(pivoted["Period"], Is.EqualTo("Jan 25 - Mar 25"));
        Assert.That(pivoted["Authority"], Is.EqualTo("Adur District Council"));
        Assert.That(pivoted["AuthorityId"], Is.EqualTo(0));
    }

    [Test]
    public void WriteNormalizedFiles_CoversOutsideUkBranchesAndOrphanAuthorities()
    {
        var writer = new FakeCsvWriter();
        var service = CreateService(writer);
        var q100 = new ConcurrentBag<Q100Data>
        {
            Q100(1, "Outside UK facility"),
            Q100(2, "Outside UK nonEU facility"),
        };
        var notQ100 = new ConcurrentBag<NotQ100Data>
        {
            new()
            {
                Authority = "Orphan Council", Period = "Jan 25 - Mar 25",
                QuestionNumber = "Q001", RowText = "Population", Data = "1", RowOrder = 1
            },
        };

        service.WriteNormalizedFiles(notQ100, q100).GetAwaiter().GetResult();

        var normalized = Path.Combine("C:/output", "NormalizedTables");
        Assert.That(writer.Files.Keys, Does.Contain(Path.Combine(normalized, "AuthoritiesWithoutId.csv")));
        Assert.That(writer.Files.Keys, Does.Contain(Path.Combine(normalized, "NationalFacilities.csv")));

        var national = writer.Files[Path.Combine(normalized, "NationalFacilities.csv")];
        Assert.That(national.Select(r => r["FacilityAddress"]),
            Does.Contain("Facility Outside The UK And Not Within the EU"));
    }

    [Test]
    public void Pivoted_RowTextColumns_ArePivoted()
    {
        var writer = new FakeCsvWriter();
        var csv = CreateService(writer);
        var q = new ConcurrentBag<Q100Data> { TestDataFactory.CreateQ100() };
        var n = new ConcurrentBag<NotQ100Data>
        {
            new()
            {
                Authority = "Adur District Council", Period = "Jan 25 - Mar 25",
                QuestionNumber = "Q001", RowText = "Population of Authority",
                Data = "64889", RowOrder = 1
            },
            new()
            {
                Authority = "Adur District Council", Period = "Jan 25 - Mar 25",
                QuestionNumber = "Q001", RowText = "Households",
                Data = "1000", RowOrder = 2
            },
        };

        csv.WriteNormalizedFiles(n, q).GetAwaiter().GetResult();

        var pivoted = writer.Files[Path.Combine("C:/output", "Q001.csv")].Single();
        Assert.That(pivoted.Keys, Does.Contain("Population of Authority"));
        Assert.That(pivoted.Keys, Does.Contain("Households"));
        Assert.That(pivoted["Population of Authority"], Is.EqualTo("64889"));
        Assert.That(pivoted["Households"], Is.EqualTo("1000"));
    }

    [Test]
    public void Pivoted_ColTextColumns_ArePivoted()
    {
        var writer = new FakeCsvWriter();
        var csv = CreateService(writer);
        var q = new ConcurrentBag<Q100Data> { TestDataFactory.CreateQ100() };
        var n = new ConcurrentBag<NotQ100Data>
        {
            new()
            {
                Authority = "Adur District Council", Period = "Jan 25 - Mar 25",
                QuestionNumber = "Q002", RowText = "Population of Authority",
                ColText = "Jan", Data = "10", RowOrder = 1, ColOrder = 1
            },
            new()
            {
                Authority = "Adur District Council", Period = "Jan 25 - Mar 25",
                QuestionNumber = "Q002", RowText = "Population of Authority",
                ColText = "Feb", Data = "20", RowOrder = 1, ColOrder = 2
            },
        };

        csv.WriteNormalizedFiles(n, q).GetAwaiter().GetResult();

        var pivoted = writer.Files[Path.Combine("C:/output", "Q002.csv")].Single();
        Assert.That(pivoted.Keys, Does.Contain("Jan"));
        Assert.That(pivoted.Keys, Does.Contain("Feb"));
        Assert.That(pivoted["Jan"], Is.EqualTo("10"));
        Assert.That(pivoted["Feb"], Is.EqualTo("20"));
    }

    [Test]
    public void WritePivoted_MultipleValuesInSameCell_AreJoined()
    {
        var writer = new FakeCsvWriter();
        var service = CreateService(writer);
        var notQ100 = new ConcurrentBag<NotQ100Data>
        {
            new()
            {
                Authority = "Adur District Council", Period = "Jan 25 - Mar 25", QuestionNumber = "Q001",
                RowText = "Population of Authority", Data = "1", RowOrder = 1
            },
            new()
            {
                Authority = "Adur District Council", Period = "Jan 25 - Mar 25", QuestionNumber = "Q001",
                RowText = "Population of Authority", Data = "2", RowOrder = 1
            },
        };

        service.WriteNormalizedFiles(notQ100, new ConcurrentBag<Q100Data>()).GetAwaiter().GetResult();

        var pivoted = writer.Files[Path.Combine("C:/output", "Q001.csv")].Single();
        Assert.That(pivoted["Population of Authority"], Is.AnyOf("1, 2", "2, 1"));
    }

    [Test]
    public void WritePivoted_HandlesNullRowTextAndData()
    {
        var writer = new FakeCsvWriter();
        var service = CreateService(writer);
        var notQ100 = new ConcurrentBag<NotQ100Data>
        {
            new() { Authority = "A", Period = "Jan 25 - Mar 25", QuestionNumber = "Q001", RowText = null, Data = "1", RowOrder = 1 },
            new() { Authority = "A", Period = "Jan 25 - Mar 25", QuestionNumber = "Q001", RowText = "Households", Data = null!, RowOrder = 2 },
        };

        service.WriteNormalizedFiles(notQ100, new ConcurrentBag<Q100Data>()).GetAwaiter().GetResult();

        var pivoted = writer.Files[Path.Combine("C:/output", "Q001.csv")].Single();
        Assert.That(pivoted["Households"], Is.EqualTo(""));
    }

    [Test]
    public void WritePivoted_ColPathHandlesNullRowText()
    {
        var writer = new FakeCsvWriter();
        var service = CreateService(writer);
        var notQ100 = new ConcurrentBag<NotQ100Data>
        {
            new() { Authority = "A", Period = "Jan 25 - Mar 25", QuestionNumber = "Q001", RowText = null, ColText = "Jan", Data = "1", RowOrder = 1, ColOrder = 1 },
            new() { Authority = "A", Period = "Jan 25 - Mar 25", QuestionNumber = "Q001", RowText = null, ColText = "Feb", Data = null!, RowOrder = 1, ColOrder = 2 },
        };

        service.WriteNormalizedFiles(notQ100, new ConcurrentBag<Q100Data>()).GetAwaiter().GetResult();

        var pivoted = writer.Files[Path.Combine("C:/output", "Q001.csv")].Single();
        Assert.That(pivoted["Feb"], Is.EqualTo(""));
    }
}
