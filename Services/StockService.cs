// Services/StockService.cs
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using StockApp.Models;

namespace StockApp.Services;

public class StockService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    // временно оставляем пустой ключ — для реальной работы вставьте свой ключ
    private const string ApiKey = "Здесь_ключ";

    public StockService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StockApp/1.0 (+https://example)");
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    // Сделаем метод виртуальным и вернём Task<Stock?> (правильный тип)
    public virtual async Task<Stock?> GetStockAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            Console.WriteLine("[StockService] ApiKey is empty — skipping real network call");
            return null;
        }

        var encoded = HttpUtility.UrlEncode(symbol);
        var url = $"https://finnhub.io/api/v1/quote?symbol={encoded}&token={ApiKey}";

        try
        {
            var resp = await _httpClient.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[StockService] TOKEN param HTTP {(int)resp.StatusCode} for {symbol}: {body}");

            if (!resp.IsSuccessStatusCode)
            {
                if ((int)resp.StatusCode == 429) Console.WriteLine("[StockService] Rate limited");
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
