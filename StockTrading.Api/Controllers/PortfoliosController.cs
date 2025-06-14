using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using StockTrading.Service.Interfaces;

namespace StockTradingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints in this controller require authentication
    public class PortfoliosController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly ILogger<PortfoliosController> _logger;

        public PortfoliosController(IPortfolioService portfolioService, ILogger<PortfoliosController> logger)
        {
            _portfolioService = portfolioService;
            _logger = logger;
        }

        [HttpGet("my-portfolio")]
        public async Task<IActionResult> GetUserPortfolio()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get UserId from JWT claims
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("API: GetUserPortfolio request failed - User ID not found in claims.");
                return Unauthorized(new { message = "User not authenticated." });
            }

            _logger.LogInformation("API: GetUserPortfolio request received for User {UserId}", userId);

            try
            {
                var portfolio = await _portfolioService.GetUserPortfolioAsync(userId);
                if (portfolio == null)
                {
                    _logger.LogWarning("API: Portfolio not found for user {UserId}.", userId);
                    return NotFound(new { message = "Portfolio not found." });
                }
                return Ok(portfolio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: An unhandled error occurred while getting portfolio for User {UserId}", userId);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }
    }
}

