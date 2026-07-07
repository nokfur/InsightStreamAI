using System.Collections.Concurrent;
using InsightStreamAI.Application.Models;
using InsightStreamAI.Application.Services;

namespace InsightStreamAI.Infrastructure.Services;

public class CitationTracker : ICitationTracker
{
    private readonly ConcurrentBag<ChatMessageCitation> _citations = new();

    public void AddCitation(ChatMessageCitation citation)
    {
        // Avoid duplicate citations by matching DocumentId and PageNumber
        bool exists = _citations.Any(c => 
            c.DocumentId == citation.DocumentId && 
            c.PageNumber == citation.PageNumber && 
            c.TextExcerpt == citation.TextExcerpt);

        if (!exists)
        {
            _citations.Add(citation);
        }
    }

    public List<ChatMessageCitation> GetCitations()
    {
        return _citations.OrderByDescending(c => c.Score).ToList();
    }

    public void Clear()
    {
        _citations.Clear();
    }
}
