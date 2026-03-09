namespace StockApp.Services;
using System.Threading.Tasks;
using StockApp.Models;

public interface IStockService
{
    Task<Stock?> GetStockAsync(string symbol);
}