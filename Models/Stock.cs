using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockApp.Models;

public class Stock : INotifyPropertyChanged
{
    private decimal _price;
    private decimal _previousPrice;

    public string Symbol { get; set; } = "";
    public string Currency { get; set; } = "USD";

    public decimal Price
    {
        get => _price;
        set
        {
            if (_price != value)
            {
                _previousPrice = _price;
                _price = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Color));
                OnPropertyChanged(nameof(DisplayPrice)); // обновляем отображаемую цену
            }
        }
    }

    // цвет
    public string Color
    {
        get
        {
            if (_previousPrice == 0 || _price == _previousPrice)
                return "White";
            return _price > _previousPrice ? "Green" : "Red";
        }
    }

    public string Arrow
    {
        get
        {
            if (_previousPrice == 0 || _price == _previousPrice)
                return "→";
            return _price > _previousPrice ? "↑" : "↓";
        }
    }

    // цена и валюта
    public string DisplayPrice => $"{Price:F2} {Currency}";

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}