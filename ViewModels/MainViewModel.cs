// ViewModels/MainViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Controls;
using StockApp.Models;
using StockApp.Services;

namespace StockApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly StockService _service = new();
    private readonly string[] _symbols =
    {
        "AAPL","MSFT","GOOG","AMZN","TSLA","META","NVDA","NFLX","BABA","INTC","ADBE","ORCL","SAP","IBM"
    };

    public ObservableCollection<Stock> Stocks { get; } = new();

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public ICommand RefreshCommand { get; }

    public MainViewModel()
    {
        RefreshCommand = new Command(async () => await RefreshCommandExecute());
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        Console.WriteLine("[VM] Initializing stocks...");

        foreach (var symbol in _symbols)
        {
            var stock = await _service.GetStockAsync(symbol);
            if (stock != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Stocks.Add(stock);
                    Console.WriteLine($"[VM] Added {stock.Symbol} with price {stock.Price}");
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Stocks.Add(new Stock { Symbol = symbol, Price = 0m, Currency = "USD" });
                    Console.WriteLine($"[VM] Added {symbol} with price 0 (failed to fetch)");
                });
            }
        }

        // Запускаем автообновление каждые 15 секунд
        _ = StartAutoRefresh();
    }

    private async Task StartAutoRefresh()
    {
        while (true)
        {
            try
            {
                await Task.Delay(15000); // 15 секунд

                foreach (var stock in Stocks)
                {
                    var updated = await _service.GetStockAsync(stock.Symbol);
                    if (updated != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            stock.Price = updated.Price;
                            Console.WriteLine($"[VM] Updated {stock.Symbol} to {stock.Price}");
                        });
                    }
                    else
                    {
                        Console.WriteLine($"[VM] Failed to update {stock.Symbol}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VM] AutoRefresh exception: {ex.Message}");
            }
        }
    }

    public async Task RefreshStocksAsync()
    {
        Console.WriteLine("[VM] Pull-to-refresh started...");

        foreach (var stock in Stocks)
        {
            var updated = await _service.GetStockAsync(stock.Symbol);
            if (updated != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    stock.Price = updated.Price;
                    Console.WriteLine($"[VM] Pull-to-refresh updated {stock.Symbol} to {stock.Price}");
                });
            }
            else
            {
                Console.WriteLine($"[VM] Pull-to-refresh failed for {stock.Symbol}");
            }
        }

        Console.WriteLine("[VM] Pull-to-refresh completed");
    }

    private async Task RefreshCommandExecute()
    {
        IsRefreshing = true;
        await RefreshStocksAsync();
        IsRefreshing = false;
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    #endregion
}