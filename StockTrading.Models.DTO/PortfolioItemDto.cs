using System;

namespace StockTrading.Models.DTO
{
    public class PortfolioItemDto
    {
        public int Id { get; set; }
        public int StockId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal AverageCost { get; set; }

        // CurrentPrice is the latest price of the stock, which can be updated from an external source.
        public decimal CurrentPrice { get; set; }
        public decimal TotalValue {
            get
            {
                return Quantity * CurrentPrice;
            }
        }
        public decimal GainLoss
        {
            get
            {
                return (Quantity* CurrentPrice) - (Quantity* AverageCost);
            }
        }
    }
}