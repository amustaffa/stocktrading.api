using System.ComponentModel.DataAnnotations;
using StockTrading.Models.Domain;

namespace StockTrading.Models.DTO
{
    public class TradeOrderDto
    {        
        [Required]
        public string Symbol { get; set; }
        [Required]
        public TradeType Type { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
        public OrderType OrderType { get; set; } = OrderType.Market; // Default to Market order
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Limit price must be greater than 0.")]
        public decimal? LimitPrice { get; set; } // Nullable because it's only required for limit orders
    }
}