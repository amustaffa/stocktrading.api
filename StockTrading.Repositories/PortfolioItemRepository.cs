using Microsoft.EntityFrameworkCore;
using StockTrading.Data;
using StockTrading.Models.Domain;
using StockTrading.Repository.Interfaces;

namespace StockTrading.Repositories
{
    public class PortfolioItemRepository : Repository<PortfolioItem>, IPortfolioItemRepository
    {
        public PortfolioItemRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PortfolioItem?> GetByPortfolioAndStockAsync(int portfolioId, int stockId)
        {
            // Stock is now in the same DbContext, so Include is straightforward.
            return await _dbSet
                .Include(pi => pi.Stock)
                .FirstOrDefaultAsync(pi => pi.PortfolioId == portfolioId && pi.StockId == stockId);
        }
    }
}
