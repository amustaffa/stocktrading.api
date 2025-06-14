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

    public async Task<TradeDto> PlaceTradeAsync(string userId, CreateTradeDto createTradeDto)
    {
        _logger.LogInformation("Attempting to place trade for User {UserId}: StockId={StockId}, Type={TradeType}, Quantity={Quantity}",
            userId, createTradeDto.StockId, createTradeDto.Type, createTradeDto.Quantity);

        var stock = await _stockRepository.GetByIdAsync(createTradeDto.StockId);
        if (stock == null)
        {
            _logger.LogWarning("Trade failed: Stock with ID {StockId} not found.", createTradeDto.StockId);
            throw new ApplicationException($"Stock with ID {createTradeDto.StockId} not found.");
        }

        var trade = new Trade
        {
            UserId = userId,
            StockId = createTradeDto.StockId,
            Type = createTradeDto.Type,
            Quantity = createTradeDto.Quantity,
            Price = stock.CurrentPrice, // Record the price at the time of trade
            TradeDate = DateTime.UtcNow,
            Stock = stock // Attach the stock object for relationship
        };

        _tradeRepository.Add(trade);
        await _tradeRepository.SaveChangesAsync();

        // Update user's portfolio
        await _portfolioService.UpdatePortfolioAsync(userId, trade);

        _logger.LogInformation("Trade {TradeId} placed successfully for User {UserId}.", trade.Id, userId);

        return new TradeDto
        {
            Id = trade.Id,
            UserId = trade.UserId,
            StockId = trade.StockId,
            StockSymbol = stock.Symbol,
            Type = trade.Type,
            Quantity = trade.Quantity,
            Price = trade.Price,
            TradeDate = trade.TradeDate
        };
    }

    public async Task<IEnumerable<TradeDto>> GetUserTradesAsync(string userId)
    {
        _logger.LogInformation("Fetching trades for User {UserId}.", userId);
        var trades = await _tradeRepository.GetUserTradesAsync(userId); // This will include Stock

        return trades
            .Select(t => new TradeDto
            {
                Id = t.Id,
                UserId = t.UserId,
                StockId = t.StockId,
                StockSymbol = t.Stock?.Symbol ?? "N/A",
                Type = t.Type,
                Quantity = t.Quantity,
                Price = t.Price,
                TradeDate = t.TradeDate
            })
            .ToList();
    }
}