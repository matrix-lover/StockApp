using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Web;
using StockApp.Models;

namespace StockApp.Services;

public class StockService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public StockService(string apiKey)
    {
        _apiKey = apiKey;
    }

    public virtual async Task<Stock?> GetStockAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            Debug.WriteLine("[StockService] ApiKey empty — skipping network call");
            return null;
        }

        var encoded = HttpUtility.UrlEncode(symbol);
        var url = $"https://finnhub.io/api/v1/quote?symbol={encoded}&token={_apiKey}";

        try
        {
            var resp = await _httpClient.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            Debug.WriteLine($"[StockService] {symbol} HTTP {(int)resp.StatusCode} - {body}");

            if (!resp.IsSuccessStatusCode)
            {
                if ((int)resp.StatusCode == 429)
                    Debug.WriteLine("[StockService] Rate limited (429)");
                return null;
            }

            var q = JsonSerializer.Deserialize<FinnhubQuote>(body, _jsonOptions);
            if (q == null || q.c <= 0) return null;

            return new Stock
            {
                Symbol = symbol,
                Price = q.c,
                Currency = "USD"
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StockService] Exception for {symbol}: {ex.Message}");
            return null;
        }
    }

    public virtual async Task<SearchResult[]> SearchStocksAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return Array.Empty<SearchResult>();

        var encoded = HttpUtility.UrlEncode(query);
        var url = $"https://finnhub.io/api/v1/search?q={encoded}&token={_apiKey}";

        try
        {
            var resp = await _httpClient.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            Debug.WriteLine($"[StockService] Search '{query}' HTTP {(int)resp.StatusCode} - {body}");

            if (!resp.IsSuccessStatusCode) return Array.Empty<SearchResult>();

            var searchResponse = JsonSerializer.Deserialize<SearchResponse>(body, _jsonOptions);
            return searchResponse?.Result?.ToArray() ?? Array.Empty<SearchResult>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StockService] Search exception for '{query}': {ex.Message}");
            return Array.Empty<SearchResult>();
        }
    }
}