using StockTrading.Models.DTO;
using StockTrading.Models.Domain;
namespace StockTrading.Service.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioDto?> GetUserPortfolioAsync(string userId);
    Task UpdatePortfolioAsync(string userId, Trade trade);
}