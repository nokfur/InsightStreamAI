using InsightStreamAI.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using InsightStreamAI.Infrastructure.Data;
using InsightStreamAI.Application.Repositories;
using InsightStreamAI.Infrastructure.Data.Repositories;
using InsightStreamAI.Application.Models;
using InsightStreamAI.Application.Services;
using InsightStreamAI.Infrastructure.Services;
using InsightStreamAI.Infrastructure.Plugins;
using InsightStreamAI.Infrastructure;
using InsightStreamAI.Infrastructure.Filters;
using InsightStreamAI.Services;
using AppConstants = InsightStreamAI.Application.Common.Constants;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults and telemetry pipelines
builder.AddServiceDefaults();
builder.Services.AddInfrastructureTelemetry();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Bind configuration strictly and validate at startup
var aiSettings = builder.Configuration.GetSection("AISettings").Get<AISettings>();
if (aiSettings == null)
{
    throw new InvalidOperationException("AISettings section is missing.");
}
if (aiSettings.UseLocalServer && string.IsNullOrWhiteSpace(aiSettings.LocalEndpoint))
{
    throw new InvalidOperationException("AISettings:LocalEndpoint is required when UseLocalServer is true.");
}
if (string.IsNullOrWhiteSpace(aiSettings.ApiKey))
{
    throw new InvalidOperationException("AISettings:ApiKey is required.");
}
builder.Services.AddSingleton(aiSettings);

// Register HttpClient with named client for web search
builder.Services.AddHttpClient(AppConstants.WebSearch.ClientName);

// Register ITextEmbeddingGenerationService in the root DI container
builder.Services.AddSingleton<ITextEmbeddingGenerationService>(sp =>
{
    var settings = sp.GetRequiredService<AISettings>();
    if (settings.UseLocalServer)
    {
        var endpointUri = new Uri(settings.LocalEndpoint);
        var httpClient = new HttpClient { BaseAddress = endpointUri };
        return new OpenAITextEmbeddingGenerationService(
            modelId: settings.EmbeddingModelId,
            apiKey: settings.ApiKey,
            httpClient: httpClient
        );
    }
    else
    {
        return new OpenAITextEmbeddingGenerationService(
            modelId: settings.EmbeddingModelId,
            apiKey: settings.ApiKey
        );
    }
});

// Register repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

// Register services
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<ISemanticKernelService, SemanticKernelService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICitationTracker, CitationTracker>();

// Workflow HITL Approval services
builder.Services.AddSingleton<IWorkflowApprovalManager, WorkflowApprovalManager>();
builder.Services.AddScoped<AgentSessionContext>();
builder.Services.AddScoped<IFunctionInvocationFilter, WorkflowApprovalFilter>();

// Register plugins for DI
builder.Services.AddTransient<TimePlugin>();
builder.Services.AddTransient<DocumentQueryPlugin>();
builder.Services.AddTransient<WebSearchPlugin>();

// Register Semantic Kernel
builder.Services.AddTransient<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    var settings = sp.GetRequiredService<AISettings>();

    IChatCompletionService chatService;
    if (settings.UseLocalServer)
    {
        var endpointUri = new Uri(settings.LocalEndpoint);
        var httpClient = new HttpClient { BaseAddress = endpointUri };
        
        chatService = new OpenAIChatCompletionService(
            modelId: settings.ChatModelId,
            apiKey: settings.ApiKey,
            httpClient: httpClient
        );
    }
    else
    {
        chatService = new OpenAIChatCompletionService(
            modelId: settings.ChatModelId,
            apiKey: settings.ApiKey
        );
    }

    var wrappedChatService = new HistoryFilteringChatCompletionService(chatService);
    kernelBuilder.Services.AddKeyedSingleton<IChatCompletionService>(settings.ChatModelId, (innerSp, key) => wrappedChatService);
    kernelBuilder.Services.AddSingleton<IChatCompletionService>(innerSp => wrappedChatService);

    // Resolve the embedding service from root DI and register inside the local Kernel builder
    var embeddingService = sp.GetRequiredService<ITextEmbeddingGenerationService>();
    kernelBuilder.Services.AddKeyedSingleton<ITextEmbeddingGenerationService>(settings.EmbeddingModelId, embeddingService);
    kernelBuilder.Services.AddSingleton<ITextEmbeddingGenerationService>(embeddingService);

    // Resolve pre-instantiated plugins from the root service provider (DI)
    kernelBuilder.Plugins.AddFromObject(sp.GetRequiredService<TimePlugin>());
    kernelBuilder.Plugins.AddFromObject(sp.GetRequiredService<DocumentQueryPlugin>());
    kernelBuilder.Plugins.AddFromObject(sp.GetRequiredService<WebSearchPlugin>());

    // Register custom function invocation filter
    kernelBuilder.Services.AddSingleton<IFunctionInvocationFilter>(sp.GetRequiredService<IFunctionInvocationFilter>());

    return kernelBuilder.Build();
});

var app = builder.Build();

// Ensure the database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapDefaultEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<InsightStreamAI.Infrastructure.Hubs.ApprovalHub>(AppConstants.WorkflowApproval.HubRoute);

app.Run();
