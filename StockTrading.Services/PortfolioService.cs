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
        var portfolioDto = new PortfolioDto();
        _logger.LogInformation("Fetching portfolio for user: {UserId}", userId);

        try
        {
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
                    LastUpdated = DateTime.UtcNow,
                    Items = new List<PortfolioItem>()
                };
                _portfolioRepository.Add(portfolio);
                await _portfolioRepository.SaveChangesAsync();
                _logger.LogInformation("Default portfolio created for user {UserId}.", userId);
            }

            portfolioDto.Id = portfolio.Id;
            portfolioDto.UserId = portfolio.UserId;
            portfolioDto.Name = portfolio.Name;
            portfolioDto.CreatedDate = portfolio.CreatedDate;
            portfolioDto.LastUpdated = portfolio.LastUpdated;
            portfolioDto.Items = new List<PortfolioItemDto>();

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
        }
        finally
        {
            _portfolioRepository.Dispose(); // Ensure the repository is disposed after use
        }
        return portfolioDto;
    }
}