using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using Jellyfin.Plugin.MetadataOrganiser.Setter.Libraries;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter;

/// <inheritdoc />
public class SetShowMetadataTask : MetadataTask
{
    private readonly ShowLibraryProcessor _libraryProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetShowMetadataTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="encoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    public SetShowMetadataTask(
        ILibraryManager libraryManager,
        IMediaEncoder encoder,
        IConfigurationManager config,
        ILoggerFactory loggerFactory)
    {
        var loggerProcessor = loggerFactory.CreateLogger<ShowLibraryProcessor>();
        _libraryProcessor = new ShowLibraryProcessor(libraryManager, encoder, config, loggerProcessor);
    }

    /// <inheritdoc />
    public override string Name => "Set metadata to show files";

    /// <inheritdoc />
    public override string Key => "SetShowMetadata";

    /// <inheritdoc />
    public override string Description => "Automatically handle assignment of metadata to show related files.";

    /// <inheritdoc />
    public override async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);
        ArgumentNullException.ThrowIfNull(MetadataOrganiserPlugin.Instance?.Configuration);

        var dryRun = MetadataOrganiserPlugin.Instance.Configuration.DryRun;

        var progressHandler = new ProgressHandler(progress, 5, 95);
        await _libraryProcessor.SetMetadata(dryRun, progressHandler, cancellationToken).ConfigureAwait(false);

        progress.Report(100);
    }
}