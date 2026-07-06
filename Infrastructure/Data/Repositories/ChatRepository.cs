using InsightStreamAI.Domain.Entities;
using InsightStreamAI.Application.Repositories;
using InsightStreamAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightStreamAI.Infrastructure.Data.Repositories;

public class ChatRepository(AppDbContext dbContext) : IChatRepository
{
    public async Task<List<ChatConversation>> GetAllConversationsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ChatConversations
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetConversationsCountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ChatConversations.CountAsync(cancellationToken);
    }

    public async Task<ChatConversation?> GetConversationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.ChatConversations.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<ChatMessage>> GetMessagesByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddConversationAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
    {
        dbContext.ChatConversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateConversationTitleAsync(Guid id, string title, CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.ChatConversations.FindAsync(new object[] { id }, cancellationToken);
        if (conversation != null)
        {
            conversation.Title = title;
            dbContext.Entry(conversation).State = EntityState.Modified;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteConversationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.ChatConversations.FindAsync(new object[] { id }, cancellationToken);
        if (conversation != null)
        {
            dbContext.ChatConversations.Remove(conversation);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
