using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace StockTradingApi.Hubs;

public class StockHub : Hub
{
    private readonly ILogger<StockHub> _logger;

    public StockHub(ILogger<StockHub> logger)
    {
        _logger = logger;
    }

    // This method can be invoked by a client (e.g., your Angular app)
    public async Task SendMessage(string message)
    {
        _logger.LogInformation("SignalR: Received message from client {ConnectionId}: {Message}", Context.ConnectionId, message);
        // Send a message back to all connected clients
        await Clients.All.SendAsync("ReceiveMessage", $"Server received: {message} at {DateTime.UtcNow}");
    }

        // This method can be invoked by a client (e.g., your Angular app)
    public async Task SubscribeToPortfolio()
    {
        _logger.LogInformation("SignalR: Received message from client {ConnectionId}", Context.ConnectionId);
        // Send a message back to all connected clients
        await Clients.All.SendAsync("SubscribeToPortfolio", $"Subscribed to portfolio updates at {DateTime.UtcNow}");
    }


    // Override OnConnectedAsync and OnDisconnectedAsync for connection lifecycle logging
    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR: Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR: Client disconnected: {ConnectionId}. Exception: {ExceptionMessage}", Context.ConnectionId, exception?.Message);
        return base.OnDisconnectedAsync(exception);
    }
}
