namespace BuildingBlocks.Messaging.Models;

/// <summary>
/// Async local storage for values across async operations.
/// </summary>
/// <typeparam name="T">Type to store</typeparam>
public static class AsyncStorage<T> where T : new()
{
    private static readonly AsyncLocal<T> s_asyncLocal = new();

    public static T Store(T val)
    {
        s_asyncLocal.Value = val;
        return s_asyncLocal.Value;
    }

    public static T? Retrieve()
    {
        return s_asyncLocal.Value;
    }
}
