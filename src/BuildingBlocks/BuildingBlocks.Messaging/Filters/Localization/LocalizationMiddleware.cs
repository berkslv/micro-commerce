using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Messaging.Filters.Localization;

/// <summary>
/// ASP.NET Core middleware that sets culture from Accept-Language header.
/// </summary>
public class LocalizationMiddleware : IMiddleware
{
    private const string AcceptLanguageHeader = "Accept-Language";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var cultureKey = context.Request.Headers[AcceptLanguageHeader].FirstOrDefault();

        CultureInfo culture;

        if (!string.IsNullOrEmpty(cultureKey) && LocalizationExtensions.IsCultureAcceptable(cultureKey))
        {
            culture = new CultureInfo(cultureKey);
        }
        else
        {
            culture = new CultureInfo("en-US");
        }

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        await next(context);
    }
}
