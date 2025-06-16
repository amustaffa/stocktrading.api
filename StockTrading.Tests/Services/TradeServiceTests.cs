using Moq;
using Xunit;
using FluentAssertions;
using StockTrading.Services;
using StockTrading.Models.Domain;
using StockTrading.Models.DTO;
using StockTrading.Repository.Interfaces;
using StockTrading.Service.Interfaces;
using Microsoft.Extensions.Logging;

namespace StockTrading.Tests.Services
{
    public class TradeServiceTests
    {
        private readonly Mock<ITradeRepository> _mockTradeRepository;
        private readonly Mock<IStockRepository> _mockStockRepository;
        private readonly Mock<IPortfolioService> _mockPortfolioService;
        private readonly Mock<ILogger<TradeService>> _mockLogger;
        private readonly TradeService _tradeService;

        public TradeServiceTests()
        {
            _mockTradeRepository = new Mock<ITradeRepository>();
            _mockStockRepository = new Mock<IStockRepository>();
            _mockPortfolioService = new Mock<IPortfolioService>();
            _mockLogger = new Mock<ILogger<TradeService>>();

            _tradeService = new TradeService(
                _mockTradeRepository.Object,
                _mockStockRepository.Object,
                _mockLogger.Object,
                _mockPortfolioService.Object
            );
        }

        [Fact]
        public async Task PlaceTradeAsync_ValidBuyOrder_ShouldSucceed()
        {
            // Arrange
            var userId = "testUser";
            var order = new TradeOrderDto
            {
                Symbol = "AAPL",
                Type = TradeType.Buy,
                Quantity = 10
            };

            var stock = new Stock
            {
                Id = 1,
                Symbol = "AAPL",
                CurrentPrice = 150.00m
            };

            _mockStockRepository
                .Setup(x => x.GetBySymbolAsync("AAPL"))
                .ReturnsAsync(stock);

            // Act
            var result = await _tradeService.PlaceTradeAsync(userId, order);

            // Assert
            result.Should().NotBeNull();
            result.Symbol.Should().Be("AAPL");
            result.Quantity.Should().Be(10);
            result.Type.Should().Be(TradeType.Buy);
            result.Price.Should().Be(150.00m);

            _mockTradeRepository.Verify(x => x.Add(It.IsAny<Trade>()), Times.Once);
            _mockTradeRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task PlaceTradeAsync_InvalidStock_ShouldThrowException()
        {
            // Arrange
            var userId = "testUser";
            var order = new TradeOrderDto
            {
                Symbol = "THIS IS A INVALID SYMBOL",
                Type = TradeType.Buy,
                Quantity = 10
            };

            _mockStockRepository
                .Setup(x => x.GetBySymbolAsync("INVALID"))
                .ReturnsAsync((Stock)null);

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(
                () => _tradeService.PlaceTradeAsync(userId, order)
            );
        }

        [Fact]
        public async Task PlaceTradeAsync_SellWithInsufficientQuantity_ShouldThrowException()
        {
            // Arrange
            var userId = "testUser";
            var order = new TradeOrderDto
            {
                Symbol = "AAPL",
                Type = TradeType.Sell,
                Quantity = 10
            };

            var stock = new Stock
            {
                Id = 1,
                Symbol = "AAPL",
                CurrentPrice = 150.00m
            };

            _mockStockRepository
                .Setup(x => x.GetBySymbolAsync("AAPL"))
                .ReturnsAsync(stock);

            _mockPortfolioService
                .Setup(x => x.ValidateTradeQuantityAsync(userId, It.IsAny<Trade>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(
                () => _tradeService.PlaceTradeAsync(userId, order)
            );
        }

        [Fact]
        public async Task GetUserTradesAsync_ShouldReturnUserTrades()
        {
            // Arrange
            var userId = "testUser";
            var trades = new List<Trade>
            {
                new Trade
                {
                    Id = 1,
                    UserId = userId,
                    StockId = 1,
                    Type = TradeType.Buy,
                    Quantity = 10,
                    Price = 150.00m,
                    Stock = new Stock { Symbol = "AAPL" }
                }
            };

            _mockTradeRepository
                .Setup(x => x.GetUserTradesAsync(userId))
                .ReturnsAsync(trades);

            // Act
            var result = await _tradeService.GetUserTradesAsync(userId);

            // Assert
            result.Should().HaveCount(1);
            var trade = result.First();
            trade.Symbol.Should().Be("AAPL");
            trade.Quantity.Should().Be(10);
        }
    }
}