namespace StockTrading.Models.Domain;
public class PortfolioItem
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; } = null!; // Navigation property
    public int StockId { get; set; }
    public Stock Stock { get; set; } = null!; // Navigation property
    public int Quantity { get; set; }
    public decimal AverageCost { get; set; } // Average price at which the stock was acquired
}