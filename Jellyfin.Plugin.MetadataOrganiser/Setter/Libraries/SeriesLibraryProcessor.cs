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
public class SeriesLibraryProcessor : LibraryProcessor<Series, TagExtractor<Series>>
{
    /// <inheritdoc cref="LibraryProcessor{Series,SeriesTagExtractor}"/>
    public SeriesLibraryProcessor(
        ILibraryManager libraryManager,
        IMediaEncoder encoder,
        IConfigurationManager config,
        ILogger<SeriesLibraryProcessor> loggerProcessor,
        ILogger<SeriesTagExtractor> loggerExtractor)
        : base(libraryManager, encoder, config, new SeriesTagExtractor(encoder, loggerExtractor), loggerProcessor)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<Series> GetItems() => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Series],
            IsVirtualItem = false,
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true
        }).OfType<Series>().Where(series => Directory.Exists(series.Path));

    /// <inheritdoc />
    protected override string GetTagDropValue(Series item) => item.Name;
}