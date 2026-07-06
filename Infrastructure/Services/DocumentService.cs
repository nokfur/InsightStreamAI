using InsightStreamAI.Domain.Entities;
using InsightStreamAI.Application.Services;
using InsightStreamAI.Application.Repositories;
using InsightStreamAI.Infrastructure.TextSplitters;

namespace InsightStreamAI.Infrastructure.Services;

public class DocumentService(IDocumentRepository documentRepository, IEmbeddingService embeddingService) : IDocumentService
{
    public async Task<List<Document>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        return await documentRepository.GetAllWithChunksAsync(cancellationToken);
    }

    public async Task<Document> IndexDocumentAsync(string title, string content, long fileSize, CancellationToken cancellationToken = default)
    {
        // 1. Create document entity
        var document = new Document
        {
            Title = title,
            Path = $"local://{title}",
            FileSize = fileSize,
            CreatedAt = DateTime.UtcNow
        };

        // 2. Chunk text
        var textChunks = TextChunker.SplitText(content, maxChunkSize: 800, overlapSize: 100);

        // 3. Generate embeddings and create chunk entities
        for (int i = 0; i < textChunks.Count; i++)
        {
            var chunkText = textChunks[i];
            
            // Generate embedding using the embedding service
            var vector = await embeddingService.GenerateEmbeddingAsync(chunkText, cancellationToken);
            
            // Convert to byte array
            var embeddingBytes = ConvertFloatsToBytes(vector);

            var chunk = new DocumentChunk
            {
                DocumentId = document.Id,
                Document = document,
                Index = i,
                Text = chunkText,
                Embedding = embeddingBytes
            };

            document.Chunks.Add(chunk);
        }

        // 4. Save via repository
        await documentRepository.AddAsync(document, cancellationToken);

        return document;
    }

    public async Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await documentRepository.DeleteAsync(id, cancellationToken);
    }

    private static byte[] ConvertFloatsToBytes(float[] floats)
    {
        if (floats == null)
        {
            return Array.Empty<byte>();
        }
        byte[] bytes = new byte[floats.Length * 4];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
