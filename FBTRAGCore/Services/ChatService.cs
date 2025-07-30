
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.VectorData;
using FBTRAGCore.Models;
using System.Text;
using Qdrant.Client.Grpc;
using Qdrant.Client;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Collections.Concurrent;

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

        // Static in-memory chat history per user (use cache/db in real apps)
        private static readonly ConcurrentDictionary<string, ChatHistory> _userChatHistories = new();

        public ChatService(
            Kernel kernel,
            VectorStore vectorStore,
            IServiceProvider serviceProvider,
            ILogger<ChatService> logger)
        {
            _kernel = kernel;
            _vectorStore = vectorStore;
            _logger = logger;
            _embeddingGenerator = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<string> GetChatResponseAsync(string userQuery, string userId)
        {
            _logger.LogInformation("Processing query for user '{UserId}': {Query}", userId, userQuery);

            // 1. Generate embedding
            Embedding<float> userQueryEmbedding;
            try
            {
                userQueryEmbedding = await _embeddingGenerator.GenerateAsync(userQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Embedding failed.");
                return "I'm sorry, I couldn't process your request.";
            }

            // 2. Semantic search
            var collection = _vectorStore.GetCollection<Guid, DocumentRecord>(CollectionName);
            var searchOptions = new VectorSearchOptions<DocumentRecord>();


            List<DocumentRecord> relevantDocs = new();
            try
            {
                var results = collection.SearchAsync<Embedding<float>>(userQueryEmbedding, 3, new VectorSearchOptions<DocumentRecord> { IncludeVectors = true });
                await foreach (var result in results)
                {
                    if (!string.IsNullOrWhiteSpace(result?.Record?.Content))
                        relevantDocs.Add(result.Record);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Qdrant search failed.");
            }

            // 3. Prepare or get chat history
            var chatHistory = _userChatHistories.GetOrAdd(userId, new ChatHistory());
            if (chatHistory.Count == 0)
            {
                chatHistory.AddSystemMessage("You are a helpful assistant. Use provided knowledge base if available. If not, respond generally.");
            }

            // 4. Add context if documents found
            if (relevantDocs.Any())
            {
                StringBuilder contextBuilder = new();
                contextBuilder.AppendLine("Context from knowledge base:");
                foreach (var doc in relevantDocs.OrderBy(d => d.PageNumber).ThenBy(d => d.ChunkIndex))
                {
                    contextBuilder.AppendLine($"- From '{doc.Source}', Page {doc.PageNumber}, Chunk {doc.ChunkIndex}: {doc.Content}");
                }
                contextBuilder.AppendLine("End of context.\n");
                chatHistory.AddUserMessage($"{contextBuilder}\nBased on the context, answer: {userQuery}");
            }
            else
            {
                chatHistory.AddUserMessage(userQuery);
            }

            // 5. Get LLM reply
            string response;
            try
            {
                OllamaPromptExecutionSettings ollamaPromptExecutionSettings = new OllamaPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };
                var result = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, ollamaPromptExecutionSettings, _kernel);
                response = result.Content;
                chatHistory.AddAssistantMessage(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM call failed.");
                return "Sorry, I couldn't get a response from the AI.";
            }

            return response;
        }
    }

}
