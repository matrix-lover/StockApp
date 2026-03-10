using System.Text.Json;
using Microsoft.Maui.Storage;
using StockApp.Models;

namespace StockApp.Services;

public class StockStorageService
{
    private const string KEY = "stocks_list";

    public void SaveStocks(List<Stock> stocks)
    {
        var json = JsonSerializer.Serialize(stocks);
        Preferences.Set(KEY, json);
    }

    public List<Stock> LoadStocks()
    {
        var json = Preferences.Get(KEY, string.Empty);

        if (string.IsNullOrEmpty(json))
            return new List<Stock>();

        return JsonSerializer.Deserialize<List<Stock>>(json) ?? new List<Stock>();
    }
}