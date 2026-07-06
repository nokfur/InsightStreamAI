using InsightStreamAI.Domain.Entities;
using InsightStreamAI.Application.Models;

namespace InsightStreamAI.Application.Repositories;

public interface IDocumentRepository
{
    Task<List<Document>> GetAllWithChunksAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetChunksCountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> GetAllChunksAsync(CancellationToken cancellationToken = default);
    Task<List<ChunkVectorProjection>> GetChunkVectorsAsync(CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> GetChunksByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
}
