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

		var apiKey = "ключ";

		StockService service;
		if (string.IsNullOrWhiteSpace(apiKey))
			service = new MockStockService();
		else
			service = new StockService(apiKey);

		BindingContext = new MainViewModel(service);
	}

	private async void SearchResultTapped(object? sender, EventArgs e)
	{
		if (sender is Frame frame && frame.BindingContext is SearchResult result)
		{
			if (BindingContext is MainViewModel vm)
			{
				await vm.AddTickerAsync(result);
			}
		}
	}
}