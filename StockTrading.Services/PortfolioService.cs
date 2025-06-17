using StockTrading.Models.Domain;
using Microsoft.Extensions.Logging;
using StockTrading.Repository.Interfaces;
using StockTrading.Models.DTO;
using StockTrading.Service.Interfaces;
using System.Runtime.CompilerServices;

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
                Symbol = item.Stock?.Symbol ?? "N/A",
                CurrentPrice = item.Stock?.CurrentPrice ?? 0,
                CompanyName = item.Stock?.CompanyName ?? "Unknown Company",
                Quantity = item.Quantity,
                AverageCost = item.AverageCost,
            });
        }

        return portfolioDto;
    }
}