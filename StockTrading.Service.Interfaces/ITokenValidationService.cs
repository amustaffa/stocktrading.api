using System.Security.Claims;

namespace StockTrading.Service.Interfaces
{
    public interface ITokenValidationService
    {
        bool ValidateToken(string token);
        ClaimsPrincipal? GetPrincipalFromToken(string token);
        string? GetUserIdFromToken(string token);
    }
}