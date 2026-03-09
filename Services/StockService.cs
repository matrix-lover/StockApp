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

    // <- ваш ключ Finnhub
    private const string ApiKey = "";

    public StockService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // User-Agent полезно ставить, чтобы сервер не блокировал
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StockApp/1.0 (+https://example)");
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<Stock?> GetStockAsync(string symbol)
    {
        var encoded = HttpUtility.UrlEncode(symbol);
        var url = $"https://finnhub.io/api/v1/quote?symbol={encoded}";

        // Попытка 1: header-based auth
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Finnhub-Secret", ApiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            Console.WriteLine($"[StockService] Fetching {symbol} via HEADER auth...");
            var resp = await _httpClient.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[StockService] HEADER auth HTTP {(int)resp.StatusCode}");
            Console.WriteLine($"[StockService] Response: {body}");

            if (resp.IsSuccessStatusCode)
            {
                var stock = ParseFinnhub(body, symbol);
                if (stock != null) return stock;
                Console.WriteLine($"[StockService] HEADER: parsed null or c==0 for {symbol}");
            }
            else
            {
                if ((int)resp.StatusCode == 429)
                {
                    Console.WriteLine($"[StockService] HEADER: RATE LIMITED for {symbol}");
                    await Task.Delay(1000);
                }
                else if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403 || resp.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"[StockService] HEADER auth failed ({(int)resp.StatusCode}) for {symbol}, trying TOKEN param...");
                }
                else
                {
                    Console.WriteLine($"[StockService] HEADER auth HTTP {(int)resp.StatusCode} for {symbol}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StockService] HEADER auth Exception for {symbol}: {ex.Message}");
        }

        // Попытка 2: token param
        try
        {
            var url2 = $"https://finnhub.io/api/v1/quote?symbol={encoded}&token={ApiKey}";
            Console.WriteLine($"[StockService] Fetching {symbol} via TOKEN param...");
            var resp2 = await _httpClient.GetAsync(url2);
            var body2 = await resp2.Content.ReadAsStringAsync();
            Console.WriteLine($"[StockService] TOKEN param HTTP {(int)resp2.StatusCode}");
            Console.WriteLine($"[StockService] Response: {body2}");

            if (resp2.IsSuccessStatusCode)
            {
                var stock = ParseFinnhub(body2, symbol);
                if (stock != null) return stock;
                Console.WriteLine($"[StockService] TOKEN: parsed null or c==0 for {symbol}");
            }
            else
            {
                if ((int)resp2.StatusCode == 429)
                {
                    Console.WriteLine($"[StockService] TOKEN: RATE LIMITED for {symbol}");
                }
                else
                {
                    Console.WriteLine($"[StockService] TOKEN auth HTTP {(int)resp2.StatusCode} for {symbol}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StockService] TOKEN param Exception for {symbol}: {ex.Message}");
        }

        return null;
    }

    private Stock? ParseFinnhub(string json, string symbol)
    {
        try
        {
            var d = JsonSerializer.Deserialize<FinnhubQuote>(json, _jsonOptions);
            if (d == null) return null;
            // Finnhub может вернуть c == 0 вне торгового времени или для неверного тикера
            if (d.c <= 0) return null;
            return new Stock { Symbol = symbol, Price = d.c, Currency = "USD" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StockService] Parse exception for {symbol}: {ex.Message}");
            return null;
        }
    }

    private class FinnhubQuote
    {
        public decimal c { get; set; } // текущая цена
    }
}