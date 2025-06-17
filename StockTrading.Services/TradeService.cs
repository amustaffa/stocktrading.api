using StockTrading.Models.Domain;
using Microsoft.Extensions.Logging;
using StockTrading.Repository.Interfaces;
using StockTrading.Models.DTO;
using StockTrading.Service.Interfaces;

namespace StockTrading.Services;

public class TradeService : ITradeService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TradeService> _logger;
    public TradeService(
        IUnitOfWork unitOfWork,
        ILogger<TradeService> logger)
    {
        _uow = unitOfWork;
        _logger = logger;
    }
    public async Task<TradeDto> PlaceTradeAsync(string userId, TradeOrderDto order)
    {
        using var transaction = await _uow.BeginTransactionAsync();
        try
        {
            var stock = await _uow.Stocks.GetBySymbolAsync(order.Symbol);
            if (stock == null)
            {
                _logger.LogWarning("Trade failed: Stock with symbol {Symbol} not found.", order.Symbol);
                throw new ApplicationException($"Stock with symbol {order.Symbol} not found.");
            }

            var trade = new Trade
            {
                UserId = userId,
                StockId = stock.Id,
                Type = order.Type,
                Quantity = order.Quantity,
                Price = stock.CurrentPrice, // Record the price at the time of trade
                TradeDate = DateTime.UtcNow,
                Stock = stock // Attach the stock object for relationship
            };

            // Validate trade quantity
            if (trade.Quantity <= 0)
            {
                _logger.LogWarning("Trade failed: Invalid quantity {Quantity} for trade type {TradeType}.", trade.Quantity, trade.Type);
                throw new ApplicationException($"Invalid quantity {trade.Quantity} for trade type {trade.Type}.");
            }

            if (trade.Type == TradeType.Sell)
            {
                var hasEnoughQuantityToSell = await ValidateTradeQuantityAsync(userId, trade);
                if (!hasEnoughQuantityToSell)
                {
                    _logger.LogWarning("Trade failed: Not enough stock quantity to sell for User {UserId}.", userId);
                    throw new ApplicationException("Not enough stock quantity to sell.");
                }
            }
            _logger.LogInformation("Placing trade: {TradeType} {Quantity} of {StockSymbol} for User {UserId}.",
                trade.Type, trade.Quantity, stock.Symbol, userId);
            // May call a third party service here to execute the trade
            // For simplicity, will just save the trade to the repository

            _uow.Trades.Add(trade);
            _logger.LogInformation("Trade placed successfully: {TradeId} for User {UserId}.", trade.Id, userId);

            // Update the portfolio based on the trade
            // Would only update the portfolio if the trade is successful
            await UpdatePortfolioAsync(userId, trade);

            await _uow.SaveChangesAsync();
            await transaction.CommitAsync();
            return MapToDto(trade);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error placing trade for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<TradeDto>> GetUserTradesAsync(string userId)
    {
        _logger.LogInformation("Fetching trades for User {UserId}.", userId);
        var trades = await _uow.Trades.GetUserTradesAsync(userId); // This will include Stock

        return trades
                  .Select(t => MapToDto(t))
                  .ToList();
    }

    private static TradeDto MapToDto(Trade trade)
    {
        return new TradeDto
        {
            Id = trade.Id,
            UserId = trade.UserId,
            StockId = trade.StockId,
            Symbol = trade.Stock?.Symbol ?? "N/A",
            Type = trade.Type,
            Quantity = trade.Quantity,
            Price = trade.Price,
            TradeDate = trade.TradeDate
        };
    }

    public async Task UpdatePortfolioAsync(string userId, Trade trade)
    {
        _logger.LogInformation("Updating portfolio for user {UserId} based on trade {TradeId}", userId, trade.Id);

        var portfolio = await _uow.Portfolios.GetUserPortfolioWithItemsAsync(userId);

        if (portfolio == null)
        {
            portfolio = new Portfolio
            {
                UserId = userId,
                Name = "My Main Portfolio",
                CreatedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            _uow.Portfolios.Add(portfolio);
            await _uow.Portfolios.SaveChangesAsync();
            portfolio = (await _uow.Portfolios.GetUserPortfolioWithItemsAsync(userId))!;
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
                _uow.PortfolioItems.Add(newItem);
                _logger.LogInformation("Added new stock {StockSymbol} ({Quantity}) to portfolio for user {UserId}.", trade.Stock?.Symbol, trade.Quantity, userId);
            }
            else
            {
                decimal totalCostBefore = existingItem.Quantity * existingItem.AverageCost;
                decimal totalCostAfter = totalCostBefore + (trade.Quantity * trade.Price);
                int newTotalQuantity = existingItem.Quantity + trade.Quantity;

                existingItem.AverageCost = totalCostAfter / newTotalQuantity;
                existingItem.Quantity = newTotalQuantity;
                _uow.PortfolioItems.Update(existingItem);
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
                    _uow.PortfolioItems.Remove(existingItem);
                    _logger.LogInformation("Removed stock {StockSymbol} from portfolio for user {UserId} (quantity reached 0).", trade.Stock?.Symbol, userId);
                }
                else
                {
                    _uow.PortfolioItems.Update(existingItem);
                }
                _logger.LogInformation("Sold {SoldQuantity} of stock {StockSymbol} from portfolio for user {UserId}. Remaining: {RemainingQuantity}", trade.Quantity, trade.Stock?.Symbol, userId, existingItem.Quantity);
            }
        }

        portfolio.LastUpdated = DateTime.UtcNow;
        _uow.Portfolios.Update(portfolio);        
        _logger.LogInformation("Portfolio for user {UserId} updated successfully.", userId);
    }

    public async Task<bool> ValidateTradeQuantityAsync(string userId, Trade trade)
    {
        _logger.LogInformation("Validating trade quantity for user {UserId}, {Symbol}", userId, trade.Stock?.Symbol);

        var portfolio = await _uow.Portfolios.GetUserPortfolioWithItemsAsync(userId);
        if (portfolio == null)
        {
            _logger.LogWarning("Validation failed: No portfolio found for user {UserId}.", userId);
            return false;
        }

        var item = portfolio.Items.FirstOrDefault(pi => pi.StockId == trade.StockId);
        if (trade.Type == TradeType.Sell && (item == null || item.Quantity < trade.Quantity))
        {
            _logger.LogWarning("Validation failed: Insufficient stock quantity for user {UserId}. Requested: {Requested}, Available: {Available}", userId, trade.Quantity, item?.Quantity ?? 0);
            return false;
        }

        _logger.LogInformation("Trade quantity validation successful for user {UserId}.", userId);
        return true;
    }
}