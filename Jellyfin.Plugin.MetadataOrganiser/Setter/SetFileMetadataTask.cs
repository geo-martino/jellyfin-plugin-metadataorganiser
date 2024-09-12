using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter;

/// <inheritdoc />
public class SetFileMetadataTask : MetadataTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaEncoder _encoder;
    private readonly ILogger<SetFileMetadataTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetFileMetadataTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="encoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{SetFileMetadataTask}"/> interface.</param>
    public SetFileMetadataTask(
        ILibraryManager libraryManager,
        IMediaEncoder encoder,
        ILogger<SetFileMetadataTask> logger)
    {
        _libraryManager = libraryManager;
        _encoder = encoder;
        _logger = logger;
    }

    /// <inheritdoc />
    public override string Name => "Set metadata to files";

    /// <inheritdoc />
    public override string Key => "SetFileMetadata";

    /// <inheritdoc />
    public override string Description => "Automatically handle assignment of metadata to files.";

    /// <inheritdoc />
    public override Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);
        ArgumentNullException.ThrowIfNull(MetadataOrganiserPlugin.Instance?.Configuration);

        var dryRun = MetadataOrganiserPlugin.Instance.Configuration.DryRun;
        var overwrite = MetadataOrganiserPlugin.Instance.Configuration.Overwrite;

        _logger.LogInformation("{DryRun} {Overwrite}", dryRun, overwrite);
        _logger.LogInformation("{EncoderPath} {ProbePath}", _encoder.EncoderPath, _encoder.ProbePath);

        progress.Report(100);
        return Task.CompletedTask;
    }
}