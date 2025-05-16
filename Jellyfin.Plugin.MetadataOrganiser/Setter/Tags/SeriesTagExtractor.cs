using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;

/// <inheritdoc />
public class SeriesTagExtractor : TagExtractor<Series>
{
    /// <inheritdoc />
    public SeriesTagExtractor(IMediaEncoder encoder, ILogger<TagExtractor<Series>> logger) : base(encoder, logger)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<KeyValuePair<string, string>> FormatTags(Series item) => FormatTags(item as BaseItem)
        .Concat(FormatSeriesTags(item).FilterNotNullOrEmpty());
}