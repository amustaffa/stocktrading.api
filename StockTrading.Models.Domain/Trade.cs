namespace StockTrading.Models.Domain;
public class Trade
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Application User
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    public int StockId { get; set; }
    /// <summary>
    /// Navigation property to linked Stock
    /// </summary>
    public Stock Stock { get; set; } = null!; 
    public TradeType Type { get; set; }
    public int Quantity { get; set; }
    /// <summary>
    /// Price at which the stock was traded
    /// </summary>
    public decimal Price { get; set; }
    public DateTime TradeDate { get; set; } = DateTime.UtcNow;
}
