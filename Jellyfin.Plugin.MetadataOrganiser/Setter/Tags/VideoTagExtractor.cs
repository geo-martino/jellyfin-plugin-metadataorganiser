using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;

/// <inheritdoc />
public abstract class VideoTagExtractor<T> : TagExtractor<T>
    where T : Video
{
    /// <inheritdoc />
    public override IEnumerable<KeyValuePair<string, string>> FormatTags(T item) => FormatTags(item as BaseItem)
        .Concat(FormatProviderIdTags(item, item.ProviderIds))
        .Concat(new Dictionary<string, string?>
        {
            { "description", item.Tagline },
            { "summary", item.Overview },
            { "law_rating", item.OfficialRating },
            { "date_released", item.PremiereDate?.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture) },
            { "production_studio", string.Join(TagArraySeparator, item.Studios) },
            { "recording_location", string.Join(TagArraySeparator, item.ProductionLocations) }
        }.FilterNotNullOrEmpty());

    private IEnumerable<KeyValuePair<string, string>> FormatProviderIdTags(
        Video item, IEnumerable<KeyValuePair<string, string>> providerIds) =>
        providerIds.Select(pair => FormatProviderIdTag(item, pair));

    private KeyValuePair<string, string> FormatProviderIdTag(Video item, KeyValuePair<string, string> pair)
    {
        var key = pair.Key.ToLowerInvariant() switch
        {
            "tvdb" => "tvdb2",
            _ => pair.Key.ToLowerInvariant()
        };

        var value = key switch
        {
            "tmdb" => FormatTmdbIdValue(item, pair.Value),
            "tvdb2" => FormatTvdbIdValue(item, pair.Value),
            _ => pair.Value
        };

        return new KeyValuePair<string, string>(key, value);
    }

    private static string FormatTmdbIdValue(Video item, string id) => item.GetBaseItemKind() switch
    {
        BaseItemKind.Movie => $"movie/{id}",
        BaseItemKind.Episode or BaseItemKind.Season or BaseItemKind.Series => $"tv/{id}",
        _ => id
    };

    private static string FormatTvdbIdValue(Video item, string id) => item.GetBaseItemKind() switch
    {
        BaseItemKind.Movie => $"movies/{id}",
        BaseItemKind.Episode => $"episodes/{id}",
        BaseItemKind.Series => $"series/{id}",
        _ => id
    };
}