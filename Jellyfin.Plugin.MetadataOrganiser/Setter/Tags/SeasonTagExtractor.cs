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
public class SeasonTagExtractor : TagExtractor<Season>
{
    /// <inheritdoc />
    public SeasonTagExtractor(IMediaEncoder encoder, ILogger<TagExtractor<Season>> logger) : base(encoder, logger)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<KeyValuePair<string, string>> FormatTags(Season item) => FormatTags(item as BaseItem)
        .Concat(FormatSeriesTags(item).FilterNotNullOrEmpty())
        .Concat(FormatSeasonTags(item).FilterNotNullOrEmpty());
}