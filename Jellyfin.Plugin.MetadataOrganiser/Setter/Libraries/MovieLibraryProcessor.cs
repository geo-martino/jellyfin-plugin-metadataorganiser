using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter.Libraries;

/// <inheritdoc />
public class MovieLibraryProcessor : LibraryProcessor<Movie, VideoTagExtractor<Movie>>
{
    /// <inheritdoc cref="LibraryProcessor{Movie,MovieTagExtractor}"/>
    public MovieLibraryProcessor(
        ILibraryManager libraryManager,
        IMediaEncoder encoder,
        IConfigurationManager config,
        ILogger<MovieLibraryProcessor> loggerProcessor,
        ILogger<VideoTagExtractor<Movie>> loggerExtractor)
        : base(libraryManager, encoder, config, new VideoTagExtractor<Movie>(encoder, loggerExtractor), loggerProcessor)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<Movie> GetItems() => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            IsVirtualItem = false,
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true
        }).OfType<Movie>().Where(movie => File.Exists(movie.Path));

    /// <inheritdoc />
    protected override string GetTagDropValue(Movie item) => item.Name;
}