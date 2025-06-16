using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Shared;

namespace StockTradingApi.Middlewares;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (exception is Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException tokenException)
        {
            _logger.LogWarning(
                "Token expired. Expiration: {Expiration}, Current UTC: {CurrentTime}, Difference: {TimeDiff} minutes",
                tokenException.Expires,
                DateTime.UtcNow,
                ((DateTime)tokenException.Expires! - DateTime.UtcNow).TotalMinutes);
        }

        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            DatabaseConnectionException dbEx => new
            {
                message = "Database connection error occurred",
                statusCode = StatusCodes.Status503ServiceUnavailable
            },
            _ => new
            {
                message = "An unexpected error occurred. Please try again later.",
                statusCode = StatusCodes.Status500InternalServerError
            }
        };
        
        context.Response.StatusCode = response.statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
