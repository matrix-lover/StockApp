using StockApp.Services;
using StockApp.ViewModels;

namespace StockApp;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		var apiKey = "ключ";

		var service = new StockService(apiKey);
        BindingContext = new MainViewModel(service);
    }
}