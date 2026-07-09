using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using InsightStreamAI.Application.Services;
using InsightStreamAI.Application.Models;
using InsightStreamAI.Infrastructure.Hubs;
using InsightStreamAI.Application.Common;

namespace InsightStreamAI.Services;

public class WorkflowApprovalManager(IHubContext<ApprovalHub> hubContext) : IWorkflowApprovalManager
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _pendingApprovals = new();
    private readonly ConcurrentDictionary<Guid, WorkflowStatus> _statuses = new();
    private readonly ConcurrentDictionary<Guid, ApprovalRequest> _pendingRequests = new();

    public async Task<bool> RequestApprovalAsync(Guid conversationId, string functionName, Dictionary<string, object?> arguments)
    {
        // 1. Update status and store approval metadata
        _statuses[conversationId] = WorkflowStatus.PendingApproval;

        var request = new ApprovalRequest
        {
            ConversationId = conversationId,
            FunctionName = functionName,
            Arguments = arguments
        };
        _pendingRequests[conversationId] = request;

        // 2. Create uncompleted TaskCompletionSource
        var tcs = new TaskCompletionSource<bool>();
        _pendingApprovals[conversationId] = tcs;

        // 3. Dispatch real-time SignalR event to UI
        var argumentsJson = JsonSerializer.Serialize(arguments);
        await hubContext.Clients.All.SendAsync(Constants.WorkflowApproval.ReceiveApprovalRequestEvent, conversationId.ToString(), functionName, argumentsJson);

        try
        {
            // 4. Await task completion (non-blocking thread yield)
            bool approved = await tcs.Task;
            
            // 5. Update status based on outcome
            _statuses[conversationId] = approved ? WorkflowStatus.Approved : WorkflowStatus.Denied;
            
            return approved;
        }
        finally
        {
            _pendingApprovals.TryRemove(conversationId, out _);
            _pendingRequests.TryRemove(conversationId, out _);
        }
    }

    public void RespondToApproval(Guid conversationId, bool approved)
    {
        if (_pendingApprovals.TryGetValue(conversationId, out var tcs))
        {
            tcs.TrySetResult(approved);
        }
    }

    public WorkflowStatus GetStatus(Guid conversationId)
    {
        return _statuses.TryGetValue(conversationId, out var status) ? status : WorkflowStatus.Idle;
    }

    public void SetStatus(Guid conversationId, WorkflowStatus status)
    {
        _statuses[conversationId] = status;
    }

    public ApprovalRequest? GetPendingRequest(Guid conversationId)
    {
        return _pendingRequests.TryGetValue(conversationId, out var request) ? request : null;
    }
}
