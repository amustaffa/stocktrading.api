using Microsoft.EntityFrameworkCore.Storage;
using StockTrading.Data;
using StockTrading.Repository.Interfaces;

namespace StockTrading.Repositories
{
    public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        private readonly ApplicationDbContext _context = context;
        private ITradeRepository? _trades;
        private IStockRepository? _stocks;
        private IPortfolioRepository? _portfolios;
        private IPortfolioItemRepository? _portfolioItems;

        public ITradeRepository Trades =>
            _trades ??= new TradeRepository(_context);

        public IStockRepository Stocks => 
            _stocks ??= new StockRepository(_context);

        public IPortfolioRepository Portfolios =>   
            _portfolios ??= new PortfolioRepository(_context);

        public IPortfolioItemRepository PortfolioItems => 
            _portfolioItems ??= new PortfolioItemRepository(_context);

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}