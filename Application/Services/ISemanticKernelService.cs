using InsightStreamAI.Domain.Entities;

namespace InsightStreamAI.Application.Services;

public interface ISemanticKernelService
{
    IAsyncEnumerable<string> GetStreamingResponseAsync(
        List<ChatMessage> conversationHistory, 
        string userPrompt, 
        CancellationToken cancellationToken = default);
}
