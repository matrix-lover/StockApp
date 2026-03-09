using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    public ObservableCollection<SearchResult> SearchResults { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            Search();
        }
    }

    private bool _isSearching;
    public bool IsSearching
    {
        get => _isSearching;
        set => SetProperty(ref _isSearching, value);
    }

    public ICommand RefreshCommand { get; }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    // Конструктор: можно передать реальный StockService или оставить пустым для Mock
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

    // Поиск (вызывается при изменении SearchText)
    private CancellationTokenSource? _searchCts;
    private async void Search()
    {
        // отмена предыдущего незавершённого поиска (debounce-ish)
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        var token = _searchCts.Token;

        // небольшая задержка, чтобы не гонять API на каждое нажатие
        try
        {
            await Task.Delay(300, token);
        }
        catch (TaskCanceledException) { return; }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults.Clear();
            IsSearching = false;
            return;
        }

        IsSearching = true;

        try
        {
            var results = await _service.SearchStocksAsync(SearchText);

            if (token.IsCancellationRequested) return;

            SearchResults.Clear();
            foreach (var r in results.Take(10))
                SearchResults.Add(r);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[VM] Search EX: " + ex.Message);
            SearchResults.Clear();
        }
    }

    // Добавить тикер в список (по клику на результат поиска)
    public async Task AddTickerAsync(SearchResult result)
    {
        if (result == null) return;

        // Избегаем дубликатов (без учета регистра)
        if (Stocks.Any(s => string.Equals(s.Symbol, result.Symbol, StringComparison.OrdinalIgnoreCase)))
            return;

        var newStock = new Stock { Symbol = result.Symbol, Price = 0m };
        MainThread.BeginInvokeOnMainThread(() => Stocks.Add(newStock));

        try
        {
            var updated = await _service.GetStockAsync(result.Symbol);
            if (updated != null)
            {
                MainThread.BeginInvokeOnMainThread(() => newStock.Price = updated.Price);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[VM] AddTickerAsync EX: " + ex.Message);
        }

        // Сбрасываем поиск
        SearchText = string.Empty;
        SearchResults.Clear();
        IsSearching = false;
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