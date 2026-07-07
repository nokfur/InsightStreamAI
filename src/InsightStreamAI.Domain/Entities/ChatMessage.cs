namespace InsightStreamAI.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    
    // Navigation property
    public ChatConversation Conversation { get; set; } = null!;
    
    public required string Role { get; set; } // "user" or "assistant"
    public required string Content { get; set; }
    public string? CitationsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
