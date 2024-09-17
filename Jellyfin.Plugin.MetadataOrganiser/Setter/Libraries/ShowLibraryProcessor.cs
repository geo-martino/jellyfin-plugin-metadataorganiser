using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter.Libraries;

/// <inheritdoc />
public class ShowLibraryProcessor : LibraryProcessor<Episode, VideoTagExtractor<Episode>>
{
    /// <inheritdoc cref="LibraryProcessor{Episode,EpisodeTagExtractor}"/>
    public ShowLibraryProcessor(
        ILibraryManager libraryManager,
        IMediaEncoder encoder,
        IConfigurationManager config,
        ILogger<ShowLibraryProcessor> loggerProcessor,
        ILogger<EpisodeTagExtractor> loggerExtractor)
        : base(libraryManager, encoder, config, new EpisodeTagExtractor(encoder, loggerExtractor), loggerProcessor)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<Episode> GetItems() => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Episode],
            IsVirtualItem = false,
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true
        }).OfType<Episode>().Where(episode => File.Exists(episode.Path));

    /// <inheritdoc />
    protected override string GetTagDropValue(Episode item) => item.Series?.Name ?? item.Name;
}