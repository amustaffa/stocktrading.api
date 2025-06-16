using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using FluentAssertions;
using StockTradingApi.Controllers;
using StockTradingApi.Hubs;
using StockTrading.Models.DTO;
using StockTrading.Models.Domain;
using StockTrading.Service.Interfaces;

namespace StockTrading.Tests.Controllers
{
    public class TradesControllerTests
    {
        private readonly Mock<ITradeService> _mockTradeService;
        private readonly Mock<IPortfolioService> _mockPortfolioService;
        private readonly Mock<ILogger<TradesController>> _mockLogger;
        private readonly Mock<IHubContext<StockHub>> _mockHubContext;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly TradesController _controller;

        public TradesControllerTests()
        {
            _mockTradeService = new Mock<ITradeService>();
            _mockPortfolioService = new Mock<IPortfolioService>();
            _mockLogger = new Mock<ILogger<TradesController>>();
            _mockHubContext = new Mock<IHubContext<StockHub>>();
            _mockClientProxy = new Mock<IClientProxy>();

            // Setup SignalR hub
            _mockHubContext.Setup(x => x.Clients.All).Returns(_mockClientProxy.Object);

            _controller = new TradesController(
                _mockTradeService.Object,
                _mockPortfolioService.Object,
                _mockLogger.Object,
                _mockHubContext.Object
            );

            // Setup ClaimsPrincipal
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Set the User property on ControllerBase
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task PlaceTrade_ValidOrder_ReturnsCreatedResult()
        {
            // Arrange
            var order = new TradeOrderDto
            {
                Symbol = "AAPL",
                Type = TradeType.Buy,
                Quantity = 10
            };

            var expectedTrade = new TradeDto
            {
                Id = 1,
                UserId = "testUserId",
                Symbol = "AAPL",
                Type = TradeType.Buy,
                Quantity = 10
            };

            _mockTradeService
                .Setup(x => x.PlaceTradeAsync("testUserId", order))
                .ReturnsAsync(expectedTrade);

            // Act
            var result = await _controller.PlaceTrade(order);

            // Assert
            var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnValue = createdAtActionResult.Value.Should().BeOfType<TradeDto>().Subject;
            
            returnValue.Symbol.Should().Be("AAPL");
            returnValue.Quantity.Should().Be(10);

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                "ReceiveMessage",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task PlaceTrade_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var order = new TradeOrderDto
            {
                Symbol = "AAPL",
                Type = TradeType.Buy,
                Quantity = 10
            };

            // Act
            var result = await _controller.PlaceTrade(order);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetUserTrades_AuthorizedUser_ReturnsOkResult()
        {
            // Arrange
            var expectedTrades = new List<TradeDto>
            {
                new TradeDto { Id = 1, Symbol = "AAPL", Quantity = 10 },
                new TradeDto { Id = 2, Symbol = "MSFT", Quantity = 20 }
            };

            _mockTradeService
                .Setup(x => x.GetUserTradesAsync("testUserId"))
                .ReturnsAsync(expectedTrades);

            // Act
            var result = await _controller.GetUserTrades();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var trades = okResult.Value.Should().BeOfType<List<TradeDto>>().Subject;
            trades.Should().HaveCount(2);
            trades.First().Symbol.Should().Be("AAPL");
        }
    }
}