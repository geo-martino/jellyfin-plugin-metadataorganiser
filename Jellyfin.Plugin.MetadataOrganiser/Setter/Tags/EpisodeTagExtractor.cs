using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;

/// <inheritdoc />
public class EpisodeTagExtractor : VideoTagExtractor<Episode>
{
    /// <inheritdoc />
    public override IEnumerable<KeyValuePair<string, string>> FormatTags(Episode item) => base.FormatTags(item)
        .Concat(new Dictionary<string, string?>
        {
            { "episode_number", item.IndexNumber?.ToString(CultureInfo.InvariantCulture) },
            {
                "episode_total", item.Season?.Children?.OfType<Episode>()
                    .Max(episode => episode.IndexNumber)?.ToString(CultureInfo.InvariantCulture)
            },
            { "season_number", item.ParentIndexNumber?.ToString(CultureInfo.InvariantCulture) },
            {
                "season_total", item.Series?.Children?.OfType<Season>()
                    .Max(season => season.IndexNumber)?.ToString(CultureInfo.InvariantCulture)
            }
        }.FilterNotNullOrEmpty());
}