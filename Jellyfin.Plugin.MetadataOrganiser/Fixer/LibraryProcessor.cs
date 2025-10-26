using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Fixer;

/// <summary>
/// Handles Library manipulation operations.
/// </summary>
public class LibraryProcessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryProcessor"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public LibraryProcessor(
        ILibraryManager libraryManager,
        ILogger<LibraryProcessor> logger)
    {
        LibraryManager = libraryManager;
        Logger = logger;
    }

    /// <summary>
    /// Gets the library manager.
    /// </summary>
    private ILibraryManager LibraryManager { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    private ILogger<LibraryProcessor> Logger { get; }

    /// <summary>
    /// Gets the items to process from the library.
    /// </summary>
    /// <returns>The items to process.</returns>
    private IReadOnlyList<BaseItem> GetItems() => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [
                BaseItemKind.Audio,
                BaseItemKind.Video,
            ],
            IsVirtualItem = false,
            Recursive = true
        });

    /// <summary>
    /// Gets the splittable items from the library.
    /// </summary>
    /// <returns>The items to process.</returns>
    private IReadOnlyList<BaseItem> GetSplittableItems() => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Genre, BaseItemKind.Studio],
            IsVirtualItem = false,
            Recursive = true
        });

    /// <summary>
    /// Split the library's list-able tags on the given separator. Skips if separator string is empty.
    /// </summary>
    /// <param name="separator">The separator string.</param>
    /// <param name="progressHandler">Instance of the <see cref="ProgressHandler"/>.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SplitStringsOnSeparator(
        string separator, ProgressHandler progressHandler, CancellationToken cancellationToken)
    {
        if (separator.Length == 0)
        {
            return;
        }

        var items = GetItems().Select((item, index) => new { Item = item, Index = index }).ToArray();
        Logger.LogInformation("Splitting metadata on {Count} items", items.Length);

        progressHandler.SetProgressToInitial();
        foreach (var it in items)
        {
            it.Item.Genres = it.Item.Genres?.SeparateValues(separator) ?? it.Item.Genres;
            it.Item.Studios = it.Item.Studios?.SeparateValues(separator) ?? it.Item.Studios;
            it.Item.Tags = it.Item.Tags?.SeparateValues(separator) ?? it.Item.Tags;
            it.Item.ProductionLocations = it.Item.ProductionLocations?.SeparateValues(separator)
                                          ?? it.Item.ProductionLocations;

            await it.Item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            progressHandler.Progress(it.Index, items.Length);
        }

        progressHandler.SetProgressToFinal();

        var dropItems = GetSplittableItems()
            .Where(item => item.Name.Contains(separator, StringComparison.OrdinalIgnoreCase));
        foreach (var item in dropItems)
        {
            LibraryManager.DeleteItem(item, new DeleteOptions
            {
                DeleteFileLocation = false, DeleteFromExternalProvider = true
            });
        }
    }
}