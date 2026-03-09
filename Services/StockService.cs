// Services/StockService.cs
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using StockApp.Models;

namespace StockApp.Services;

public class StockService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _apiKey;

    // Передавайте ключ в конструкторе. Для локального теста можно временно указать строку тут.
    public StockService(string apiKey = "Ключ")
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StockApp/1.0 (+https://example)");
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public virtual async Task<Stock> GetStockAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            Console.WriteLine("[StockService] ApiKey empty — skipping network call");
            return null;
        }

        var encoded = HttpUtility.UrlEncode(symbol);
        var url = $"https://finnhub.io/api/v1/quote?symbol={encoded}&token={_apiKey}";

        try
        {
            var resp = await _httpClient.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[StockService] {symbol} HTTP {(int)resp.StatusCode} - {body}");

            if (!resp.IsSuccessStatusCode)
            {
                if ((int)resp.StatusCode == 429) Console.WriteLine("[StockService] Rate limited (429)");
                return null;
            }

            var q = JsonSerializer.Deserialize<FinnhubQuote>(body, _jsonOptions);
            if (q == null) return null;
            if (q.c <= 0) return null;

            return new Stock { Symbol = symbol, Price = q.c, Currency = "USD" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StockService] Exception for {symbol}: {ex.Message}");
            return null;
        }
    }

    private class FinnhubQuote
    {
        public decimal c { get; set; }    // current price
        public decimal h { get; set; }    // high
        public decimal l { get; set; }    // low
        public decimal o { get; set; }    // open
        public long t { get; set; }       // timestamp
    }
}