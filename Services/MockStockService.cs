// Services/MockStockService.cs
using System;
using System.Threading.Tasks;
using StockApp.Models;

namespace StockApp.Services;

public class MockStockService : StockService
{
    private readonly Random _r = new();

    // override базовый виртуальный метод
    public override async Task<Stock?> GetStockAsync(string symbol)
    {
        await Task.Delay(50); // имитация сети
        return new Stock
        {
            Symbol = symbol,
            Price = Math.Round((decimal)(100 + _r.NextDouble() * 900), 2),
            Currency = "USD"
        };
    }
}