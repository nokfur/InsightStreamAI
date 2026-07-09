using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using InsightStreamAI.Domain.Entities;
using InsightStreamAI.Application.Services;
using InsightStreamAI.Application.Common;
using InsightStreamAI.Infrastructure.Plugins;
using InsightStreamAI.Application.Models;
using InsightStreamAI.Application.Exceptions;
using System.Runtime.CompilerServices;


namespace InsightStreamAI.Infrastructure.Services;

public class SemanticKernelService(
    Kernel kernel,
    TimePlugin timePlugin,
    DocumentQueryPlugin documentQueryPlugin,
    WebSearchPlugin webSearchPlugin,
    IWorkflowApprovalManager approvalManager,
    AgentSessionContext sessionContext,
    IFunctionInvocationFilter approvalFilter) : ISemanticKernelService
{
    public async IAsyncEnumerable<string> GetStreamingResponseAsync(
        List<ChatMessage> conversationHistory, 
        string userPrompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var conversationId = conversationHistory.FirstOrDefault()?.ConversationId ?? Guid.Empty;
        if (conversationId != Guid.Empty)
        {
            sessionContext.ConversationId = conversationId;
            sessionContext.ApprovedSignatures.Clear();
            approvalManager.SetStatus(conversationId, WorkflowStatus.Running);
        }

        try
        {
        // 1. Create shallow cloned kernels with designated plugin scoping
        var routingKernel = kernel.Clone();
        routingKernel.Plugins.Clear();
        routingKernel.Plugins.AddFromObject(new HandoffPlugin());
        routingKernel.FunctionInvocationFilters.Add(approvalFilter);

        var documentResearchKernel = kernel.Clone();
        documentResearchKernel.Plugins.Clear();
        documentResearchKernel.Plugins.AddFromObject(documentQueryPlugin);
        documentResearchKernel.Plugins.AddFromObject(new HandoffPlugin());
        documentResearchKernel.FunctionInvocationFilters.Add(approvalFilter);

        var workspaceAutomationKernel = kernel.Clone();
        workspaceAutomationKernel.Plugins.Clear();
        workspaceAutomationKernel.Plugins.AddFromObject(webSearchPlugin);
        workspaceAutomationKernel.Plugins.AddFromObject(timePlugin);
        workspaceAutomationKernel.Plugins.AddFromObject(new HandoffPlugin());
        workspaceAutomationKernel.FunctionInvocationFilters.Add(approvalFilter);

        // 2. Define specialized agents
        var routerAgent = new ChatCompletionAgent
        {
            Name = Constants.Prompts.RouterAgentName,
            Description = "Central agent evaluating user intent to delegate to specialised sub-agents.",
            Instructions = Constants.Prompts.RouterAgentInstructions,
            Kernel = routingKernel,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions })
        };

        var researchAgent = new ChatCompletionAgent
        {
            Name = Constants.Prompts.DocumentResearchAgentName,
            Description = "Agent specialized in vector retrieval and contextual search in uploaded documents.",
            Instructions = Constants.Prompts.DocumentResearchAgentInstructions,
            Kernel = documentResearchKernel,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions })
        };

        var automationAgent = new ChatCompletionAgent
        {
            Name = Constants.Prompts.WorkspaceAutomationAgentName,
            Description = "Agent specialized in checking time and executing DuckDuckGo search engine workflows.",
            Instructions = Constants.Prompts.WorkspaceAutomationAgentInstructions,
            Kernel = workspaceAutomationKernel,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions })
        };

        // 3. Create the coordinated group chat
        var chat = new AgentGroupChat(routerAgent, researchAgent, automationAgent)
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                SelectionStrategy = new MultiAgentSelectionStrategy
                {
                    InitialAgent = routerAgent
                },
                TerminationStrategy = new MultiAgentTerminationStrategy()
            }
        };

        // 4. Seed conversation history
        foreach (var msg in conversationHistory)
        {
            var role = msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? AuthorRole.User : AuthorRole.Assistant;
            chat.AddChatMessage(new ChatMessageContent(role, msg.Content) 
            { 
                AuthorName = msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? null : Constants.Prompts.RouterAgentName
            });
        }

        // 5. Invoke execution and stream output
        IAsyncEnumerable<StreamingChatMessageContent>? stream = null;
        string? errorMessage = null;
        try
        {
            stream = chat.InvokeStreamingAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error initiating multi-agent chat stream: {ex.Message}";
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
                    // Hide routing command instructions from UI output
                    if (chunk.Content.Contains(Constants.Prompts.HandoffPrefix) || chunk.Content.Contains("HANDOFF_TO"))
                    {
                        continue;
                    }
                    yield return chunk.Content;
                }
            }
        }
        }
        finally
        {
            if (conversationId != Guid.Empty)
            {
                approvalManager.SetStatus(conversationId, WorkflowStatus.Idle);
            }
        }
    }
}

