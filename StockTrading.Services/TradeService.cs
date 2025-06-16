using StockTrading.Models.Domain;
using Microsoft.Extensions.Logging;
using StockTrading.Repository.Interfaces;
using StockTrading.Models.DTO;
using StockTrading.Service.Interfaces;

namespace StockTrading.Services;

public class TradeService : ITradeService
{
    private readonly ITradeRepository _tradeRepository;
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<TradeService> _logger;
    private readonly IPortfolioService _portfolioService;

    public TradeService(ITradeRepository tradeRepository, IStockRepository stockRepository, ILogger<TradeService> logger, IPortfolioService portfolioService)
    {
        _tradeRepository = tradeRepository;
        _stockRepository = stockRepository;
        _logger = logger;
        _portfolioService = portfolioService;
    }

    public async Task<TradeDto> PlaceTradeAsync(string userId, TradeOrderDto order)
    {
        //using var transaction = await _tradeRepository.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Attempting to place trade for User {UserId}: StockId={StockId}, Type={TradeType}, Quantity={Quantity}",
                userId, order.Symbol, order.Type, order.Quantity);

            var stock = await _stockRepository.GetBySymbolAsync(order.Symbol);
            if (stock == null)
            {
                _logger.LogWarning("Trade failed: Stock with ID {symbol} not found.", order.Symbol);
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
                var hasEnoughQuantityToSell = await _portfolioService.ValidateTradeQuantityAsync(userId, trade);
                if (!hasEnoughQuantityToSell)
                {
                    _logger.LogWarning("Trade failed: Not enough stock quantity to sell for User {UserId}.", userId);
                    throw new ApplicationException("Not enough stock quantity to sell.");
                }
            }
            _logger.LogInformation("Placing trade: {TradeType} {Quantity} of {StockSymbol} for User {UserId}.",
                trade.Type, trade.Quantity, stock.Symbol, userId);
            // May call a third party service here to execute the trade
            // For simplicity, we will just save the trade to the repository

            _tradeRepository.Add(trade);
            await _tradeRepository.SaveChangesAsync();
            await _portfolioService.UpdatePortfolioAsync(userId, trade);
            
            //await transaction.CommitAsync();
            return MapToDto(trade);
        }
        catch (Exception ex)
        {
            //await transaction.RollbackAsync();
            _logger.LogError(ex, "Error placing trade for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<TradeDto>> GetUserTradesAsync(string userId)
    {
        _logger.LogInformation("Fetching trades for User {UserId}.", userId);
        var trades = await _tradeRepository.GetUserTradesAsync(userId); // This will include Stock

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
}