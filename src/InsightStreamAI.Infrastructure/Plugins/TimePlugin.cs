using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace InsightStreamAI.Infrastructure.Plugins;

public class TimePlugin
{
    [KernelFunction, Description("Gets the current local date and time.")]
    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("F");
    }
}
