using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using System.Text.Json.Serialization;

namespace FBTRAGCore.Models
{
    public class DocumentRecord
    {
        [VectorStoreKey]
        public Guid Id { get; set; }

        [VectorStoreVector(Dimensions: 4, DistanceFunction = DistanceFunction.CosineSimilarity)]
        public Embedding<float> Embedding { get; set; }

        [VectorStoreData]
        public string Content { get; set; }

        [VectorStoreData]
        public string Source { get; set; }

        [VectorStoreData]
        [JsonPropertyName("page_number")]
        public int PageNumber { get; set; }

        [VectorStoreData]
        [JsonPropertyName("chunk_index")]
        public int ChunkIndex { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = "General";
    }
}
