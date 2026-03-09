using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using StockApp.Models;

namespace StockApp.Services;

public class StockService : IStockService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _apiKey;

    public StockService(string apiKey = "Мой_ключ_здесь")
    {
        _apiKey = apiKey ?? "";
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StockApp/1.0 (+https://example)");
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<Stock?> GetStockAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            Console.WriteLine("[StockService] Empty API key — skipping network call");
            return null;
        }

        var encoded = Uri.EscapeDataString(symbol);
        var url = $"https://finnhub.io/api/v1/quote?symbol={encoded}&token={_apiKey}";

        try
        {
            var resp = await _httpClient.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[StockService] {symbol} HTTP {(int)resp.StatusCode} - {body}");

            if (!resp.IsSuccessStatusCode)
            {
                if ((int)resp.StatusCode == 429) // rate limit
                {
                    Console.WriteLine("[StockService] Rate limited");
                }
                return null;
            }

            var q = JsonSerializer.Deserialize<FinnhubQuote>(body, _jsonOptions);
            if (q == null || q.c <= 0) return null;

            return new Stock { Symbol = symbol, Price = q.c, Currency = "USD" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StockService] Exception for {symbol}: {ex.Message}");
            return null;
        }
    }

    private class FinnhubQuote { public decimal c { get; set; } }
}