using System;

namespace StockTrading.Models.DTO
{
    public class PortfolioItemDto
    {
        public int Id { get; set; }
        public int StockId { get; set; }
        public string StockSymbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal CurrentMarketValue { get; set; } // Quantity * CurrentPrice
        public decimal ProfitLoss { get; set; } // CurrentMarketValue - (Quantity * AverageCost)
    }
}