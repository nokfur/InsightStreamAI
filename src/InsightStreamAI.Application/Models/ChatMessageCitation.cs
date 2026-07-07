namespace InsightStreamAI.Application.Models;

public class ChatMessageCitation
{
    public required string DocumentTitle { get; set; }
    public Guid DocumentId { get; set; }
    public float Score { get; set; }
    public required string TextExcerpt { get; set; }
    public int? PageNumber { get; set; }
}
