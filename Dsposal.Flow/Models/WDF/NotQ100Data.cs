using System.Runtime.Serialization;

namespace Dsposal.Flow.Models.WDF;

public class NotQ100Data
{
    public string Authority { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string QuestionNumber { get; set; } = string.Empty;
    [DataMember(Name = "QuText")] public string QuestionText { get; set; } = string.Empty;
    public string? CollateText { get; set; }
    public string? RowText { get; set; }
    public string? ColText { get; set; }
    public string Data { get; set; } = string.Empty;
    public string? MaterialGroup { get; set; }

    public long RowOrder { get; set; }
    public long ColOrder { get; set; }
    public long RowIdent { get; set; }
    public long ColIdent { get; set; }

    private string _updatedPeriod => Period.Replace("Sep", "Sept");

    // Period will be a string "Jan 20 - Dec 20". So, we need to parse it to get the start and end dates.
    public DateOnly PeriodStart =>
        DateOnly.FromDateTime(DateTime.ParseExact(_updatedPeriod.Split('-')[0].Trim(), "MMM yy", null));

    public DateOnly PeriodEnd => DateOnly.FromDateTime(DateTime
        .ParseExact(_updatedPeriod.Split('-')[1].Trim(), "MMM yy", null)
        .AddMonths(1).AddDays(-1)); // Get the last day of the month
}