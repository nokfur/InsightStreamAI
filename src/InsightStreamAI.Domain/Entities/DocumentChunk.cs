namespace InsightStreamAI.Domain.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    
    // Navigation property
    public Document Document { get; set; } = null!;
    
    public int Index { get; set; }
    public required string Text { get; set; }
    public required byte[] Embedding { get; set; }
    public int? PageNumber { get; set; }
}
