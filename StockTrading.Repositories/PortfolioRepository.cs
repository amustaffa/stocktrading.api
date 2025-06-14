using Microsoft.EntityFrameworkCore;
using StockTrading.Data;
using StockTrading.Models.Domain;
using StockTrading.Repository.Interfaces;

namespace StockTrading.Repositories
{
    public class PortfolioRepository : Repository<Portfolio>, IPortfolioRepository
    {
        public PortfolioRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Portfolio?> GetUserPortfolioWithItemsAsync(string userId)
        {
            // Stock is now in the same DbContext, so Include is straightforward.
            return await _dbSet
                .Include(p => p.Items)
                    .ThenInclude(pi => pi.Stock)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }
    }
}