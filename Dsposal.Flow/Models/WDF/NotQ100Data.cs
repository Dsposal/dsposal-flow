using System.Runtime.Serialization;

namespace Dsposal.Flow.Models.WDF;

public class NotQ100Data : SharedData
{
    public string QuestionNumber { get; set; } = string.Empty;
    [DataMember(Name = "QuText")] public string QuestionText { get; set; } = string.Empty;
    public string? CollateText { get; set; }
    public string? RowText { get; set; }
    public string? ColText { get; set; }
    public string Data { get; set; } = string.Empty;

    public long RowOrder { get; set; }
    public long ColOrder { get; set; }
    public long RowIdent { get; set; }
    public long ColIdent { get; set; }
    
    [DataMember(Name = "CollateID")]
    public long CollateId { get; set; }
}