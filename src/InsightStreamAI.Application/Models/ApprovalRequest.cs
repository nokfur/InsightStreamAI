using System;
using System.Collections.Generic;

namespace InsightStreamAI.Application.Models;

public class ApprovalRequest
{
    public Guid ConversationId { get; set; }
    public string FunctionName { get; set; } = null!;
    public Dictionary<string, object?> Arguments { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
