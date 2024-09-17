using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
    /// Gets the media encoder.
    /// </summary>
    private IMediaEncoder Encoder { get; }

    /// <summary>
    /// Gets the configuration manager.
    /// </summary>
    private IConfigurationManager Config { get; }

    /// <summary>
    /// Gets the extractor.
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
    /// Gets the value for which tags should be removed if they contain this value.
    /// </summary>
    /// <param name="item">The item to extract a value from.</param>
    /// <returns>The items to process.</returns>
    protected abstract string GetTagDropValue(TItem item);

    /// <summary>
    /// Extract and set metadata for all items in this library.
    /// </summary>
    /// <param name="dropStreamTags">The stream tags to remove.</param>
    /// <param name="dropStreamTagsOnValue">The stream tags to remove if they contain the set item value.</param>
    /// <param name="tagMapPath">The path to a JSON file containing a tag map.</param>
    /// <param name="dryRun">Whether to execute as a dry run, which does not modify any files.</param>
    /// <param name="force">Force process all items, even if they have already been processed.</param>
    /// <param name="progressHandler">Instance of the <see cref="ProgressHandler"/>.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SetMetadata(
        IReadOnlyCollection<string> dropStreamTags,
        IReadOnlyCollection<string> dropStreamTagsOnValue,
        string tagMapPath,
        bool dryRun,
        bool force,
        ProgressHandler progressHandler,
        CancellationToken cancellationToken)
    {
        var items = GetItems().Select((item, idx) => new { Item = item, Index = idx }).ToArray();
        progressHandler.SetProgressToInitial();

        Logger.LogInformation("Removing all stream tags: {0:l}", string.Join(", ", dropStreamTags));
        Logger.LogInformation(
            "Removing stream tags which contain the item name from the following tag names: {0:l}",
            string.Join(", ", dropStreamTagsOnValue));

        var tagMap = Extractor.ReadTagMapping(tagMapPath);

        foreach (var it in items)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            progressHandler.Progress(it.Index, items.Length);
            if (!force && Extractor.ItemHasBeenProcessed(it.Item))
            {
                continue;
            }

            await SetMetadataOnItem(it.Item, dropStreamTags, dropStreamTagsOnValue, tagMap, dryRun, cancellationToken)
                .ConfigureAwait(false);
        }

        progressHandler.SetProgressToFinal();
    }

    private async Task SetMetadataOnItem(
        TItem item,
        IReadOnlyCollection<string> dropStreamTags,
        IReadOnlyCollection<string> dropStreamTagsOnValue,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> tagMap,
        bool dryRun,
        CancellationToken cancellationToken) => await SetMetadataOnItem(
        item,
        Extractor.FormatTags(item),
        dropStreamTags,
        dropStreamTagsOnValue,
        GetTagDropValue(item),
        tagMap,
        dryRun,
        cancellationToken).ConfigureAwait(false);

    private async Task SetMetadataOnItem(
        BaseItem item,
        IEnumerable<KeyValuePair<string, string>> formatTags,
        IReadOnlyCollection<string> dropStreamTags,
        IReadOnlyCollection<string> dropStreamTagsOnValue,
        string dropOnValue,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> tagMap,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var streamTags = Extractor
            .GetDropStreamTags(item, dropOnValue, dropStreamTagsOnValue)
            .Select(pair => new KeyValuePair<MediaStream, IEnumerable<KeyValuePair<string, string>>>(
                pair.Key, pair.Value.Concat(dropStreamTags.MapTagsToValue(string.Empty))))
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToDictionary().AsReadOnly()).AsReadOnly()
            .Concat(Extractor.GetMappedStreamTagValues(item, tagMap));

        var transcodePath = await EncodeMetadata(item, formatTags.KeysToUpper(), streamTags, dryRun, cancellationToken)
            .ConfigureAwait(false);
        MoveFile(transcodePath, item.Path, dryRun);

        foreach (var extra in item.GetExtras())
        {
            await SetMetadataOnItem(
                    extra,
                    Extractor.FormatTags(extra),
                    dropStreamTags,
                    dropStreamTagsOnValue,
                    dropOnValue,
                    tagMap,
                    dryRun,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var transcodeDirectory = GetTranscodeDirectory();
        if (Directory.Exists(transcodeDirectory))
        {
            Directory.Delete(transcodeDirectory, true);
        }
    }

    /// <summary>
    /// Run the encoder and set the metadata.
    /// </summary>
    /// <param name="item">The item to encode.</param>
    /// <param name="formatTags">The tags to set.</param>
    /// <param name="streamTags">The streams -> tags to set.</param>
    /// <param name="dryRun">Whether to execute as a dry run, which does not modify any files.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> which returns the transcoded file's path.</returns>
    private async Task<string> EncodeMetadata(
        BaseItem item,
        IEnumerable<KeyValuePair<string, string>> formatTags,
        IEnumerable<KeyValuePair<MediaStream, ReadOnlyDictionary<string, string>>> streamTags,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var args = GetTranscodeArgs(item, formatTags, streamTags);
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
            "{Prefix:l}Encoding metadata to file:\n{ProcessDescription:l}", logPrefix, processDescription);

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

    private IEnumerable<string> GetTranscodeArgs(
        BaseItem item,
        IEnumerable<KeyValuePair<string, string>> formatTags,
        IEnumerable<KeyValuePair<MediaStream, ReadOnlyDictionary<string, string>>> streamTags) =>
            new List<string>
                {
                    "-loglevel", "warning", "-i", $"file:\"{item.Path}\"", "-map", "0", "-map_metadata:g", "-1"
                }
                .Concat(item.GetMediaStreams().SelectMany(GetStreamMapArg))
                .Concat(["-c", "copy"])
                .Concat(formatTags.SelectMany(GetFormatTagArg))
                .Concat(streamTags.ToList()
                    .OrderBy(pair => pair.Key.Index)
                    .SelectMany(streamPair => GetStreamTagArg(streamPair.Key, streamPair.Value)))
                .Concat(["-y"]);

    private string GetTranscodeDirectory() => Path.Combine(Config.GetTranscodePath(), MetadataFolderName);

    private string GetTranscodePath(BaseItem item) => Path.Combine(
        GetTranscodeDirectory(), item.Id.ToString(), Path.GetFileName(item.Path));

    private IEnumerable<string> GetStreamMapArg(MediaStream stream) =>
    [
        $"-map_metadata:s:{stream.Index}", $"0:s:{stream.Index}"
    ];

    private IEnumerable<string> GetFormatTagArg(KeyValuePair<string, string> pair) =>
    [
        "-metadata:g", $"\"{pair.Key}={pair.Value.Replace("\"", "\\\"", StringComparison.InvariantCultureIgnoreCase)}\""
    ];

    private IEnumerable<string> GetStreamTagArg(MediaStream stream, IEnumerable<KeyValuePair<string, string>> tags) =>
        tags.SelectMany<KeyValuePair<string, string>, string>(
            pair => [$"-metadata:s:{stream.Index}", $"\"{pair.Key}={pair.Value}\""]);

    private void MoveFile(string sourcePath, string targetPath, bool dryRun)
    {
        var logPrefix = dryRun ? "DRY RUN | " : string.Empty;
        Logger.LogInformation("{Prefix:l}Moving file: {SourcePath} -> {TargetPath}", logPrefix, sourcePath, targetPath);
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