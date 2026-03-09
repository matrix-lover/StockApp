using StockApp.Services;
using StockApp.ViewModels;

namespace StockApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel();
    }
}