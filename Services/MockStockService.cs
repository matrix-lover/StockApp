using System;
using System.Threading.Tasks;
using StockApp.Models;

namespace StockApp.Services;

public class MockStockService : IStockService
{
    private readonly Random _r = new();

    public async Task<Stock?> GetStockAsync(string symbol)
    {
        // имитируем задержку сети и ненулевой ответ
        await Task.Delay(200);
        return new Stock
        {
            Symbol = symbol,
            Price = Math.Round((decimal)(100 + _r.NextDouble() * 900), 2),
            Currency = "USD"
        };
    }
}