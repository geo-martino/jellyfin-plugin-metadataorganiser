using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter.Libraries;

/// <summary>
/// Processes metadata across a library containing items of type <typeparamref name="TItem"/>.
/// </summary>
/// <typeparam name="TItem">The <see cref="BaseItem"/> type that this processor can process.</typeparam>
/// <typeparam name="TExtractor">The <see cref="TagExtractor{TItem}"/> to process item tags.</typeparam>
public abstract class LibraryProcessor<TItem, TExtractor>
    where TItem : BaseItem
    where TExtractor : TagExtractor<TItem>
{
    private const string MetadataFolderName = "metadata";

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryProcessor{TItem,TExtractor}"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="encoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
    /// <param name="tagExtractor">Instance of the <see cref="ILogger{TagExtractor}"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{LibraryProcessor}"/> interface.</param>
    protected LibraryProcessor(
        ILibraryManager libraryManager,
        IMediaEncoder encoder,
        IConfigurationManager config,
        TExtractor tagExtractor,
        ILogger<LibraryProcessor<TItem, TExtractor>> logger)
    {
        LibraryManager = libraryManager;
        Encoder = encoder;
        Config = config;

        Extractor = tagExtractor;
        Logger = logger;
    }

    /// <summary>
    /// Gets the library manager.
    /// </summary>
    protected ILibraryManager LibraryManager { get; }

    /// <summary>
    /// Gets the library manager.
    /// </summary>
    private IMediaEncoder Encoder { get; }

    /// <summary>
    /// Gets the library manager.
    /// </summary>
    private IConfigurationManager Config { get; }

    /// <summary>
    /// Gets the library manager.
    /// </summary>
    private TExtractor Extractor { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    private ILogger<LibraryProcessor<TItem, TExtractor>> Logger { get; }

    /// <summary>
    /// Gets the items to process from the library.
    /// </summary>
    /// <returns>The items to process.</returns>
    protected abstract IEnumerable<TItem> GetItems();

    /// <summary>
    /// Extract and set metadata for all items in this library.
    /// </summary>
    /// <param name="dryRun">Whether to execute as a dry run, which does not modify any files.</param>
    /// <param name="progressHandler">Instance of the <see cref="ProgressHandler"/>.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SetMetadata(bool dryRun, ProgressHandler progressHandler, CancellationToken cancellationToken)
    {
        var items = GetItems().Take(5).Select((item, idx) => new { Item = item, Index = idx }).ToArray();
        progressHandler.SetProgressToInitial();

        foreach (var it in items)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            progressHandler.Progress(it.Index, items.Length);

            var tags = Extractor.FormatTags(it.Item).KeysToUpper();
            var streams = Extractor.GetStreamsToClean(it.Item);
            var transcodePath = await EncodeMetadata(it.Item, tags, streams, dryRun, cancellationToken)
                .ConfigureAwait(false);
            MoveFile(transcodePath, it.Item.Path, dryRun);
        }

        progressHandler.SetProgressToFinal();
    }

    /// <summary>
    /// Run the encoder and set the metadata.
    /// </summary>
    /// <param name="item">The item to encode.</param>
    /// <param name="tags">The tags to set.</param>
    /// <param name="streams">The streams containing metadata which needs to be cleaned.</param>
    /// <param name="dryRun">Whether to execute as a dry run, which does not modify any files.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> which returns the transcoded file's path.</returns>
    private async Task<string> EncodeMetadata(
        BaseItem item,
        IEnumerable<KeyValuePair<string, string>> tags,
        IEnumerable<MediaStream> streams,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var args = GenerateTranscodeArgs(item, tags, streams);
        var transcodePath = GetTranscodePath(item);

        var processStartInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            FileName = Encoder.EncoderPath,
            Arguments = string.Join(" ", args.Concat([$"\"{transcodePath}\""])),
            WindowStyle = ProcessWindowStyle.Hidden,
            ErrorDialog = false,
        };

        var logPrefix = dryRun ? "DRY RUN | " : string.Empty;

        var processDescription = string.Format(
            CultureInfo.InvariantCulture, "{0} {1}", processStartInfo.FileName, processStartInfo.Arguments);
        Logger.LogInformation(
            "{Prefix:l}Encoding metadata to file:\n{ProcessDescription}", logPrefix, processDescription);

        if (dryRun)
        {
            return transcodePath;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(transcodePath)!);

        var process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return transcodePath;
    }

    private IEnumerable<string> GenerateTranscodeArgs(
        BaseItem item,
        IEnumerable<KeyValuePair<string, string>> tags,
        IEnumerable<MediaStream> streams) => new List<string>
            {
                "-loglevel", "16", "-i", $"file:\"{item.Path}\"", "-map", "0", "-map_metadata:g", "-1"
            }
            .Concat(item.GetMediaStreams().SelectMany(GenerateStreamArg))
            .Concat(["-c", "copy"])
            .Concat(tags.SelectMany(GenerateTagArg))
            .Concat(streams.SelectMany(GenerateStreamCleanArg))
            .Concat(["-y"]);

    private string GetTranscodePath(BaseItem item) => Path.Combine(
        Config.GetTranscodePath(),
        MetadataFolderName,
        Path.GetRelativePath(item.GetTopParent().Path, item.Path));

    private IEnumerable<string> GenerateTagArg(KeyValuePair<string, string> pair) =>
    [
        "-metadata:g", $"\"{pair.Key}={pair.Value.Replace("\"", "\\\"", StringComparison.InvariantCultureIgnoreCase)}\""
    ];

    private IEnumerable<string> GenerateStreamArg(MediaStream stream) =>
    [
        $"-map_metadata:s:{stream.Index}", $"0:s:{stream.Index}"
    ];

    private IEnumerable<string> GenerateStreamCleanArg(MediaStream stream) =>
    [
        $"-metadata:s:{stream.Index}", $"\"title=\""
    ];

    private void MoveFile(string sourcePath, string targetPath, bool dryRun)
    {
        Logger.LogInformation("Moving file: {SourcePath} -> {TargetPath}", sourcePath, targetPath);
        if (dryRun)
        {
            return;
        }

        if (!File.Exists(sourcePath))
        {
            Logger.LogWarning("Could not find file: {SourcePath}", sourcePath);
            return;
        }

        try
        {
            File.Delete(targetPath);
            File.Move(sourcePath, targetPath);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to move file: {Source} -> {Target}", sourcePath, targetPath);
            throw;
        }
    }
}