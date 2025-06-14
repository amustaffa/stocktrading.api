using StockTrading.Models.Domain;
using Microsoft.Extensions.Logging;
using StockTrading.Repository.Interfaces;
using StockTrading.Models.DTO;
using StockTrading.Service.Interfaces;

namespace StockTrading.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IPortfolioItemRepository _portfolioItemRepository;
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(IPortfolioRepository portfolioRepository, IPortfolioItemRepository portfolioItemRepository, IStockRepository stockRepository, ILogger<PortfolioService> logger)
    {
        _portfolioRepository = portfolioRepository;
        _portfolioItemRepository = portfolioItemRepository;
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<PortfolioDto?> GetUserPortfolioAsync(string userId)
    {
        _logger.LogInformation("Fetching portfolio for user: {UserId}", userId);

        var portfolio = await _portfolioRepository.GetUserPortfolioWithItemsAsync(userId);

        if (portfolio == null)
        {
            // Create a default portfolio if one doesn't exist
            _logger.LogInformation("No portfolio found for user {UserId}. Creating a new default portfolio.", userId);
            portfolio = new Portfolio
            {
                UserId = userId,
                Name = "My Main Portfolio",
                CreatedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            _portfolioRepository.Add(portfolio);
            await _portfolioRepository.SaveChangesAsync();
            // After saving, reload to get the newly created portfolio with its ID and its items
            portfolio = (await _portfolioRepository.GetUserPortfolioWithItemsAsync(userId))!;
        }

        var portfolioDto = new PortfolioDto
        {
            Id = portfolio.Id,
            UserId = portfolio.UserId,
            Name = portfolio.Name,
            CreatedDate = portfolio.CreatedDate,
            LastUpdated = portfolio.LastUpdated,
            Items = new List<PortfolioItemDto>()
        };

        foreach (var item in portfolio.Items)
        {
            portfolioDto.Items.Add(new PortfolioItemDto
            {
                Id = item.Id,
                StockId = item.StockId,
                StockSymbol = item.Stock?.Symbol ?? "N/A",
                CompanyName = item.Stock?.CompanyName ?? "Unknown Company",
                Quantity = item.Quantity,
                AverageCost = item.AverageCost,
                CurrentMarketValue = item.Quantity * (item.Stock?.CurrentPrice ?? 0m),
                ProfitLoss = (item.Quantity * (item.Stock?.CurrentPrice ?? 0m)) - (item.Quantity * item.AverageCost)
            });
        }

        return portfolioDto;
    }


    public async Task UpdatePortfolioAsync(string userId, Trade trade)
    {
        _logger.LogInformation("Updating portfolio for user {UserId} based on trade {TradeId}", userId, trade.Id);

        var portfolio = await _portfolioRepository.GetUserPortfolioWithItemsAsync(userId);

        if (portfolio == null)
        {
            portfolio = new Portfolio
            {
                UserId = userId,
                Name = "My Main Portfolio",
                CreatedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            _portfolioRepository.Add(portfolio);
            await _portfolioRepository.SaveChangesAsync();
            portfolio = (await _portfolioRepository.GetUserPortfolioWithItemsAsync(userId))!;
        }

        var existingItem = portfolio.Items.FirstOrDefault(pi => pi.StockId == trade.StockId);

        if (trade.Type == TradeType.Buy)
        {
            if (existingItem == null)
            {
                var newItem = new PortfolioItem
                {
                    StockId = trade.StockId,
                    Quantity = trade.Quantity,
                    AverageCost = trade.Price,
                    PortfolioId = portfolio.Id,
                    Stock = trade.Stock // Directly use the Stock from the trade, it's now tracked by the same context
                };
                _portfolioItemRepository.Add(newItem);
                _logger.LogInformation("Added new stock {StockSymbol} ({Quantity}) to portfolio for user {UserId}.", trade.Stock?.Symbol, trade.Quantity, userId);
            }
            else
            {
                decimal totalCostBefore = existingItem.Quantity * existingItem.AverageCost;
                decimal totalCostAfter = totalCostBefore + (trade.Quantity * trade.Price);
                int newTotalQuantity = existingItem.Quantity + trade.Quantity;

                existingItem.AverageCost = totalCostAfter / newTotalQuantity;
                existingItem.Quantity = newTotalQuantity;
                _portfolioItemRepository.Update(existingItem);
                _logger.LogInformation("Updated stock {StockSymbol} ({Quantity}) in portfolio for user {UserId}. New Avg Cost: {AvgCost}", trade.Stock?.Symbol, existingItem.Quantity, userId, existingItem.AverageCost);
            }
        }
        else if (trade.Type == TradeType.Sell)
        {
            if (existingItem == null || existingItem.Quantity < trade.Quantity)
            {
                _logger.LogWarning("Sell trade failed: Insufficient stock {StockSymbol} quantity for user {UserId}. Requested: {Requested}, Available: {Available}", trade.Stock?.Symbol, userId, trade.Quantity, existingItem?.Quantity ?? 0);
                throw new ApplicationException("Insufficient stock quantity to sell.");
            }
            else
            {
                existingItem.Quantity -= trade.Quantity;
                if (existingItem.Quantity == 0)
                {
                    _portfolioItemRepository.Remove(existingItem);
                    _logger.LogInformation("Removed stock {StockSymbol} from portfolio for user {UserId} (quantity reached 0).", trade.Stock?.Symbol, userId);
                }
                else
                {
                    _portfolioItemRepository.Update(existingItem);
                }
                _logger.LogInformation("Sold {SoldQuantity} of stock {StockSymbol} from portfolio for user {UserId}. Remaining: {RemainingQuantity}", trade.Quantity, trade.Stock?.Symbol, userId, existingItem.Quantity);
            }
        }

        portfolio.LastUpdated = DateTime.UtcNow;
        _portfolioRepository.Update(portfolio);
        await _portfolioRepository.SaveChangesAsync();
        _logger.LogInformation("Portfolio for user {UserId} updated successfully.", userId);
    }
}
