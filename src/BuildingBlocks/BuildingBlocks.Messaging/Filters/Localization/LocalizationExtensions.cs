namespace BuildingBlocks.Messaging.Filters.Localization;

/// <summary>
/// Extension methods for localization.
/// </summary>
public static class LocalizationExtensions
{
    private static readonly HashSet<string> s_acceptableCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "en-US",
        "tr-TR",
        "de-DE",
        "fr-FR",
        "es-ES"
    };

    /// <summary>
    /// Checks if the provided culture is acceptable.
    /// </summary>
    /// <param name="cultureName">Culture name to check</param>
    /// <returns>True if culture is acceptable</returns>
    public static bool IsCultureAcceptable(string cultureName)
    {
        return s_acceptableCultures.Contains(cultureName);
    }

    /// <summary>
    /// Gets the list of acceptable cultures.
    /// </summary>
    public static IEnumerable<string> AcceptableCultures
    {
        get
        {
            return s_acceptableCultures;
        }
    }
}
