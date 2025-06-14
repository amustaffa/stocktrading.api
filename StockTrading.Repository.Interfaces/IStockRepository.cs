using StockTrading.Models.Domain;

namespace StockTrading.Repository.Interfaces;

public interface IStockRepository : IRepository<Stock>
{
    Task<Stock?> GetBySymbolAsync(string symbol);
    // Add any stock-specific methods here if needed, e.g., GetTopPerformingStocks
}