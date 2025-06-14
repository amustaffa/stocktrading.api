using StockTrading.Models.Domain;

namespace StockTrading.Repository.Interfaces;

public interface ITradeRepository : IRepository<Trade>
{
    Task<IEnumerable<Trade>> GetUserTradesAsync(string userId);
}
