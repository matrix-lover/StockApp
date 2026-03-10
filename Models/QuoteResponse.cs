using System.Text.Json.Serialization;

namespace StockApp.Models;

public class QuoteResponse
{
    [JsonPropertyName("c")]
    public decimal CurrentPrice { get; set; }
}