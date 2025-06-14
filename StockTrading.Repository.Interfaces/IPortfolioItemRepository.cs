using StockTrading.Models.Domain;

namespace StockTrading.Repository.Interfaces
{
    public interface IPortfolioItemRepository : IRepository<PortfolioItem>
    {
        Task<PortfolioItem?> GetByPortfolioAndStockAsync(int portfolioId, int stockId);
    }
}