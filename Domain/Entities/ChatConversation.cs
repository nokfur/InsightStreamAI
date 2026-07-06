namespace InsightStreamAI.Domain.Entities;

public class ChatConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
