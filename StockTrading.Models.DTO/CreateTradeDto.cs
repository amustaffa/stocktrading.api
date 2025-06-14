using System.ComponentModel.DataAnnotations;
using StockTrading.Models.Domain;

namespace StockTrading.Models.DTO
{
    public class CreateTradeDto
    {
        [Required]
        public int StockId { get; set; }
        [Required]
        public TradeType Type { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }
}