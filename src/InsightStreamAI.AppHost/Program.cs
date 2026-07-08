using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Explicitly load User Secrets to ensure availability during all debug configurations
builder.Configuration.AddUserSecrets<Program>();

// Register database connection string resource (retrieved from AppHost configuration/secrets automatically)
var sqlite = builder.AddConnectionString("DefaultConnection");

// Read AI configurations (falls back to User Secrets automatically)
var useLocalServer = builder.Configuration["AISettings:UseLocalServer"];
var localEndpoint = builder.Configuration["AISettings:LocalEndpoint"];
var chatModelId = builder.Configuration["AISettings:ChatModelId"];
var embeddingModelId = builder.Configuration["AISettings:EmbeddingModelId"];
var apiKey = builder.Configuration["AISettings:ApiKey"];

// Register and configure the Blazor application with references and settings
builder.AddProject<Projects.InsightStreamAI>("insightstream-ai")
    .WithReference(sqlite)
    .WithEnvironment("AISettings__UseLocalServer", useLocalServer)
    .WithEnvironment("AISettings__LocalEndpoint", localEndpoint)
    .WithEnvironment("AISettings__ChatModelId", chatModelId)
    .WithEnvironment("AISettings__EmbeddingModelId", embeddingModelId)
    .WithEnvironment("AISettings__ApiKey", apiKey);

builder.Build().Run();
