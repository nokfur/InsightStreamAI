using InsightStreamAI.Application.Services;
using Microsoft.SemanticKernel.Embeddings;

namespace InsightStreamAI.Infrastructure.Services;

#pragma warning disable CS0618
public class EmbeddingService(ITextEmbeddingGenerationService embeddingService) : IEmbeddingService
{
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embedding = await embeddingService.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return embedding.ToArray();
    }
}
