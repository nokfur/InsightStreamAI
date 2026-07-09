using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using InsightStreamAI.Application.Common;
using System.Linq;

namespace InsightStreamAI.Infrastructure.Services;

/// <summary>
/// Custom termination strategy that concludes execution after a specialist answers,
/// or after the Router agent answers directly without handoff.
/// </summary>
public class MultiAgentTerminationStrategy : TerminationStrategy
{
    protected override Task<bool> ShouldAgentTerminateAsync(
        Agent agent, 
        IReadOnlyList<ChatMessageContent> history, 
        CancellationToken cancellationToken)
    {

        var lastMessage = history.LastOrDefault();
        if (lastMessage == null)
        {
            return Task.FromResult(false);
        }

        // If the RouterAgent is speaking, it might have delegated to a specialist or answered directly.
        if (agent.Name == Constants.Prompts.RouterAgentName)
        {
            // Find the index of the user's latest prompt to isolate the current turn's messaging history.
            int lastUserIndex = history.Count - 1;
            while (lastUserIndex >= 0 && history[lastUserIndex].Role != AuthorRole.User)
            {
                lastUserIndex--;
            }

            int startIndex = Math.Max(0, lastUserIndex);
            
            // Extract all text chunks generated in the current execution turn (message body + content items).
            var texts = history.Skip(startIndex)
                .SelectMany(msg => msg.Items
                    .Select(i => i switch
                    {
                        FunctionResultContent fnResult => fnResult.Result?.ToString(),
                        _ => i.ToString()
                    })
                    .Prepend(msg.Content))
                .Where(t => !string.IsNullOrEmpty(t));

            // If a handoff was requested in this turn, keep the chat loop open (do not terminate).
            // This allows the selected specialist agent to speak next.
            bool hasHandoff = texts.Any(t => t!.Contains(Constants.Prompts.HandoffPrefix));
            return Task.FromResult(!hasHandoff);
        }

        // Specialists (DocumentResearchAgent / WorkspaceAutomationAgent) always terminate after speaking once.
        return Task.FromResult(true);
    }
}
