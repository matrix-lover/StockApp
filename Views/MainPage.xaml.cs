using StockApp.Services;
using StockApp.ViewModels;

namespace StockApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        // тестируем сначала с MockStockService — чтобы исключить проблемы сети/ключа
        var service = new MockStockService();
        BindingContext = new MainViewModel(service);
    }
}