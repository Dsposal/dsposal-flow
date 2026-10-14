using System.Collections.Concurrent;
using Dsposal.Flow.Models.WDF;
using Dsposal.Flow.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DsposalForgeTests;

public class PureCsvTests
{
    [Test]
    public void Q100_Writer_RecordsExpectedContent()
    {
        var writer = new FakeCsvWriter();
        var csv = new CsvFileService("C:/output", NullLogger<CsvFileService>.Instance, writer);
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

    [Test]
    public void Normalized_Writer_RecordsPivotedQuestionAndQuestions()
    {
        var writer = new FakeCsvWriter();
        var csv = new CsvFileService("C:/output", NullLogger<CsvFileService>.Instance, writer);
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
    public void Pivoted_RowTextColumns_ArePivoted()
    {
        var writer = new FakeCsvWriter();
        var csv = new CsvFileService("C:/output", NullLogger<CsvFileService>.Instance, writer);
        var q = new ConcurrentBag<Q100Data> { TestDataFactory.CreateQ100() };
        var n = new ConcurrentBag<NotQ100Data>
        {
            new NotQ100Data
            {
                Authority = "Adur District Council", Period = "Jan 25 - Mar 25",
                QuestionNumber = "Q001", RowText = "Population of Authority",
                Data = "64889", RowOrder = 1
            },
            new NotQ100Data
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
        var csv = new CsvFileService("C:/output", NullLogger<CsvFileService>.Instance, writer);
        var q = new ConcurrentBag<Q100Data> { TestDataFactory.CreateQ100() };
        var n = new ConcurrentBag<NotQ100Data>
        {
            new NotQ100Data
            {
                Authority = "Adur District Council", Period = "Jan 25 - Mar 25",
                QuestionNumber = "Q002", RowText = "Population of Authority",
                ColText = "Jan", Data = "10", RowOrder = 1, ColOrder = 1
            },
            new NotQ100Data
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

}
