using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using StockApp.Models;
using StockApp.Services;

namespace StockApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly StockService _service;
    private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

    public ObservableCollection<Stock> Stocks { get; } = new();

    public ICommand RefreshCommand { get; }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public MainViewModel(StockService? service = null)
    {
        _service = service ?? new MockStockService();
        RefreshCommand = new Command(async () => await RefreshCommandExecute());
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var symbols = new[] { "AAPL","MSFT","GOOG","AMZN","TSLA","META","NVDA","NFLX","BABA","INTC","ADBE","ORCL","SAP","IBM" };

        foreach (var s in symbols)
        {
            var st = await _service.GetStockAsync(s) ?? new Stock { Symbol = s, Price = 0m };
            MainThread.BeginInvokeOnMainThread(() => Stocks.Add(st));
        }

        // Запускаем автообновление в фоне (не трогает IsRefreshing)
        _ = StartAutoRefresh();
    }

    // Pull-to-refresh (с индикатором)
    private async Task RefreshCommandExecute()
    {
        // Попытка выполнить только одну ручную операцию
        if (!await _refreshLock.WaitAsync(0))
        {
            Console.WriteLine("[VM] Manual refresh skipped because another refresh is running");
            return;
        }

        try
        {
            IsRefreshing = true;
            Console.WriteLine("[VM] Manual refresh START");
            await RefreshStocksAsync();
            Console.WriteLine("[VM] Manual refresh DONE");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[VM] Manual refresh EX: " + ex);
        }
        finally
        {
            IsRefreshing = false;
            _refreshLock.Release();
            Console.WriteLine("[VM] IsRefreshing set to false (manual)");
        }
    }

    // Общая логика обновления (используется и auto, и manual)
    private async Task RefreshStocksAsync()
    {
        foreach (var stock in Stocks)
        {
            try
            {
                var updated = await _service.GetStockAsync(stock.Symbol);
                if (updated != null)
                {
                    MainThread.BeginInvokeOnMainThread(() => stock.Price = updated.Price);
                }
                else
                {
                    Console.WriteLine($"[VM] update returned null for {stock.Symbol}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VM] exception updating {stock.Symbol}: {ex.Message}");
            }

            // маленькая задержка, чтобы снизить вероятность rate-limit
            await Task.Delay(200);
        }
    }

    // Автообновление — НЕ трогает IsRefreshing; если ручной refresh идёт, авто пропускается
    private async Task StartAutoRefresh()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60)); // интервал автообновления

                // Попробовать захватить лок, но не ждать. Если не удалось — пропустить этот проход.
                if (!await _refreshLock.WaitAsync(0))
                {
                    Console.WriteLine("[VM] Auto refresh skipped because manual refresh is running");
                    continue;
                }

                try
                {
                    Console.WriteLine("[VM] Auto refresh START");
                    await RefreshStocksAsync();
                    Console.WriteLine("[VM] Auto refresh DONE");
                }
                finally
                {
                    _refreshLock.Release();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[VM] AutoRefresh EX: " + ex.Message);
                await Task.Delay(5000);
            }
        }
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    #endregion
}