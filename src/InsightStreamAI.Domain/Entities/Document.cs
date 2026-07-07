namespace InsightStreamAI.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Title { get; set; }
    public required string Path { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
