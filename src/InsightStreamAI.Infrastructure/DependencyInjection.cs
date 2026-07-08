using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace InsightStreamAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureTelemetry(this IServiceCollection services)
    {
        // 1. Enable experimental diagnostics and sensitive data logging (prompts, completions, token usage)
        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnostics", true);
        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

        // 2. Configure tracer provider to include Semantic Kernel activities
        services.ConfigureOpenTelemetryTracerProvider(tracing =>
        {
            tracing.AddSource("Microsoft.SemanticKernel*");
        });

        // 3. Configure meter provider to include Semantic Kernel metrics (token usage, response latencies, etc.)
        services.ConfigureOpenTelemetryMeterProvider(metrics =>
        {
            metrics.AddMeter("Microsoft.SemanticKernel*");
        });

        return services;
    }
}
