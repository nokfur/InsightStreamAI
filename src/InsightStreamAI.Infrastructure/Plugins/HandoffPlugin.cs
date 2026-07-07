using Microsoft.SemanticKernel;
using System.ComponentModel;
using InsightStreamAI.Application.Common;

namespace InsightStreamAI.Infrastructure.Plugins;

public class HandoffPlugin
{
    [KernelFunction, Description("Call this function to transfer control to the Document Research Agent when the user asks questions that require document search, retrieval, or analyzing local files/PDFs.")]
    public string HandOverToDocumentResearch() => $"{Constants.Prompts.HandoffPrefix} {Constants.Prompts.DocumentResearchAgentName}";

    [KernelFunction, Description("Call this function to transfer control to the Workspace Automation Agent when the user asks to search the web for current info, check the current time, or execute platform workflows.")]
    public string HandOverToWorkspaceAutomation() => $"{Constants.Prompts.HandoffPrefix} {Constants.Prompts.WorkspaceAutomationAgentName}";

    [KernelFunction, Description("Call this function to transfer control back to the central Router Agent when a specialist agent is done answering or needs the Router to guide the next steps.")]
    public string HandOverToRouter() => $"{Constants.Prompts.HandoffPrefix} {Constants.Prompts.RouterAgentName}";
}
