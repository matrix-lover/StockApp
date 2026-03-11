using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Storage;    
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using StockApp.Models;
using StockApp.Services;

namespace StockApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public ICommand RemoveStockCommand { get; }
    public ICommand AddTickerCommand { get; }
    private readonly StockService _service;
    private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
    public ObservableCollection<Stock> Stocks { get; } = new();
    public ObservableCollection<SearchResult> SearchResults { get; } = new();

    private readonly StockStorageService storage = new();

    private string _searchText;
    public string SearchText
    {
        get => _searchText;
        set
        {
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

    public MainViewModel(StockService? service = null)
    {
        // команда удаления должна быть инициализирована до любых вызовов UI
        RemoveStockCommand = new Command<Stock>(RemoveStock);

        // загрузка ранее сохранённого списка (единственный источник правды)
        var savedStocks = storage.LoadStocks();
        foreach (var stock in savedStocks)
            Stocks.Add(stock);

        _service = service ?? new StockService("");
        RefreshCommand = new Command(async () => await RefreshCommandExecute());

        AddTickerCommand = new Command<SearchResult>(async result => await AddTickerAsync(result));

        // Инициализация дефолтных тикеров только если список пуст (чтобы не дублировать)
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (Stocks.Any()) // если уже есть сохранённые — не добавляем дефолтные
            return;

        var symbols = new[] { "AAPL", "MSFT", "GOOG", "AMZN", "TSLA" };
        foreach (var s in symbols)
        {
            try
            {
                var st = await _service.GetStockAsync(s) ?? new Stock { Symbol = s, Price = 0m };
                MainThread.BeginInvokeOnMainThread(() => Stocks.Add(st));
            }
            catch { /* игнорируем ошибки при старте */ }

            await Task.Delay(200);
        }

        // сохраним стартовый список (если он был пуст и мы добавили дефолтные)
        storage.SaveStocks(Stocks.ToList());

        _ = StartAutoRefresh();
    }

    private async Task RefreshCommandExecute()
    {
        if (!await _refreshLock.WaitAsync(0)) return;
        try
        {
            IsRefreshing = true;
            await RefreshStocksAsync();
        }
        finally
        {
            IsRefreshing = false;
            _refreshLock.Release();
        }
    }

    private async Task RefreshStocksAsync()
    {
        foreach (var stock in Stocks.ToList()) // to avoid collection-modify issues
        {
            try
            {
                var updated = await _service.GetStockAsync(stock.Symbol);
                if (updated != null)
                    MainThread.BeginInvokeOnMainThread(() => stock.Price = updated.Price);
            }
            catch { }
            await Task.Delay(200);
        }
    }

    private async Task StartAutoRefresh()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            if (!await _refreshLock.WaitAsync(0)) continue;
            try { await RefreshStocksAsync(); } finally { _refreshLock.Release(); }
        }
    }

    private async void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            IsSearching = false;
            SearchResults.Clear();
            return;
        }

        IsSearching = true;
        var results = await _service.SearchStocksAsync(SearchText);

        SearchResults.Clear();

        var filtered = results.Where(r => r.Type != null &&
            r.Type.Contains("Common", StringComparison.OrdinalIgnoreCase)).Where(r => !r.Symbol.Contains('.')).Take(10);

        foreach (var r in filtered)
        {
            SearchResults.Add(r);
        }
    }

    public async Task AddTickerAsync(SearchResult result)
    {
        if (result == null) return;
        if (Stocks.Any(s => s.Symbol == result.Symbol)) return;

        var stock = await _service.GetStockAsync(result.Symbol);

        if (stock != null)
            {
                MainThread.BeginInvokeOnMainThread(() => Stocks.Add(stock));
                // сохраняем список сразу после добавления — единый источник правды
                storage.SaveStocks(Stocks.ToList());
            }

        // очищаем поиск после добавления
        SearchText = string.Empty;
        SearchResults.Clear();
        IsSearching = false;
    }

    public void RemoveStock(Stock stock)
    {
        if (stock == null) return;

        if (Stocks.Contains(stock))
        {
            // удалить из коллекции (единственный массив)
            Stocks.Remove(stock);

            try
            {
                // удалить сохранённую последнюю цену для этой акции, если вы её сохраняли отдельно
                Preferences.Remove($"stock_{stock.Symbol}_last");
            }
            catch { /* игнорируем ошибки Preferences */ }

            // пересохранить список в хранилище
            storage.SaveStocks(Stocks.ToList());
        }
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