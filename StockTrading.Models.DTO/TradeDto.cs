using StockTrading.Models.Domain;

namespace StockTrading.Models.DTO
{
    public class TradeDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int StockId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public TradeType Type { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime TradeDate { get; set; }
    }
}