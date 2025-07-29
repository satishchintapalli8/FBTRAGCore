
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.VectorData;
using FBTRAGCore.Models;
using System.Text;
using Qdrant.Client.Grpc;
using Qdrant.Client;

namespace FBTRAGCore.Services
{
    public class ChatService : IChatService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<ChatService> _logger;
        private readonly VectorStore _vectorStore; 
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator; 
        private readonly IChatCompletionService _chatCompletionService; 

        private const string CollectionName = "fbt-knowledge-base";

        public IServiceProvider ServiceProvider { get; }

        public ChatService(
            Kernel kernel,
            VectorStore vectorStore,
            IServiceProvider serviceProvider,
            ILogger<ChatService> logger)
        {
            _kernel = kernel;
            _vectorStore = vectorStore;
            ServiceProvider = serviceProvider;
            _logger = logger;
            _embeddingGenerator = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>(); 
        }

        public async Task<string> GetChatResponseAsync(string userQuery, string categoryFilter = null)
        {
            _logger.LogInformation("Processing chat query: '{UserQuery}'", userQuery);

            // 1. Generate embedding for the user's query
            Embedding<float> userQueryEmbedding;
            try
            {
                userQueryEmbedding = await _embeddingGenerator.GenerateAsync(userQuery);
                _logger.LogDebug("User query embedded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for user query.");
                return "I'm sorry, I couldn't process your request right now due to an embedding error.";
            }

            // 2. Perform semantic search in Qdrant            
            var collection = _vectorStore.GetCollection<Guid, DocumentRecord>(CollectionName);
            var searchOptions = new VectorSearchOptions<DocumentRecord>
            {
                           
            };

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                searchOptions.Filter = r => r.Category == categoryFilter;
                _logger.LogInformation("Applying category filter: {Category}", categoryFilter);
            }

            List<DocumentRecord> relevantDocuments = new List<DocumentRecord>();
            try
            {
                int topResult = 3;
                VectorSearchOptions<DocumentRecord> vectorSearchExtensions = new VectorSearchOptions<DocumentRecord>()
                {
                    IncludeVectors = true
                };
                var results = collection.SearchAsync<Embedding<float>>(userQueryEmbedding, topResult, vectorSearchExtensions);
                await foreach (var result in results)
                {
                    if (result != null && result.Record != null && !string.IsNullOrWhiteSpace(result.Record.Content))
                    {
                        relevantDocuments.Add(result.Record);
                        _logger.LogDebug("Found relevant document: {Source} (Page: {PageNumber}, Score: {Score})", result.Record.Source, result.Record.PageNumber, result.Score);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Qdrant for relevant documents.");                
            }
            
            var chatHistory = new ChatHistory();
            //chatHistory.AddSystemMessage("You are a helpful assistant. Answer questions based on the provided context. If the answer is not in the context, state that you don't know, and do not make up information. Be concise and to the point.");

            if (relevantDocuments.Any())
            {
                StringBuilder contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("Context from knowledge base:");
                foreach (var doc in relevantDocuments.OrderBy(d => d.PageNumber).ThenBy(d => d.ChunkIndex))
                {
                    contextBuilder.AppendLine($"- From '{doc.Source}', Page {doc.PageNumber}, Chunk {doc.ChunkIndex}: {doc.Content}");
                }
                contextBuilder.AppendLine("End of context.");
                contextBuilder.AppendLine();

                chatHistory.AddUserMessage(contextBuilder.ToString() + "Based on the provided context, answer the following question: " + userQuery);
                _logger.LogInformation("Augmented prompt with {Count} relevant documents.", relevantDocuments.Count);
            }
            else
            {
                _logger.LogWarning("No relevant documents found for query: '{UserQuery}'. Answering without specific context.", userQuery);
                chatHistory.AddUserMessage("No specific context found. " + userQuery);
            }

            // 4. Get response from the LLM (Llama via Ollama)
            string llmResponse;
            try
            {
                var result = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);
                llmResponse = result.Content;
                _logger.LogInformation("LLM responded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting response from LLM (Ollama).");
                return "I'm sorry, I couldn't get a response from the AI model at this moment.";
            }

            return llmResponse;
        }
    }
}
