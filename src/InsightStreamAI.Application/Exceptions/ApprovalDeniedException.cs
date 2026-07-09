using System;

namespace InsightStreamAI.Application.Exceptions;

public class ApprovalDeniedException : Exception
{
    public ApprovalDeniedException(string message) : base(message) { }
}
