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
        .Concat(FormatSeriesTags(item).FilterNotNullOrEmpty())
        .Concat(FormatSeasonTags(item).FilterNotNullOrEmpty())
        .Concat(FormatEpisodeTags(item).FilterNotNullOrEmpty());
}