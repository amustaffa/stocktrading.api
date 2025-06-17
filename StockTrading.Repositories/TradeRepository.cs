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
            return await _dbSet
                .Where(t => t.UserId == userId)
                .Include(t => t.Stock)
                .OrderByDescending(t => t.TradeDate)
                .ToListAsync();
        }
    }
}