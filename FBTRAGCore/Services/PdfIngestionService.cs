using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using UglyToad.PdfPig;
using FBTRAGCore.Models;
using Microsoft.Extensions.AI;
using Qdrant.Client.Grpc;
using Qdrant.Client;

namespace FBTRAGCore.Services
{
    public class PdfIngestionService : IPdfIngestionService
    {
        private readonly Kernel _kernel;
        private readonly VectorStore _vectorStore;
        private readonly ILogger<PdfIngestionService> _logger;
        private const string CollectionName = "fbt-knowledge-base";

        public IServiceProvider ServiceProvider { get; }

        public PdfIngestionService(Kernel kernel, IServiceProvider serviceProvider, VectorStore vectorStore, ILogger<PdfIngestionService> logger)
        {
            _kernel = kernel;
            ServiceProvider = serviceProvider;
            _vectorStore = vectorStore;
            _logger = logger;
        }
        public async Task IngestPdfAsync(Stream pdfStream, string fileName, string category)
        {
            _logger.LogInformation("Starting PDF ingestion for file: {FileName}", fileName);
                       
            List<string> pageTexts = new List<string>();
            try
            {
                using (var document = PdfDocument.Open(pdfStream))
                {
                    foreach (var page in document.GetPages())
                    {
                        var text = page.Text;
                        pageTexts.Add(text);
                        _logger.LogDebug("Extracted text from page {PageNumber} of {FileName}", page.Number, fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF file: {FileName}", fileName);
                throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}", ex);
            }

            if (!pageTexts.Any())
            {
                _logger.LogWarning("No text extracted from PDF file: {FileName}", fileName);
                return;
            }

            // 2. Text Pre-processing & Chunking
            var textSplitter = new RecursiveCharacterTextSplitter(
                chunkSize: 1000,   
                overlapSize: 100    
            );

            try
            {
                VectorParams vectorParams = new VectorParams
                {
                    Size = 768,
                    Distance = Distance.Cosine 
                };
                var embeddingGenerator = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
                var qdrantClient = ServiceProvider.GetRequiredService<QdrantClient>();
                if(await qdrantClient.CollectionExistsAsync(CollectionName))
                {
                    await qdrantClient.DeleteCollectionAsync(CollectionName);
                }
                await qdrantClient.CreateCollectionAsync(collectionName: CollectionName, vectorsConfig: vectorParams);
                var collection = _vectorStore.GetCollection<Guid, DocumentRecord>(CollectionName);
                
                var recordsToUpsert = new List<DocumentRecord>();
                for (int pageNum = 0; pageNum < pageTexts.Count; pageNum++)
                {
                    var chunks = textSplitter.Split(pageTexts[pageNum]).ToList();

                    for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
                    {
                        var chunkContent = chunks[chunkIndex];
                        // 3. Embedding Generation for each chunk
                        Embedding<float> embedding;
                        try
                        {
                            EmbeddingGenerationOptions options = new EmbeddingGenerationOptions();
                            embedding = await embeddingGenerator.GenerateAsync(chunkContent, options);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating embedding for chunk from {FileName}, page {PageNumber}, chunk {ChunkIndex}", fileName, pageNum + 1, chunkIndex);
                            continue;
                        }

                        // 4. Create DocumentRecord for storage
                        recordsToUpsert.Add(new DocumentRecord
                        {
                            Id = Guid.NewGuid(),
                            Content = chunkContent,
                            Source = fileName,
                            PageNumber = pageNum + 1,
                            ChunkIndex = chunkIndex,
                            Category = category,
                            Embedding = embedding
                        });
                    }
                }

                if (recordsToUpsert.Any())
                {
                    try
                    {
                        await collection.UpsertAsync(recordsToUpsert);
                        _logger.LogInformation("Successfully ingested {RecordCount} records for file: {FileName}", recordsToUpsert.Count, fileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error upserting records to Qdrant for file: {FileName}", fileName);
                        throw new InvalidOperationException($"Failed to save data to Qdrant: {ex.Message}", ex);
                    }
                }
                else
                {
                    _logger.LogWarning("No records to upsert for file: {FileName} after processing.", fileName);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }        
    }
}
