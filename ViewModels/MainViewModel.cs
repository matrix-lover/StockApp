using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using StockApp.Models;
using StockApp.Services;

namespace StockApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IStockService _service;

    public ObservableCollection<Stock> Stocks { get; } = new();

    public ICommand RefreshCommand { get; }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public MainViewModel(IStockService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        RefreshCommand = new Command(async () => await RefreshCommandExecute());
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // начальная загрузка (Mock/реальный сервис)
        var symbols = new[] { "AAPL", "MSFT", "GOOG" };
        foreach (var s in symbols)
        {
            var st = await _service.GetStockAsync(s) ?? new Stock { Symbol = s, Price = 0m };
            MainThread.BeginInvokeOnMainThread(() => Stocks.Add(st));
        }
    }

    private async Task RefreshCommandExecute()
    {
        if (IsRefreshing) return;

        IsRefreshing = true;
        try
        {
            Console.WriteLine("[VM] RefreshCommandExecute START");
            await RefreshStocksAsync();
            Console.WriteLine("[VM] RefreshCommandExecute DONE");
        }
        catch (Exception ex)
        {
            // важно логировать — исключения часто ломают flow
            Console.WriteLine("[VM] Refresh EX: " + ex);
        }
        finally
        {
            // ВСЕГДА сбрасываем флаг в finally — это решает "бесконечную загрузку"
            IsRefreshing = false;
            Console.WriteLine("[VM] IsRefreshing set to false");
        }
    }

    private async Task RefreshStocksAsync()
    {
        // обновляем последовательно, чтобы не получить rate limit / не заблокировать UI
        foreach (var stock in Stocks)
        {
            try
            {
                var updated = await _service.GetStockAsync(stock.Symbol);
                if (updated != null)
                {
                    // обновление на UI-потоке
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

            // небольшая задержка между запросами — уменьшает риск 429
            await Task.Delay(300);
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