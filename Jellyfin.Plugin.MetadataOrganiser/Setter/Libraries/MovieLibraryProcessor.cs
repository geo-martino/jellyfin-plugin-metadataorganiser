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
public class MovieLibraryProcessor : LibraryProcessor<Movie, MovieTagExtractor>
{
    /// <inheritdoc cref="LibraryProcessor{Movie,MovieTagExtractor}"/>
    public MovieLibraryProcessor(
        ILibraryManager libraryManager,
        IMediaEncoder encoder,
        IConfigurationManager config,
        ILogger<MovieLibraryProcessor> loggerProcessor)
        : base(libraryManager, encoder, config, new MovieTagExtractor(), loggerProcessor)
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
}