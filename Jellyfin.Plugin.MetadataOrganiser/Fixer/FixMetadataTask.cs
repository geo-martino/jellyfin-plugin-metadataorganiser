using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Fixer;

/// <inheritdoc />
public class FixMetadataTask : MetadataTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaEncoder _encoder;
    private readonly ILogger<FixMetadataTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixMetadataTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="encoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{FixMetadataTask}"/> interface.</param>
    public FixMetadataTask(
        ILibraryManager libraryManager,
        IMediaEncoder encoder,
        ILogger<FixMetadataTask> logger)
    {
        _libraryManager = libraryManager;
        _encoder = encoder;
        _logger = logger;
    }

    /// <inheritdoc />
    public override string Name => "Fix metadata in Jellyfin's library";

    /// <inheritdoc />
    public override string Key => "FixMetadata";

    /// <inheritdoc />
    public override string Description => "Fix metadata in Jellyfin's library incorrectly read from files.";

    /// <inheritdoc />
    public override Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);
        ArgumentNullException.ThrowIfNull(MetadataOrganiserPlugin.Instance?.Configuration);

        var dryRun = MetadataOrganiserPlugin.Instance.Configuration.DryRun;

        _logger.LogInformation("{DryRun}", dryRun);
        _logger.LogInformation("{EncoderPath} {ProbePath}", _encoder.EncoderPath, _encoder.ProbePath);

        progress.Report(100);
        return Task.CompletedTask;
    }
}