using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;

/// <summary>
/// Handles tag extraction from an item.
/// </summary>
/// <typeparam name="TItem">The <see cref="BaseItem"/> type that this extractor can process.</typeparam>
public abstract class TagExtractor<TItem>
    where TItem : BaseItem
{
    /// <summary>
    /// Gets the tag separator to use when joining a collection of tag values together as a string.
    /// </summary>
    protected const string TagArraySeparator = ",";

    /// <summary>
    /// Get the tags to assign to an item.
    /// </summary>
    /// <param name="item">The item to extract tags for.</param>
    /// <returns>The map of key-value pairs for tags to assign to an item.</returns>
    public abstract IEnumerable<KeyValuePair<string, string>> FormatTags(TItem item);

    /// <summary>
    /// Gets the core set of tags that apply to all <see cref="BaseItem"/> types.
    /// </summary>
    /// <param name="item">The item to extract tags for.</param>
    /// <returns>The map of key-value pairs for tags to assign to an item.</returns>
    protected IEnumerable<KeyValuePair<string, string>> FormatTags(BaseItem item) => new Dictionary<string, string?>
    {
        { "title", item.Name },
        { "album", item.Album },
        { "genre", string.Join(TagArraySeparator, item.Genres) },
        { "rating", (item.CriticRating / 20)?.ToString(CultureInfo.InvariantCulture) },
        { "keywords", string.Join(TagArraySeparator, item.Tags) },
    }.FilterNotNullOrEmpty();

    /// <summary>
    /// Gets the media streams indices containing metadata which need to be cleaned.
    /// </summary>
    /// <param name="item">The item with media streams to check.</param>
    /// <returns>The indices of the media streams in the item that need to be cleaned.</returns>
    public IEnumerable<MediaStream> GetStreamsToClean(TItem item) =>
        item.GetMediaStreams().Where(stream => ShouldStreamBeCleaned(stream, item));

    private bool ShouldStreamBeCleaned(MediaStream stream, TItem item)
    {
        string CleanValue(string value)
        {
            value = string.Join(string.Empty, value.Split("()?:<>'\"".ToArray()));
            value = string.Join(' ', value.Split("-_.|".ToArray()));
            return value;
        }

        return stream.Title != null && CleanValue(stream.Title)
            .Contains(CleanValue(item.Name), StringComparison.InvariantCultureIgnoreCase);
    }
}