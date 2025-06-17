using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

public sealed class MarketDataCache
{
    //lock used inherently to ensure thread safety
    private static readonly Lazy<MarketDataCache> _instance =
        new(() => new MarketDataCache());

    public static MarketDataCache Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, decimal> _stockPrices;
    private readonly ILogger<MarketDataCache> _logger;

    private MarketDataCache()
    {
        _stockPrices = new ConcurrentDictionary<string, decimal>();
        _logger = LoggerFactory.Create(builder => builder.AddConsole())
                              .CreateLogger<MarketDataCache>();
    }

    public void UpdatePrice(string symbol, decimal price)
    {
        _stockPrices.AddOrUpdate(symbol, price, (_, _) => price);
        _logger.LogInformation("Updated price for {Symbol}: {Price}", symbol, price);
    }

    public decimal? GetPrice(string symbol)
    {
        return _stockPrices.TryGetValue(symbol, out var price) ? price : null;
    }
}