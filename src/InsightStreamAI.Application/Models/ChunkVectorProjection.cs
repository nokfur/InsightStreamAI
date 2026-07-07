namespace InsightStreamAI.Application.Models;

public class ChunkVectorProjection
{
    public Guid Id { get; set; }
    public required string DocumentTitle { get; set; }
    public required byte[] Embedding { get; set; }
}
