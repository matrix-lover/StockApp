using System.Text.Json.Serialization;

namespace StockApp.Models
{
    public class SearchResult
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("displaySymbol")]
        public string DisplaySymbol { get; set; }

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class SearchResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("result")]
        public List<SearchResult> Result { get; set; }
    }
}