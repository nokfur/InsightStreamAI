using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using InsightStreamAI.Application.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InsightStreamAI.Infrastructure.Services;

/// <summary>
/// A chat completion service wrapper/decorator that filters the chat history passed to specialist agents.
/// Local LLMs get confused by consecutive assistant messages or intermediate handoff tokens.
/// Filtering the history for specialist agents ensures they only see system instructions, user prompts,
/// and their own execution context.
/// </summary>
public class HistoryFilteringChatCompletionService(IChatCompletionService inner) : IChatCompletionService
{
    private readonly IChatCompletionService _inner = inner;

    public IReadOnlyDictionary<string, object?> Attributes => _inner.Attributes;

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory, 
        PromptExecutionSettings? executionSettings = null, 
        Kernel? kernel = null, 
        CancellationToken cancellationToken = default)
    {
        var filteredHistory = FilterHistory(chatHistory);
        return _inner.GetChatMessageContentsAsync(filteredHistory, executionSettings, kernel, cancellationToken);
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory, 
        PromptExecutionSettings? executionSettings = null, 
        Kernel? kernel = null, 
        CancellationToken cancellationToken = default)
    {
        var filteredHistory = FilterHistory(chatHistory);
        return _inner.GetStreamingChatMessageContentsAsync(filteredHistory, executionSettings, kernel, cancellationToken);
    }

    private ChatHistory FilterHistory(ChatHistory chatHistory)
    {
        // Find system message to identify the agent
        string? systemContent = null;
        foreach (var msg in chatHistory)
        {
            if (msg.Role == AuthorRole.System)
            {
                systemContent = msg.Content;
                break;
            }
        }

        // If it's a specialist agent (not Router), filter the history to prevent local LLM confusion
        if (systemContent != null && !systemContent.Contains("Router Agent"))
        {
            string? specialistName = null;
            if (systemContent.Contains("Document Research Agent"))
            {
                specialistName = Constants.Prompts.DocumentResearchAgentName;
            }
            else if (systemContent.Contains("Workspace Automation Agent"))
            {
                specialistName = Constants.Prompts.WorkspaceAutomationAgentName;
            }

            if (specialistName != null)
            {
                var filtered = new ChatHistory();
                foreach (var msg in chatHistory)
                {
                    if (msg.Role == AuthorRole.System || 
                        msg.Role == AuthorRole.User || 
                        msg.AuthorName == specialistName)
                    {
                        filtered.Add(msg);
                    }
                }
                return filtered;
            }
        }

        return chatHistory;
    }
}
