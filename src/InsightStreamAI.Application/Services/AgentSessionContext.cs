using System;
using System.Collections.Concurrent;

namespace InsightStreamAI.Application.Services;

public class AgentSessionContext
{
    public Guid ConversationId { get; set; }
    public ConcurrentDictionary<string, byte> ApprovedSignatures { get; } = new();
}
