using Microsoft.AspNetCore.Mvc;
using StockTrading.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace StockTradingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints in this controller require authentication
    public class StocksController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StocksController> _logger;

        public StocksController(IStockService stockService, ILogger<StocksController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStocks()
        {
            _logger.LogInformation("API: Get all stocks request received.");
            try
            {
                var stocks = await _stockService.GetAllStocksAsync();
                return Ok(stocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: An unhandled error occurred while getting all stocks.");
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpGet("{symbol}")]
        public async Task<IActionResult> GetStockBySymbol(string symbol)
        {
            _logger.LogInformation("API: Get stock by symbol request received for symbol: {Symbol}", symbol);
            try
            {
                var stock = await _stockService.GetStockBySymbolAsync(symbol.ToUpper());
                if (stock == null)
                {
                    _logger.LogWarning("API: Stock with symbol {Symbol} not found.", symbol);
                    return NotFound(new { message = $"Stock with symbol '{symbol}' not found." });
                }
                return Ok(stock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: An unhandled error occurred while getting stock by symbol {Symbol}", symbol);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpPut("{id}/price")] // Example: Admin endpoint to update price
        [Authorize(Roles = "Admin")] // Example: Only admin can update prices
        public async Task<IActionResult> UpdateStockPrice(int id, [FromQuery] decimal newPrice)
        {
            _logger.LogInformation("API: Update stock price request for ID {StockId} to {NewPrice}", id, newPrice);
            try
            {
                var stock = await _stockService.GetStockByIdAsync(id);
                if (stock == null)
                {
                    _logger.LogWarning("API: Stock with ID {StockId} not found for price update.", id);
                    return NotFound(new { message = $"Stock with ID '{id}' not found." });
                }

                await _stockService.UpdateStockPriceAsync(id, newPrice);
                return NoContent(); // 204 No Content
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: An unhandled error occurred while updating stock price for ID {StockId}", id);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }
    }
}