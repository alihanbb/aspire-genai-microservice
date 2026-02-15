using AspireApps.CatalogService.Data;
using AspireApps.CatalogService.Endpoint;
using AspireApps.CatalogService.Services;
using System.Reflection;
using AspireApps.ServiceDefaults.Messaging;
using Microsoft.SemanticKernel;
using Catalog.Services;
using Microsoft.Extensions.AI;
using System.Data.Common;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductAIService>();

builder.AddNpgsqlDbContext<ProductDbContext>(connectionName: "catalogdb");

// PostgreSQL Read Replica - for read operations
builder.AddNpgsqlDbContext<ProductReadDbContext>(connectionName: "catalogdb-read");


builder.Services.AddProblemDetails();
builder.Services.AddMasstransitWithAssemblies(Assembly.GetExecutingAssembly());

// Get Ollama endpoint from llama3-2 connection string (which Aspire injects correctly)
// and use it as fallback for all-minilm if its connection string is missing
Uri? ollamaEndpoint = null;
var llamaCs = builder.Configuration.GetConnectionString("llama3-2");
if (!string.IsNullOrEmpty(llamaCs))
{
    var csBuilder = new DbConnectionStringBuilder { ConnectionString = llamaCs };
    if (csBuilder.ContainsKey("Endpoint") && Uri.TryCreate(csBuilder["Endpoint"].ToString(), UriKind.Absolute, out var ep))
    {
        ollamaEndpoint = ep;
    }
}

// 1. Chat Client (uses llama3-2 model)
builder.AddOllamaApiClient("llama3-2")
    .AddKeyedChatClient("llama3-2");

// 2. Embedding Generator (uses all-minilm model)
// If ConnectionStrings:all-minilm is missing, use the same Ollama endpoint from llama3-2
var allMinilmCs = builder.Configuration.GetConnectionString("all-minilm");
if (string.IsNullOrEmpty(allMinilmCs) && ollamaEndpoint != null)
{
    builder.AddOllamaApiClient("all-minilm", settings =>
    {
        settings.Endpoint = ollamaEndpoint;
        settings.SelectedModel = "all-minilm";
    }).AddKeyedEmbeddingGenerator("all-minilm");
}
else
{
    builder.AddOllamaApiClient("all-minilm")
        .AddKeyedEmbeddingGenerator("all-minilm");
}

// HttpClient
builder.Services.AddHttpClient("llama3-2", client => {
    client.Timeout = TimeSpan.FromMinutes(10);
});

// Vector Store
builder.Services.AddInMemoryVectorStoreRecordCollection<ulong, ProductVector>("products");

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();  
    app.MapOpenApi();
    app.UseMigration();
}
else
{
    app.UseExceptionHandler();        
}

app.MapDefaultEndpoints();
app.MapProductEndpoints();

app.Run();
