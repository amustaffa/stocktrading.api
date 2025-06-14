using System.Collections.Generic;
using System.Threading.Tasks;
using StockTrading.Models.DTO;

namespace StockTrading.Service.Interfaces
{
    public interface IStockService
    {
        Task<IEnumerable<StockDto>> GetAllStocksAsync();
        Task<StockDto?> GetStockBySymbolAsync(string symbol);
        Task<StockDto?> GetStockByIdAsync(int id);
        Task UpdateStockPriceAsync(int stockId, decimal newPrice);
    }
}
