using InsightStreamAI.Application.Models;

namespace InsightStreamAI.Application.Services;

public interface ICitationTracker
{
    void AddCitation(ChatMessageCitation citation);
    List<ChatMessageCitation> GetCitations();
    void Clear();
}
