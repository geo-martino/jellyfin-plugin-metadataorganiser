using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Fixer;

/// <inheritdoc />
public class FixMetadataTask : MetadataTask
{
    private readonly LibraryProcessor _libraryProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixMetadataTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    public FixMetadataTask(
        ILibraryManager libraryManager,
        ILoggerFactory loggerFactory)
    {
        var loggerProcessor = loggerFactory.CreateLogger<LibraryProcessor>();

        _libraryProcessor = new LibraryProcessor(libraryManager, loggerProcessor);
    }

    /// <inheritdoc />
    public override string Name => "Fix library metadata";

    /// <inheritdoc />
    public override string Key => "FixMetadata";

    /// <inheritdoc />
    public override string Description => "Fix incorrectly parsed library metadata values.";

    /// <inheritdoc />
    public override async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);
        ArgumentNullException.ThrowIfNull(MetadataOrganiserPlugin.Instance?.Configuration);

        var separator = MetadataOrganiserPlugin.Instance.Configuration.TagSeparator;
        var progressHandler = new ProgressHandler(progress, 5, 100);

        await _libraryProcessor
            .SplitStringsOnSeparator(separator, progressHandler, cancellationToken)
            .ConfigureAwait(false);

        progress.Report(100);
    }
}