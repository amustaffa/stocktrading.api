using Microsoft.Extensions.Logging;
using StockTrading.Repository.Interfaces;
using StockTrading.Models.DTO;
using StockTrading.Service.Interfaces;

namespace StockTrading.Services;
public class StockService : IStockService
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<StockService> _logger;

    public StockService(IStockRepository stockRepository, ILogger<StockService> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<StockDto>> GetAllStocksAsync()
    {
        _logger.LogInformation("Fetching all stocks.");
        var stocks = await _stockRepository.GetAllAsync();
        return stocks
            .Select(s => new StockDto
            {
                Id = s.Id,
                Symbol = s.Symbol,
                CompanyName = s.CompanyName,
                CurrentPrice = s.CurrentPrice,
                LastUpdated = s.LastUpdated
            })
            .ToList();
    }

    public async Task<StockDto?> GetStockBySymbolAsync(string symbol)
    {
        _logger.LogInformation("Fetching stock by symbol: {Symbol}", symbol);
        var stock = await _stockRepository.GetBySymbolAsync(symbol);
        if (stock == null) return null;

        return new StockDto
        {
            Id = stock.Id,
            Symbol = stock.Symbol,
            CompanyName = stock.CompanyName,
            CurrentPrice = stock.CurrentPrice,
            LastUpdated = stock.LastUpdated
        };
    }

    public async Task<StockDto?> GetStockByIdAsync(int id)
    {
        _logger.LogInformation("Fetching stock by ID: {Id}", id);
        var stock = await _stockRepository.GetByIdAsync(id);
        if (stock == null) return null;

        return new StockDto
        {
            Id = stock.Id,
            Symbol = stock.Symbol,
            CompanyName = stock.CompanyName,
            CurrentPrice = stock.CurrentPrice,
            LastUpdated = stock.LastUpdated
        };
    }

    public async Task UpdateStockPriceAsync(int stockId, decimal newPrice)
    {
        _logger.LogInformation("Updating price for Stock ID {StockId} to {NewPrice}", stockId, newPrice);
        var stock = await _stockRepository.GetByIdAsync(stockId);
        if (stock != null)
        {
            stock.CurrentPrice = newPrice;
            stock.LastUpdated = DateTime.UtcNow;
            _stockRepository.Update(stock);
            await _stockRepository.SaveChangesAsync();
            _logger.LogInformation("Stock ID {StockId} price updated successfully.", stockId);
        }
        else
        {
            _logger.LogWarning("Stock ID {StockId} not found for price update.", stockId);
        }
    }
}
