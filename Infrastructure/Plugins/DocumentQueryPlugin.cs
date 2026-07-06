using System.ComponentModel;
using Microsoft.SemanticKernel;
using InsightStreamAI.Application.Repositories;
using InsightStreamAI.Application.Services;

namespace InsightStreamAI.Infrastructure.Plugins;

public class DocumentQueryPlugin(IDocumentRepository documentRepository, IEmbeddingService embeddingService)
{
    [KernelFunction, Description("Queries the database for sections of uploaded documents that match a user's question.")]
    public async Task<string> QueryDocumentsAsync(
        [Description("The question or search query for the document contents")] string query, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "No search query provided.";
        }

        try
        {
            // 1. Generate embedding for query
            var queryVector = await embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

            // 2. Fetch lightweight projected chunk vectors
            var projectedChunks = await documentRepository.GetChunkVectorsAsync(cancellationToken);

            if (projectedChunks.Count == 0)
            {
                return "No indexed documents found in the database. Please upload and index documents first.";
            }

            // 3. Compute cosine similarity in memory
            var scoredProjections = new List<(Guid Id, string DocumentTitle, float Score)>();

            foreach (var chunk in projectedChunks)
            {
                var chunkVector = ConvertBytesToFloats(chunk.Embedding);
                if (chunkVector.Length == 0 || chunkVector.Length != queryVector.Length)
                {
                    continue; // Mismatched embedding dimensions
                }

                float score = CosineSimilarity(queryVector, chunkVector);
                scoredProjections.Add((chunk.Id, chunk.DocumentTitle, score));
            }

            // 4. Sort and filter top results
            var topScored = scoredProjections
                .Where(c => c.Score >= 0.35f) // Reasonably low threshold for semantic match
                .OrderByDescending(c => c.Score)
                .Take(4)
                .ToList();

            if (topScored.Count == 0)
            {
                return "No relevant sections found in the documents matching the query.";
            }

            // 5. Fetch full content (including text) only for the top matched IDs
            var matchedIds = topScored.Select(x => x.Id).ToList();
            var fullChunks = await documentRepository.GetChunksByIdsAsync(matchedIds, cancellationToken);

            // Map scores back to the fetched full chunks
            var scoredChunks = fullChunks
                .Select(chunk =>
                {
                    var score = topScored.First(ts => ts.Id == chunk.Id).Score;
                    return new { Chunk = chunk, Score = score };
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            // 6. Format results for the LLM context
            var formattedResults = scoredChunks.Select(r => 
                $"[Source Document: {r.Chunk.Document.Title}] (Relevance Score: {r.Score:P0})\n{r.Chunk.Text}");

            return "Relevant document sections found:\n\n" + string.Join("\n\n---\n\n", formattedResults);
        }
        catch (Exception ex)
        {
            return $"Error querying document content: {ex.Message}";
        }
    }

    private static float[] ConvertBytesToFloats(byte[] bytes)
    {
        if (bytes == null || bytes.Length % 4 != 0)
        {
            return Array.Empty<float>();
        }
        float[] floats = new float[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    private static float CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        float dotProduct = 0.0f;
        float normA = 0.0f;
        float normB = 0.0f;
        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }
        if (normA == 0.0f || normB == 0.0f) return 0.0f;
        return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }
}
