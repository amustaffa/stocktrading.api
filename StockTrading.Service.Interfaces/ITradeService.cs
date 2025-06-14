using StockTrading.Models.DTO;

namespace StockTrading.Service.Interfaces;

public interface ITradeService
{
    Task<TradeDto> PlaceTradeAsync(string userId, CreateTradeDto createTradeDto);
    Task<IEnumerable<TradeDto>> GetUserTradesAsync(string userId);
}
