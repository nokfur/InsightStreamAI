using InsightStreamAI.Domain.Entities;

namespace InsightStreamAI.Application.Services;

public interface IDocumentService
{
    Task<List<Document>> GetDocumentsAsync(CancellationToken cancellationToken = default);
    Task<Document> IndexDocumentAsync(string title, byte[] fileBytes, string fileExtension, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
}
