using Microsoft.EntityFrameworkCore;
using StockTrading.Data;
using StockTrading.Models.Domain;
using StockTrading.Repository.Interfaces;

namespace StockTrading.Repositories
{
    public class StockRepository : Repository<Stock>, IStockRepository
    {
        public StockRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Stock?> GetBySymbolAsync(string symbol)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.Symbol == symbol);
        }
    }
}