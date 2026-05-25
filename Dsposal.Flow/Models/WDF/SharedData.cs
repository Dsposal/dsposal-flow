namespace Dsposal.Flow.Models.WDF;

public class SharedData
{
    public string Authority { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    
    public string? MaterialGroup { get; set; }

    private string _updatedPeriod => Period.Replace("Sep", "Sept");

    // Period will be a string "Jan 20 - Dec 20". So, we need to parse it to get the start and end dates.
    public DateOnly PeriodStart =>
        DateOnly.FromDateTime(DateTime.ParseExact(_updatedPeriod.Split('-')[0].Trim(), "MMM yy", null));

    public DateOnly PeriodEnd => DateOnly.FromDateTime(DateTime
        .ParseExact(_updatedPeriod.Split('-')[1].Trim(), "MMM yy", null)
        .AddMonths(1).AddDays(-1)); // Get the last day of the month
    
    public int Quarter => (int)Math.Ceiling(PeriodStart.Month / 3.0);
    
    public int Year => PeriodStart.Year;
}