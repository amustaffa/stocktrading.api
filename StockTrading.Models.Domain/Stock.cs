namespace StockTrading.Models.Domain;
public class Stock
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    /// <summary>
    /// Current market price of the stock (as of last close)
    /// </summary>
    public decimal CurrentPrice { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}