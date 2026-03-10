using System.Text.Json.Serialization;

namespace StockApp.Models;

public class FinnhubQuote
{
    [JsonPropertyName("c")] public decimal c { get; set; }
    [JsonPropertyName("h")] public decimal h { get; set; }
    [JsonPropertyName("l")] public decimal l { get; set; }
    [JsonPropertyName("o")] public decimal o { get; set; }
    [JsonPropertyName("pc")] public decimal pc { get; set; }
}