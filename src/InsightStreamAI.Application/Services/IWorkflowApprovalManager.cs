using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InsightStreamAI.Application.Models;

namespace InsightStreamAI.Application.Services;

public interface IWorkflowApprovalManager
{
    Task<bool> RequestApprovalAsync(Guid conversationId, string functionName, Dictionary<string, object?> arguments);
    void RespondToApproval(Guid conversationId, bool approved);
    WorkflowStatus GetStatus(Guid conversationId);
    void SetStatus(Guid conversationId, WorkflowStatus status);
    ApprovalRequest? GetPendingRequest(Guid conversationId);
}
