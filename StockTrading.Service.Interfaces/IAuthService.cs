using StockTrading.Models.DTO;
namespace StockTrading.Service.Interfaces;
public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto model);
    Task<AuthResponseDto?> LoginAsync(LoginDto model);
}