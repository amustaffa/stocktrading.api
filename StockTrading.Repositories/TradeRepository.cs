using Microsoft.EntityFrameworkCore;
using StockTrading.Data;
using StockTrading.Models.Domain;
using StockTrading.Repository.Interfaces;

namespace StockTrading.Repositories
{
    public class TradeRepository : Repository<Trade>, ITradeRepository
    {
        public TradeRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Trade>> GetUserTradesAsync(string userId)
        {
            // Stock is now in the same DbContext, so Include is straightforward.
            return await _dbSet
                .Where(t => t.UserId == userId)
                .Include(t => t.Stock) // Include stock details
                .OrderByDescending(t => t.TradeDate)
                .ToListAsync();
        }
    }
}