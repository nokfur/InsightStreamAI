using InsightStreamAI.Domain.Entities;
using InsightStreamAI.Application.Models;
using InsightStreamAI.Application.Repositories;
using InsightStreamAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightStreamAI.Infrastructure.Data.Repositories;

public class DocumentRepository(AppDbContext dbContext) : IDocumentRepository
{
    public async Task<List<Document>> GetAllWithChunksAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .Include(d => d.Chunks)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents.CountAsync(cancellationToken);
    }

    public async Task<int> GetChunksCountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.DocumentChunks.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents.FindAsync(new object[] { id }, cancellationToken);
        if (document != null)
        {
            dbContext.Documents.Remove(document);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<DocumentChunk>> GetAllChunksAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.DocumentChunks
            .Include(c => c.Document)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChunkVectorProjection>> GetChunkVectorsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.DocumentChunks
            .Select(c => new ChunkVectorProjection
            {
                Id = c.Id,
                DocumentTitle = c.Document.Title,
                Embedding = c.Embedding
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DocumentChunk>> GetChunksByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await dbContext.DocumentChunks
            .Include(c => c.Document)
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }
}
