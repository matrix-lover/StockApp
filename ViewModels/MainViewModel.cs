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
using System.Windows.Input;

namespace StockApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public ICommand AddTickerCommand { get; }
    private readonly StockService _service;
    private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

    public ObservableCollection<Stock> Stocks { get; } = new();
    public ObservableCollection<SearchResult> SearchResults { get; } = new();

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
        _service = service ?? new StockService("");
        RefreshCommand = new Command(async () => await RefreshCommandExecute());

        AddTickerCommand = new Command<SearchResult>(async result => await AddTickerAsync(result));

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var symbols = new[] { "AAPL","MSFT","GOOG","AMZN","TSLA" };
        foreach (var s in symbols)
        {
            var st = await _service.GetStockAsync(s) ?? new Stock { Symbol = s, Price = 0m };
            MainThread.BeginInvokeOnMainThread(() => Stocks.Add(st));
        }

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
        foreach (var stock in Stocks)
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
            await Task.Delay(TimeSpan.FromSeconds(60));
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
        foreach (var r in results.Take(10))
            SearchResults.Add(r);
    }

    public async Task AddTickerAsync(SearchResult result)
    {
        if (Stocks.Any(s => s.Symbol == result.Symbol)) return;

        var stock = await _service.GetStockAsync(result.Symbol);
        if (stock != null)
        {
            MainThread.BeginInvokeOnMainThread(() => Stocks.Add(stock));
        }

        // очищаем поиск после добавления
        SearchText = string.Empty;
        SearchResults.Clear();
        IsSearching = false;
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