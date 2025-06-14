using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using Jellyfin.Plugin.MetadataOrganiser.Setter.Libraries;
using Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter;

/// <inheritdoc />
public class SetShowMetadataTask : MetadataTask
{
    private readonly IApplicationPaths _appPaths;
    private readonly SeriesLibraryProcessor _seriesLibraryProcessor;
    private readonly SeasonLibraryProcessor _seasonLibraryProcessor;
    private readonly EpisodeLibraryProcessor _episodeLibraryProcessor;

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
        _appPaths = config.CommonApplicationPaths;

        _seriesLibraryProcessor = new SeriesLibraryProcessor(
            libraryManager,
            encoder,
            config,
            loggerFactory.CreateLogger<SeriesLibraryProcessor>(),
            loggerFactory.CreateLogger<SeriesTagExtractor>());

        _seasonLibraryProcessor = new SeasonLibraryProcessor(
            libraryManager,
            encoder,
            config,
            loggerFactory.CreateLogger<SeasonLibraryProcessor>(),
            loggerFactory.CreateLogger<SeasonTagExtractor>());

        _episodeLibraryProcessor = new EpisodeLibraryProcessor(
            libraryManager,
            encoder,
            config,
            loggerFactory.CreateLogger<EpisodeLibraryProcessor>(),
            loggerFactory.CreateLogger<EpisodeTagExtractor>());
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
        var force = MetadataOrganiserPlugin.Instance.Configuration.Force;

        var tagMapPath = MetadataOrganiserPlugin.Instance.Configuration.TagMapPath;
        if (tagMapPath.Length == 0)
        {
            tagMapPath = Path.Combine(_appPaths.ConfigurationDirectoryPath, "metadata_map.json");
        }

        var dropStreamTags = MetadataOrganiserPlugin.Instance.Configuration.DropStreamTags
            .SplitArguments().ToArray();
        var dropStreamTagsOnName = MetadataOrganiserPlugin.Instance.Configuration.DropStreamTagsOnItemName
            .SplitArguments().ToArray();

        await _episodeLibraryProcessor.SetMetadata(
            dropStreamTags,
            dropStreamTagsOnName,
            tagMapPath,
            dryRun,
            force,
            new ProgressHandler(progress, 0, 80),
            cancellationToken).ConfigureAwait(false);
        await _seasonLibraryProcessor.SetMetadata(
            dropStreamTags,
            dropStreamTagsOnName,
            tagMapPath,
            dryRun,
            force,
            new ProgressHandler(progress, 80, 90),
            cancellationToken).ConfigureAwait(false);
        await _seriesLibraryProcessor.SetMetadata(
            dropStreamTags,
            dropStreamTagsOnName,
            tagMapPath,
            dryRun,
            force,
            new ProgressHandler(progress, 90, 100),
            cancellationToken).ConfigureAwait(false);

        progress.Report(100);
    }
}