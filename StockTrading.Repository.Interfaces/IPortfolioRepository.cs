using StockTrading.Models.Domain;

namespace StockTrading.Repository.Interfaces
{
    public interface IPortfolioRepository : IRepository<Portfolio>
    {
        Task<Portfolio?> GetUserPortfolioWithItemsAsync(string userId);
    }
}