using Microsoft.AspNetCore.Identity;
namespace StockTrading.Models.Domain;

public class ApplicationUser : IdentityUser
{
    // Add any additional properties for your user here (e.g., FirstName, LastName)
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}