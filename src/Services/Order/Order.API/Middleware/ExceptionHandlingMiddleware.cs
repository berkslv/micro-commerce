using System.Net;
using System.Text.Json;
using BuildingBlocks.Common.Exceptions;

namespace Order.API.Middleware;

/// <summary>
/// Middleware for handling exceptions globally.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse(
                    "Validation Error",
                    "One or more validation errors occurred.",
                    validationEx.Errors)),

            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ErrorResponse(
                    "Not Found",
                    notFoundEx.Message,
                    null)),

            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse(
                    "Domain Error",
                    domainEx.Message,
                    null)),

            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse(
                    "Internal Server Error",
                    "An unexpected error occurred.",
                    null))
        };

        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

/// <summary>
/// Error response model.
/// </summary>
public sealed record ErrorResponse(
    string Title,
    string Detail,
    IDictionary<string, string[]>? Errors);

/// <summary>
/// Extension methods for ExceptionHandlingMiddleware.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
