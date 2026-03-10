using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.Maui.Storage;

namespace StockApp.Models;

public class Stock : INotifyPropertyChanged
{
    // backing fields
    private decimal _price;
    private decimal _previousPrice;
    private string _symbol = string.Empty;

    // public API
    public string Symbol
    {
        get => _symbol;
        set
        {
            if (_symbol == value) return;
            _symbol = value ?? string.Empty;
            // Load previously saved last price for this symbol (if any)
            LoadPreviousPriceFromPreferences();
            OnPropertyChanged();
            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(Arrow));
            OnPropertyChanged(nameof(DisplayPrice));
        }
    }

    public string Currency { get; set; } = "USD";

    // map JSON "c" -> Price
    [JsonPropertyName("c")]
    public decimal Price
    {
        get => _price;
        set
        {
            if (_price == value) return;

            _previousPrice = _price;

            _price = value;

            // persist the newly received price as the "last" price for future app runs
            SaveCurrentPriceToPreferences();

            // notify bindings
            OnPropertyChanged();
            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(Arrow));
            OnPropertyChanged(nameof(DisplayPrice));
        }
    }

    // computed UI properties
    public string Color
    {
        get
        {
            if (_previousPrice == 0 || _price == _previousPrice) return "White";
            return _price > _previousPrice ? "Green" : "Red";
        }
    }

    public string Arrow
    {
        get
        {
            if (_previousPrice == 0 || _price == _previousPrice) return "→";
            return _price > _previousPrice ? "↑" : "↓";
        }
    }

    public string DisplayPrice => $"{Price:F2} {Currency}";

    // INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // --- Preferences persistence helpers ---

    // preferences key: "stock_{SYMBOL}_last"
    string PrefKeyForSymbol()
    {
        if (string.IsNullOrWhiteSpace(_symbol)) return string.Empty;
        return $"stock_{_symbol}_last";
    }

    void LoadPreviousPriceFromPreferences()
    {
        try
        {
            var key = PrefKeyForSymbol();
            if (string.IsNullOrEmpty(key))
            {
                _previousPrice = 0m;
                return;
            }

            // Preferences supports generic Get; store as double to be safe (Preferences uses primitives)
            var saved = Preferences.Get(key, 0d);
            _previousPrice = (decimal)saved;
        }
        catch
        {
            // on error, keep previousPrice = 0
            _previousPrice = 0m;
        }
    }

    void SaveCurrentPriceToPreferences()
    {
        try
        {
            var key = PrefKeyForSymbol();
            if (string.IsNullOrEmpty(key)) return;

            // store as double (Preferences accepts primitive types)
            Preferences.Set(key, (double)_price);
        }
        catch
        {
            // ignore persistence errors for now
        }
    }
}