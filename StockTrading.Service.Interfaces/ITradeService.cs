using StockTrading.Models.DTO;

namespace StockTrading.Service.Interfaces;

public interface ITradeService
{
    Task<TradeDto> PlaceTradeAsync(string userId, TradeOrderDto order);
    Task<IEnumerable<TradeDto>> GetUserTradesAsync(string userId);
}
