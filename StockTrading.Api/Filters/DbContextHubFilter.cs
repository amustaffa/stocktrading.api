using Microsoft.AspNetCore.SignalR;
using StockTrading.Data;

namespace StockTradingApi.Filters;
public class DbContextHubFilter : IHubFilter
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DbContextHubFilter(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext, 
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            return await next(invocationContext);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }
}