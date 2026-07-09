using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.SemanticKernel;
using InsightStreamAI.Application.Services;
using InsightStreamAI.Application.Exceptions;

namespace InsightStreamAI.Infrastructure.Filters;

public class WorkflowApprovalFilter(
    IWorkflowApprovalManager approvalManager, 
    AgentSessionContext sessionContext) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var functionName = context.Function.Name;
        var conversationId = sessionContext.ConversationId;

        // Check if approval is required:
        // 1. By presence of [RequiresApproval] attribute on the underlying method
        bool requiresApproval = false;

        // Check custom attribute using reflection on target method to bypass the internal visibility of KernelFunctionFromMethod
        // In modern Semantic Kernel, the property is named "UnderlyingMethod", falling back to "MethodInfo" for older versions
        var methodInfoProp = context.Function.GetType().GetProperty("UnderlyingMethod", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? context.Function.GetType().GetProperty("MethodInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var methodInfo = methodInfoProp?.GetValue(context.Function) as MethodInfo;

        if (methodInfo != null && methodInfo.GetCustomAttributes(typeof(RequiresApprovalAttribute), true).Any())
        {
            requiresApproval = true;
        }

        if (requiresApproval && conversationId != Guid.Empty)
        {
            // Filter out non-serializable arguments like CancellationToken
            var arguments = context.Arguments
                .Where(kvp => kvp.Value is not CancellationToken)
                .ToDictionary(k => k.Key, v => v.Value);
            var argumentsJson = System.Text.Json.JsonSerializer.Serialize(arguments);
            var signature = $"{functionName}:{argumentsJson}";

            if (sessionContext.ApprovedSignatures.ContainsKey(signature))
            {
                await next(context);
                return;
            }

            // Pause execution and wait for user response
            bool approved = await approvalManager.RequestApprovalAsync(conversationId, functionName, arguments);

            if (!approved)
            {
                throw new ApprovalDeniedException($"The automation task execution for function '{functionName}' was denied by the user.");
            }

            // Record approval for the rest of this user prompt turn
            sessionContext.ApprovedSignatures.TryAdd(signature, 0);
        }

        await next(context);
    }
}
