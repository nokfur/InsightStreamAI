using InsightStreamAI.Domain.Entities;

namespace InsightStreamAI.Application.Repositories;

public interface IChatRepository
{
    Task<List<ChatConversation>> GetAllConversationsAsync(CancellationToken cancellationToken = default);
    Task<int> GetConversationsCountAsync(CancellationToken cancellationToken = default);
    Task<ChatConversation?> GetConversationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ChatMessage>> GetMessagesByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task AddConversationAsync(ChatConversation conversation, CancellationToken cancellationToken = default);
    Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task UpdateConversationTitleAsync(Guid id, string title, CancellationToken cancellationToken = default);
    Task DeleteConversationAsync(Guid id, CancellationToken cancellationToken = default);
}
