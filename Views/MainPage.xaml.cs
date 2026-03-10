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

		var apiKey = "key";
		StockService service = string.IsNullOrWhiteSpace(apiKey) ? new MockStockService() : new StockService(apiKey);

		BindingContext = new MainViewModel(service);
	}

	private async void SearchResultTapped(object sender, EventArgs e)
	{
		// sender — ContentView; BindingContext у него — SearchResult
		if (sender is ContentView cv && cv.BindingContext is SearchResult result)
		{
			// debug: временно можно раскомментировать, чтобы убедиться, что tap ловится
			await DisplayAlert("Tapped", $"You tapped {result.DisplaySymbol}", "OK");

			if (BindingContext is MainViewModel vm)
			{
				// вызываем метод VM — он добавит тикер и загрузит цену
				await vm.AddTickerAsync(result);
			}

			// Сброс UI-стана поиска: очистим поле и результаты
			if (BindingContext is MainViewModel vm2)
			{
				vm2.SearchText = string.Empty;
				vm2.SearchResults.Clear();
				vm2.IsSearching = false;
			}
		}
	}

	private async void OnDeleteStock(object sender, EventArgs e)
	{
		var swipeItem = sender as SwipeItem;
		var stock = swipeItem?.BindingContext as Stock;

		if (stock == null)
			return;

		bool confirm = await DisplayAlert(
			"Delete stock",
			$"Delete {stock.Symbol} from list?",
			"Yes",
			"No");

		if (confirm)
		{
			var vm = BindingContext as MainViewModel;
			vm?.Stocks.Remove(stock);
		}
	}
}