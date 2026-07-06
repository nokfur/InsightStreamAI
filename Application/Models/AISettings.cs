namespace InsightStreamAI.Application.Models;

public class AISettings
{
    public required bool UseLocalServer { get; set; }
    public required string LocalEndpoint { get; set; }
    public required string ChatModelId { get; set; }
    public required string EmbeddingModelId { get; set; }
    public required string ApiKey { get; set; }
}
