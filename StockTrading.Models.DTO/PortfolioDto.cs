using System;
using System.Collections.Generic;

namespace StockTrading.Models.DTO
{
    public class PortfolioDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }

        // TotalValue is the sum of CurrentMarketValue of all items
        public decimal TotalValue { 
            get 
            {
                decimal total = 0;
                foreach (var item in Items)
                {
                    total += item.TotalValue;
                }
                return total;
            } 
        }

        // TotalGainLoss is the sum of ProfitLoss of all items
        public decimal TotalCost { 
            get 
            {
                decimal total = 0;
                foreach (var item in Items)
                {
                    total += item.Quantity * item.AverageCost;
                }
                return total;
            } 
        }
        public decimal TotalGainLoss {
            get 
            {
                decimal total = 0;
                foreach (var item in Items)
                {
                    total += item.GainLoss;
                }
                return total;
            } 
        } 
        public decimal TotalGainLossPercent 
        { 
            get 
            {
                if (TotalValue == 0) return 0;
                    return (TotalGainLoss / TotalValue) * 100;
            } 
        }
        public List<PortfolioItemDto> Items { get; set; } = new List<PortfolioItemDto>();
    }
}