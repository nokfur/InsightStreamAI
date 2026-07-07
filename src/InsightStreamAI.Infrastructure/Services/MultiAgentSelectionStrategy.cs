using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using InsightStreamAI.Application.Common;
using System.Linq;

namespace InsightStreamAI.Infrastructure.Services;

/// <summary>
/// Custom speaker selection strategy that handles routing control between agents.
/// It intercepts handoff tokens returned by tool executions during the current turn.
/// </summary>
public class MultiAgentSelectionStrategy : SelectionStrategy
{
    protected override Task<Agent> SelectAgentAsync(
        IReadOnlyList<Agent> agents, 
        IReadOnlyList<ChatMessageContent> history, 
        CancellationToken cancellationToken = default)
    {
        // Find the index of the user's latest prompt to isolate the current turn's messaging history.
        int lastUserIndex = history.Count - 1;
        while (lastUserIndex >= 0 && history[lastUserIndex].Role != AuthorRole.User)
        {
            lastUserIndex--;
        }

        int startIndex = Math.Max(0, lastUserIndex);
        
        // Extract all text chunks generated in the current execution turn.
        // This projects the message body and the string representations of all internal content items (such as function outputs)
        // because the handoff command will be populated inside a FunctionResultContent item.
        var texts = history.Skip(startIndex)
            .SelectMany(msg => msg.Items
                .Select(i => i.ToString())
                .Prepend(msg.Content))
            .Where(t => !string.IsNullOrEmpty(t));

        var handoffDocToken = $"{Constants.Prompts.HandoffPrefix} {Constants.Prompts.DocumentResearchAgentName}";
        var handoffWebToken = $"{Constants.Prompts.HandoffPrefix} {Constants.Prompts.WorkspaceAutomationAgentName}";
        var handoffRouterToken = $"{Constants.Prompts.HandoffPrefix} {Constants.Prompts.RouterAgentName}";

        // Iterate through all texts from the current turn to check if a handoff token was emitted.
        foreach (var text in texts)
        {
            if (text!.Contains(handoffDocToken))
            {
                return Task.FromResult(agents.First(a => a.Name == Constants.Prompts.DocumentResearchAgentName));
            }
            if (text.Contains(handoffWebToken))
            {
                return Task.FromResult(agents.First(a => a.Name == Constants.Prompts.WorkspaceAutomationAgentName));
            }
            if (text.Contains(handoffRouterToken))
            {
                return Task.FromResult(agents.First(a => a.Name == Constants.Prompts.RouterAgentName));
            }
        }

        // Default back to the RouterAgent if no handoff command was found in the current turn.
        return Task.FromResult(agents.First(a => a.Name == Constants.Prompts.RouterAgentName));
    }
}
