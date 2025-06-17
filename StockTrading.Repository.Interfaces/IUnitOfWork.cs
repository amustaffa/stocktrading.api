using Microsoft.EntityFrameworkCore.Storage;

namespace StockTrading.Repository.Interfaces
{
    public interface IUnitOfWork
    {
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<int> SaveChangesAsync();
        ITradeRepository Trades { get; }
        IStockRepository Stocks { get; }
        IPortfolioRepository Portfolios { get; }
        IPortfolioItemRepository PortfolioItems { get; } 
    }
}