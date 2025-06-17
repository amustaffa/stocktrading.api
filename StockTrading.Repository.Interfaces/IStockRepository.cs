using StockTrading.Models.Domain;

namespace StockTrading.Repository.Interfaces;

public interface IStockRepository : IRepository<Stock>
{
    Task<Stock?> GetBySymbolAsync(string symbol);
}