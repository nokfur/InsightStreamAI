using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using InsightStreamAI.Domain.Entities;
using InsightStreamAI.Application.Services;
using InsightStreamAI.Application.Common;
using System.Runtime.CompilerServices;

namespace InsightStreamAI.Infrastructure.Services;

public class SemanticKernelService(Kernel kernel) : ISemanticKernelService
{
    public async IAsyncEnumerable<string> GetStreamingResponseAsync(
        List<ChatMessage> conversationHistory, 
        string userPrompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(Constants.Prompts.SystemInstruction);

        // Add history
        foreach (var msg in conversationHistory)
        {
            if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                chatHistory.AddUserMessage(msg.Content);
            }
            else
            {
                chatHistory.AddAssistantMessage(msg.Content);
            }
        }

        // Add user prompt
        chatHistory.AddUserMessage(userPrompt);

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        IAsyncEnumerable<StreamingChatMessageContent>? stream = null;
        string? errorMessage = null;
        try
        {
            stream = chatService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error initiating chat stream: {ex.Message}";
        }

        if (errorMessage != null)
        {
            yield return errorMessage;
            yield break;
        }

        if (stream != null)
        {
            await foreach (var chunk in stream.WithCancellation(cancellationToken))
            {
                if (chunk.Content != null)
                {
                    yield return chunk.Content;
                }
            }
        }
    }
}
