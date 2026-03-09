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

		// Поставьте свой ключ сюда для реального Finnhub, либо оставьте пустым ("")
		// чтобы использовать MockStockService.
		var apiKey = ""; // <-- Вставьте ваш FINNHUB API KEY, если хотите реальный API

		StockService service;
		if (string.IsNullOrWhiteSpace(apiKey))
			service = new MockStockService();
		else
			service = new StockService(apiKey);

		BindingContext = new MainViewModel(service);
	}

	private async void SearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is SearchResult selected)
		{
			if (BindingContext is MainViewModel vm)
			{
				await vm.AddTickerAsync(selected);
			}

			// Сбрасываем selection чтобы можно было выбирать тот же элемент снова
			if (sender is CollectionView cv)
				cv.SelectedItem = null;
		}
	}
}