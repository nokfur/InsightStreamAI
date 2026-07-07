using InsightStreamAI.Domain.Entities;
using InsightStreamAI.Application.Services;
using InsightStreamAI.Application.Repositories;
using InsightStreamAI.Infrastructure.TextSplitters;
using UglyToad.PdfPig;
using System.Text;

namespace InsightStreamAI.Infrastructure.Services;

public class DocumentService(IDocumentRepository documentRepository, IEmbeddingService embeddingService) : IDocumentService
{
    public async Task<List<Document>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        return await documentRepository.GetAllWithChunksAsync(cancellationToken);
    }

    public async Task<Document> IndexDocumentAsync(string title, byte[] fileBytes, string fileExtension, CancellationToken cancellationToken = default)
    {
        // 1. Create document entity
        var document = new Document
        {
            Title = title,
            Path = $"local://{title}",
            FileSize = fileBytes.Length,
            CreatedAt = DateTime.UtcNow
        };

        var isPdf = fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        int chunkSequenceIndex = 0;

        if (isPdf)
        {
            // Parse PDF page-by-page using UglyToad.PdfPig
            using (var pdf = PdfDocument.Open(fileBytes))
            {
                foreach (var page in pdf.GetPages())
                {
                    var pageText = page.Text;
                    if (string.IsNullOrWhiteSpace(pageText))
                    {
                        continue;
                    }

                    var textChunks = TextChunker.SplitText(pageText, maxChunkSize: 800, overlapSize: 100);
                    foreach (var chunkText in textChunks)
                    {
                        var vector = await embeddingService.GenerateEmbeddingAsync(chunkText, cancellationToken);
                        var embeddingBytes = ConvertFloatsToBytes(vector);

                        var chunk = new DocumentChunk
                        {
                            DocumentId = document.Id,
                            Document = document,
                            Index = chunkSequenceIndex++,
                            Text = chunkText,
                            Embedding = embeddingBytes,
                            PageNumber = page.Number
                        };

                        document.Chunks.Add(chunk);
                    }
                }
            }
        }
        else
        {
            // Parse plain text file
            var content = Encoding.UTF8.GetString(fileBytes);
            var textChunks = TextChunker.SplitText(content, maxChunkSize: 800, overlapSize: 100);

            foreach (var chunkText in textChunks)
            {
                var vector = await embeddingService.GenerateEmbeddingAsync(chunkText, cancellationToken);
                var embeddingBytes = ConvertFloatsToBytes(vector);

                var chunk = new DocumentChunk
                {
                    DocumentId = document.Id,
                    Document = document,
                    Index = chunkSequenceIndex++,
                    Text = chunkText,
                    Embedding = embeddingBytes,
                    PageNumber = null // Plain text has no pages
                };

                document.Chunks.Add(chunk);
            }
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
