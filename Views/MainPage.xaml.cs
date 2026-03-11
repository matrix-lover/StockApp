using System.Linq;
using StockApp.Models;
using StockApp.Services;
using StockApp.ViewModels;

namespace StockApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        var apiKey = "key";;
        StockService service = string.IsNullOrWhiteSpace(apiKey) ? new MockStockService() : new StockService(apiKey);

        BindingContext = new MainViewModel(service);
    }
}