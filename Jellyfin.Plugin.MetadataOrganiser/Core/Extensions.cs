using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.MetadataOrganiser.Core;

/// <summary>
/// Provides extension methods for various classes.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Split and trim arguments given in a comma-delimited string.
    /// </summary>
    /// <param name="arg">The string representation of the argument list.</param>
    /// <returns>The separate arguments.</returns>
    public static IEnumerable<string> SplitArguments(this string arg) => arg
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => x.TrimStart('.').ToLowerInvariant());

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
    /// Filters out pairs that contain a null value.
    /// </summary>
    /// <param name="pairs">The pairs to filter.</param>
    /// <param name="key">The key to get a value for.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The value if found.</returns>
    public static T? GetValueCaseInsensitive<T>(
        this IEnumerable<KeyValuePair<string, T>> pairs, string key) =>
        pairs.FirstOrDefault(pair => pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;

    /// <summary>
    /// Makes all values in the given enumerable upper-case.
    /// </summary>
    /// <param name="values">The pairs to process.</param>
    /// <returns>The upper-case values.</returns>
    public static IEnumerable<string> ValuesToUpper(this IEnumerable<string> values) => values
        .Select(pair => pair.ToUpperInvariant());

    /// <summary>
    /// Maps the set of given items to the same value.
    /// </summary>
    /// <param name="items">The items to map.</param>
    /// <param name="value">The value to map to.</param>
    /// <typeparam name="T">The type of the value to map to.</typeparam>
    /// <returns>The mapped tags.</returns>
    public static IEnumerable<KeyValuePair<string, T>> MapTagsToValue<T>(this IEnumerable<string> items, T value) => items
        .Select(tag => new KeyValuePair<string, T>(tag, value));

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