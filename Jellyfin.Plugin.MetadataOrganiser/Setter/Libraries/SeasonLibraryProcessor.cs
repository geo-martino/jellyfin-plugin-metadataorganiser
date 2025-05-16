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
public class SeasonLibraryProcessor : LibraryProcessor<Season, TagExtractor<Season>>
{
    /// <inheritdoc cref="LibraryProcessor{Season,SeasonTagExtractor}"/>
    public SeasonLibraryProcessor(
        ILibraryManager libraryManager,
        IMediaEncoder encoder,
        IConfigurationManager config,
        ILogger<SeasonLibraryProcessor> loggerProcessor,
        ILogger<SeasonTagExtractor> loggerExtractor)
        : base(libraryManager, encoder, config, new SeasonTagExtractor(encoder, loggerExtractor), loggerProcessor)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<Season> GetItems() => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Season],
            IsVirtualItem = false,
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true
        }).OfType<Season>().Where(season => Directory.Exists(season.Path));

    /// <inheritdoc />
    protected override string GetTagDropValue(Season item) => item.Series?.Name ?? item.Name;
}