// Services/MockStockService.cs
using System;
using System.Threading.Tasks;
using StockApp.Models;

namespace StockApp.Services;

public class MockStockService : StockService // либо реализовать IStockService, но для быстроты:
{
    private readonly Random _r = new();
    public new async Task<Stock?> GetStockAsync(string symbol)
    {
        await Task.Delay(50);
        return new Stock { Symbol = symbol, Price = Math.Round((decimal)(100 + _r.NextDouble() * 900), 2), Currency = "USD" };
    }
}

