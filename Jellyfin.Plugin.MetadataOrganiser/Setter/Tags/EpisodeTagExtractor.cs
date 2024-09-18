using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;

/// <inheritdoc />
public class EpisodeTagExtractor : VideoTagExtractor<Episode>
{
    /// <inheritdoc />
    public EpisodeTagExtractor(IMediaEncoder encoder, ILogger<TagExtractor<Episode>> logger) : base(encoder, logger)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<KeyValuePair<string, string>> FormatTags(Episode item) => FormatTags(item as BaseItem)
        .Concat(new Dictionary<string, string?>
        {
            { "episode_number", item.IndexNumber?.ToString(CultureInfo.InvariantCulture) },
            {
                "episode_total", item.Season?.Children?.OfType<Episode>()
                    .Max(episode => episode.IndexNumber)?.ToString(CultureInfo.InvariantCulture) ?? "0"
            },
            { "season_number", item.ParentIndexNumber?.ToString(CultureInfo.InvariantCulture) },
            {
                "season_total", item.Series?.Children?.OfType<Season>()
                .Max(season => season.IndexNumber)?.ToString(CultureInfo.InvariantCulture) ?? "0"
            },
            { "show", item.Series?.Name },
        }.FilterNotNullOrEmpty());
}