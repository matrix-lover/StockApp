using System.Collections.Generic;
using System.Threading.Tasks;
using StockApp.Models;

namespace StockApp.Services;

public class MockStockService : StockService
{
    public MockStockService(string apiKey = "") : base(apiKey)
    {
    }

    public override async Task<Stock?> GetStockAsync(string symbol)
    {
        await Task.Delay(100); // имитация сети
        return new Stock { Symbol = symbol, Price = 100m };
    }

    public override async Task<SearchResult[]> SearchStocksAsync(string query)
    {
        await Task.Delay(100);
        var list = new List<SearchResult>
        {
            new SearchResult { Symbol = "AAPL", Description = "Apple Inc." },
            new SearchResult { Symbol = "MSFT", Description = "Microsoft Corp." }
        };
        return list.ToArray();
    }
}