using BuildingBlocks.Messaging.Models;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Messaging.Filters.Tokens;

/// <summary>
/// ASP.NET Core middleware that extracts JWT token from Authorization header.
/// </summary>
public class TokenMiddleware : IMiddleware
{
    private const string AuthorizationHeader = "Authorization";

    private const string BearerPrefix = "Bearer ";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Headers.TryGetValue(AuthorizationHeader, out var authHeader))
        {
            var authValue = authHeader.ToString();
            if (authValue.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var token = authValue[BearerPrefix.Length..];
                AsyncStorage<Token>.Store(new Token { Content = token });
            }
        }

        await next(context);
    }
}
