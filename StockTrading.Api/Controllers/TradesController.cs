using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using StockTradingApi.Hubs;
using StockTrading.Service.Interfaces;
using StockTrading.Models.DTO;

namespace StockTradingApi.Controllers
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize] // All endpoints in this controller require authentication
    public class TradesController : ControllerBase
    {
        private readonly ITradeService _tradeService;
        private readonly IPortfolioService _portfolioService;
        private readonly ILogger<TradesController> _logger;
        private readonly IHubContext<StockHub> _hubContext; // Inject SignalR Hub Context

        public TradesController(ITradeService tradeService, IPortfolioService portfolioService, ILogger<TradesController> logger, IHubContext<StockHub> hubContext)
        {
            _tradeService = tradeService;
            _portfolioService = portfolioService;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceTrade([FromBody] TradeOrderDto order)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get UserId from JWT claims
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("API: PlaceTrade request failed - User ID not found in claims.");
                return Unauthorized(new { message = "User not authenticated." });
            }

            _logger.LogInformation("API: PlaceTrade request received for User {UserId}: Symbol={Symbol}, Type={TradeType}, Quantity={Quantity}",
                userId, order.Symbol, order.Type, order.Quantity);

            try
            {
                // Notify all connected clients about the new trade via SignalR
                // In a real application, you might only send to the specific user or a group
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", $"New trade: {order.Type} {order.Quantity} of {order.Symbol} by user {userId}");                
                var trade = await _tradeService.PlaceTradeAsync(userId, order);
                await _hubContext.Clients.All.SendAsync("PortfolioUpdate", await _portfolioService.GetUserPortfolioAsync(userId));
                return CreatedAtAction(nameof(GetUserTrades), new { userId = userId }, trade);
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning("API: PlaceTrade failed for User {UserId}: {Message}", userId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: An unhandled error occurred during trade placement for User {UserId}", userId);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserTrades()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("API: GetUserTrades request failed - User ID not found in claims.");
                return Unauthorized(new { message = "User not authenticated." });
            }

            _logger.LogInformation("API: GetUserTrades request received for User {UserId}", userId);

            try
            {
                var trades = await _tradeService.GetUserTradesAsync(userId);
                return Ok(trades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: An unhandled error occurred while getting trades for User {UserId}", userId);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }
    }
}