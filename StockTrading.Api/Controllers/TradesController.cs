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
    [Route("api/[controller]")]
    [Authorize] // All endpoints in this controller require authentication
    public class TradesController : ControllerBase
    {
        private readonly ITradeService _tradeService;
        private readonly ILogger<TradesController> _logger;
        private readonly IHubContext<StockHub> _hubContext; // Inject SignalR Hub Context

        public TradesController(ITradeService tradeService, ILogger<TradesController> logger, IHubContext<StockHub> hubContext)
        {
            _tradeService = tradeService;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceTrade([FromBody] CreateTradeDto createTradeDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get UserId from JWT claims
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("API: PlaceTrade request failed - User ID not found in claims.");
                return Unauthorized(new { message = "User not authenticated." });
            }

            _logger.LogInformation("API: PlaceTrade request received for User {UserId}: StockId={StockId}, Type={TradeType}, Quantity={Quantity}",
                userId, createTradeDto.StockId, createTradeDto.Type, createTradeDto.Quantity);

            try
            {
                var trade = await _tradeService.PlaceTradeAsync(userId, createTradeDto);

                // Notify all connected clients about the new trade via SignalR
                // In a real application, you might only send to the specific user or a group
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", $"New trade: {trade.Type} {trade.Quantity} of {trade.StockSymbol} by user {userId}");

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

        [HttpGet("my-trades")]
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