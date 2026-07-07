using InsightStreamAI.Domain.Entities;

namespace InsightStreamAI.Application.Services;

public interface IDocumentService
{
    Task<List<Document>> GetDocumentsAsync(CancellationToken cancellationToken = default);
    Task<Document> IndexDocumentAsync(string title, string content, long fileSize, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
}
