using Microsoft.AspNetCore.SignalR;
using InsightStreamAI.Application.Services;

namespace InsightStreamAI.Infrastructure.Hubs;

public class ApprovalHub(IWorkflowApprovalManager approvalManager) : Hub
{
    public void RespondToApproval(string conversationIdStr, bool approved)
    {
        if (Guid.TryParse(conversationIdStr, out var conversationId))
        {
            approvalManager.RespondToApproval(conversationId, approved);
        }
    }
}
