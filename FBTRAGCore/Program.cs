using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.KernelMemory.Service.AspNetCore;
using FBTRAGCore;
using FBTRAGCore.Plugin;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Chroma;
using Qdrant.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.Extensions.VectorData;
using FBTRAGCore.Services;
using Microsoft.KernelMemory.AI;
using Qdrant.Client.Grpc;
using Grpc.Net.Client;
using System.Net.Http;
using Grpc.Core.Interceptors;
using System.Security.Authentication;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddTransient<IPdfIngestionService, PdfIngestionService>();
builder.Services.AddTransient<IChatService, ChatService>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4400")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<QdrantClient>(sp =>
{
    var host = "41b85ddc-a2d9-4179-808e-661dd79ca137.europe-west3-0.gcp.cloud.qdrant.io";
    var qdrantApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY2Nlc3MiOiJtIiwiZXhwIjoxNzU2Mzk1OTEzfQ.fdCaHN4BPU_WJ_trtwxZANA2lncRSdYnGcW0RxQkdh0";

    HttpClient.DefaultProxy = new WebProxy();
    var qdrantClient = new QdrantClient(host: host, https: true, apiKey: qdrantApiKey);    
    return qdrantClient;
});

// Register QdrantVectorStore as the IVectorStore implementation
builder.Services.AddSingleton<VectorStore>(sp =>
{
    var qdrantClient = sp.GetRequiredService<QdrantClient>();
    // You can pass QdrantVectorStoreOptions here if needed for specific configurations
    return new QdrantVectorStore(qdrantClient, false, new QdrantVectorStoreOptions {  });
});

// Configure Semantic Kernel
builder.Services.AddSingleton<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    var ollamaBaseUrl = "http://localhost:11434/";
    var ollamaChatModelId = "llama3.1:8b";
    var ollamaEmbeddingModelId = "nomic-embed-text";

    kernelBuilder.AddOllamaChatCompletion(
        modelId: ollamaChatModelId,
        endpoint: new Uri(ollamaBaseUrl)
    );

    kernelBuilder.AddOllamaEmbeddingGenerator(
        modelId: ollamaEmbeddingModelId,
        endpoint: new Uri(ollamaBaseUrl)
    );
    kernelBuilder.Plugins.AddFromType<FbtPlugins>("Fbt");

    return kernelBuilder.Build();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors("AllowAngularApp");
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
