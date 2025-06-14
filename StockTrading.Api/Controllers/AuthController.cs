using Microsoft.AspNetCore.Mvc;
using StockTrading.Models.DTO;
using StockTrading.Service.Interfaces;
using Microsoft.Extensions.Logging;

namespace StockTradingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            _logger.LogInformation("API: Register request received for email: {Email}", model.Email);
            try
            {
                var response = await _authService.RegisterAsync(model);
                if (response == null) return BadRequest(new { message = "Registration failed unexpectedly." });
                return Ok(response);
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning("API: Registration failed for email {Email}: {Message}", model.Email, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: An unhandled error occurred during registration for email {Email}", model.Email);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            _logger.LogInformation("API: Login request received for email: {Email}", model.Email);
            try
            {
                var response = await _authService.LoginAsync(model);
                if (response == null) return Unauthorized(new { message = "Invalid credentials." });
                return Ok(response);
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning("API: Login failed for email {Email}: {Message}", model.Email, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: An unhandled error occurred during login for email {Email}", model.Email);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }
    }
}