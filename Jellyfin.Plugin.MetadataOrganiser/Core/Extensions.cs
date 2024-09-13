using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.MetadataOrganiser.Core;

/// <summary>
/// Provides extension methods for various classes.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Filters out pairs that contain a null value.
    /// </summary>
    /// <param name="pairs">The pairs to filter.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The filtered pairs.</returns>
    public static IEnumerable<KeyValuePair<TKey, TValue>> FilterNotNullOrEmpty<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue?>> pairs) =>
        pairs.Where(pair => pair.Value is not null && pair.Value!.ToString()?.Length > 0)
            .OfType<KeyValuePair<TKey, TValue>>();

    /// <summary>
    /// Makes all keys in the given pairs upper-case.
    /// </summary>
    /// <param name="pairs">The pairs to process.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The pairs with upper-case keys.</returns>
    public static IEnumerable<KeyValuePair<string, T>> KeysToUpper<T>(
        this IEnumerable<KeyValuePair<string, T>> pairs) =>
        pairs.Select(pair => new KeyValuePair<string, T>(pair.Key.ToUpperInvariant(), pair.Value));
}