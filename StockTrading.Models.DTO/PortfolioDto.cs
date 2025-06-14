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
        public List<PortfolioItemDto> Items { get; set; } = new List<PortfolioItemDto>();
    }
}