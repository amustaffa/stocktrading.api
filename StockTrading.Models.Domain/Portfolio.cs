namespace StockTrading.Models.Domain;
public class Portfolio
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // Foreign key to ApplicationUser
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation property for portfolio items
    public ICollection<PortfolioItem> Items { get; set; } = new List<PortfolioItem>();
}