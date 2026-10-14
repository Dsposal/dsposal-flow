using Dsposal.Flow.Extensions;

namespace DsposalForgeTests.Extensions;

public class ExtensionTests
{
    [Test]
    public void CleanupPostcode_NormalizesValidPostcode()
    {
        Assert.That("SW1A 1AA".CleanupPostcode(), Is.EqualTo("SW1A 1AA"));
        Assert.That("SW1A1AA".CleanupPostcode(), Is.EqualTo("SW1A 1AA"));
        Assert.That("SW1A1A".CleanupPostcode(), Is.EqualTo("SW1 A1A"));
    }

    [Test]
    public void CleanupPostcode_NullSentinelsReturnNull()
    {
        Assert.That("N/A".CleanupPostcode(), Is.Null);
        Assert.That("not provided".CleanupPostcode(), Is.Null);
    }

    [Test]
    public void CleanupPostcode_InvalidLengthReturnsOriginal()
    {
        Assert.That("AB1".CleanupPostcode(), Is.EqualTo("AB1"));
        Assert.That("AB123CDE".CleanupPostcode(), Is.EqualTo("AB123CDE"));
    }

    [Test]
    public void CleanupPostcode_NullAndNonAlphanumericReturnOriginal()
    {
        string? nullPostcode = null;
        Assert.That(nullPostcode.CleanupPostcode(), Is.Null);
        Assert.That("SW1A-1AA".CleanupPostcode(), Is.EqualTo("SW1A-1AA"));
    }

    [Test]
    public void ExtractPermits_FindsPermitCode()
    {
        var permits = "Site uses EPR/AB1234CD under registration".ExtractPermits().ToList();
        Assert.That(permits, Contains.Item("EPR/AB1234CD"));
    }

    [Test]
    public void ExtractRegistrations_FindsRegistrationCode()
    {
        var regs = "Operator holds WCR/R/1234567".ExtractRegistrations().ToList();
        Assert.That(regs, Contains.Item("WCR/R/1234567"));
    }

    [Test]
    public void GetData_DistinctsOrdersFiltersAndProjects()
    {
        var source = new[] { 3, 1, 2, 2, 4 };
        var result = source.GetData(
            selector: x => x * 10,
            keySelector: x => x,
            filter: x => x > 1,
            orderBySelector: x => x).ToList();

        Assert.That(result, Is.EqualTo(new[] { 20, 30, 40 }));
    }
}
