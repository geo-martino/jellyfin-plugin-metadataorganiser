using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.MetadataOrganiser.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;

/// <summary>
/// Handles tag extraction from an item.
/// </summary>
/// <typeparam name="TItem">The <see cref="BaseItem"/> type that this extractor can process.</typeparam>
public abstract class TagExtractor<TItem>
    where TItem : BaseItem
{
    /// <summary>
    /// Gets the tag separator to use when joining a collection of tag values together as a string.
    /// </summary>
    protected const string TagArraySeparator = ",";

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TagExtractor{TItem}"/> class.
    /// </summary>
    /// <param name="encoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TagExtractor}"/> interface.</param>
    protected TagExtractor(IMediaEncoder encoder, ILogger<TagExtractor<TItem>> logger)
    {
        Encoder = encoder;
        Logger = logger;
    }

    /// <summary>
    /// Gets the media encoder.
    /// </summary>
    private IMediaEncoder Encoder { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    private ILogger<TagExtractor<TItem>> Logger { get; }

    /// <summary>
    /// Gets the tag to apply which indicates that this item has been processed for future runs.
    /// In order for the encoder to successfully assign this tag to all file types,
    /// the key of this tag must be a generic tag field e.g. comment, title etc.
    /// </summary>
    private KeyValuePair<string, string?> ProcessedTag => new("comment", "Processed by Jellyfin");

    /// <summary>
    /// Get the tags to assign to an item.
    /// </summary>
    /// <param name="item">The item to extract tags for.</param>
    /// <returns>The map of key-value pairs for tags to assign to an item.</returns>
    public abstract IEnumerable<KeyValuePair<string, string>> FormatTags(TItem item);

    /// <summary>
    /// Gets the core set of tags that apply to all <see cref="BaseItem"/> types.
    /// </summary>
    /// <param name="item">The item to extract tags for.</param>
    /// <returns>The map of key-value pairs for tags to assign to an item.</returns>
    public IEnumerable<KeyValuePair<string, string>> FormatTags(BaseItem item) => new Dictionary<string, string?>
    {
        { "title", item.Name },
        { "album", item.Album },
        { "genre", string.Join(TagArraySeparator, item.Genres) },
        { "rating", (item.CriticRating / 20)?.ToString(CultureInfo.InvariantCulture) },
        { "keywords", string.Join(TagArraySeparator, item.Tags) }
    }.Append(ProcessedTag).FilterNotNullOrEmpty();

    /// <summary>
    /// Generate a show's series tags.
    /// </summary>
    /// <param name="item">The series to generate tags for.</param>
    /// <returns>The generated tags.</returns>
    protected static IEnumerable<KeyValuePair<string, string?>> FormatSeriesTags(Series item) =>
    [
        new("show", item.Name)
    ];

    /// <summary>
    /// Generate a season's series tags.
    /// </summary>
    /// <param name="item">The season to generate tags for.</param>
    /// <returns>The generated tags.</returns>
    protected static IEnumerable<KeyValuePair<string, string?>> FormatSeriesTags(Season item) =>
        FormatSeriesTags(item.Series);

    /// <summary>
    /// Generate an episode's series tags.
    /// </summary>
    /// <param name="item">The episode to generate tags for.</param>
    /// <returns>The generated tags.</returns>
    protected static IEnumerable<KeyValuePair<string, string?>> FormatSeriesTags(Episode item) =>
        FormatSeriesTags(item.Series);

    /// <summary>
    /// Generate a season's tags.
    /// </summary>
    /// <param name="item">The season to generate tags for.</param>
    /// <returns>The generated tags.</returns>
    protected static IEnumerable<KeyValuePair<string, string?>> FormatSeasonTags(Season item) =>
    [
        new("season_number", item.IndexNumber?.ToString(CultureInfo.InvariantCulture)),
        new("season_total", item.Series?.Children?.OfType<Season>()
            .Max(season => season.IndexNumber)?.ToString(CultureInfo.InvariantCulture) ?? "0")
    ];

    /// <summary>
    /// Generate an episode's season tags.
    /// </summary>
    /// <param name="item">The episode to generate tags for.</param>
    /// <returns>The generated tags.</returns>
    protected static IEnumerable<KeyValuePair<string, string?>> FormatSeasonTags(Episode item) =>
        FormatSeasonTags(item.Season);

    /// <summary>
    /// Generate an episode's tags.
    /// </summary>
    /// <param name="item">The episode to generate tags for.</param>
    /// <returns>The generated tags.</returns>
    protected static IEnumerable<KeyValuePair<string, string?>> FormatEpisodeTags(Episode item) =>
    [
        new("episode_number", item.IndexNumber?.ToString(CultureInfo.InvariantCulture)),
        new("episode_total", item.Season?.Children?.OfType<Episode>()
            .Max(episode => episode.IndexNumber)?.ToString(CultureInfo.InvariantCulture) ?? "0")
    ];

    /// <summary>
    /// Gets a map of format tags to be replaced in the given item.
    /// </summary>
    /// <param name="item">The item with tags to remap.</param>
    /// <param name="mapping">
    /// Mapping of the tag values to replace in the form:
    /// {
    ///   "TAG FIELD": {
    ///     "OLD VALUE": "NEW VALUE",
    ///     ...
    ///   },
    /// ...
    /// }.</param>
    /// <returns>A map of the new tags to apply.</returns>
    public IEnumerable<KeyValuePair<string, string>> GetMappedFormatTagValues(
        BaseItem item,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> mapping)
    {
        var formatTags = ExtractFormatTagsFromFile(item);
        var remappedTags = GetMappedTagValues(formatTags, mapping);

        return remappedTags;
    }

    /// <summary>
    /// Gets a map of stream tags to be replaced in the given item.
    /// </summary>
    /// <param name="item">The item with media stream tags to remap.</param>
    /// <param name="mapping">
    /// Mapping of the tag values to replace in the form:
    /// {
    ///   "TAG FIELD": {
    ///     "OLD VALUE": "NEW VALUE",
    ///     ...
    ///   },
    /// ...
    /// }.</param>
    /// <returns>A map of the new tags to apply per stream.</returns>
    public ReadOnlyDictionary<MediaStream, ReadOnlyDictionary<string, string>> GetMappedStreamTagValues(
        BaseItem item,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> mapping)
    {
        var streamTags = ExtractStreamTagsFromFile(item);
        var remappedTags = item.GetMediaStreams()
            .Select(stream => new KeyValuePair<MediaStream, IEnumerable<KeyValuePair<string, string>>?>(
                stream, streamTags?.First(tags => tags.Index == stream.Index).Tags))
            .Select(pair => new KeyValuePair<MediaStream, IEnumerable<KeyValuePair<string, string>>>(
                pair.Key, GetMappedTagValues(pair.Value, mapping)))
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToDictionary().AsReadOnly()).AsReadOnly();

        return remappedTags;
    }

    private IEnumerable<KeyValuePair<string, string>> GetMappedTagValues(
        IEnumerable<KeyValuePair<string, string>>? tags,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> mapping) => tags?
        .Select(pair => new KeyValuePair<string, string?>(
            pair.Key, GetMappedTagValue(pair.Value, mapping.GetValueCaseInsensitive(pair.Key))))
        .Where(pair => pair.Value != null)
        .OfType<KeyValuePair<string, string>>() ?? [];

    private string? GetMappedTagValue(
        string tagValue, IReadOnlyCollection<KeyValuePair<string, string>>? mapping) =>
        mapping?.GetValueCaseInsensitive(tagValue);

    /// <summary>
    /// Gets the media streams indices containing metadata which need to be dropped.
    /// </summary>
    /// <param name="item">The item with media streams to check.</param>
    /// <param name="onValue">The value to match on.</param>
    /// <param name="dropStreamTagsOnName">The tags to check.</param>
    /// <returns>The stream -> list of tags to be dropped.</returns>
    public ReadOnlyDictionary<MediaStream, ReadOnlyDictionary<string, string>> GetDropStreamTags(
        BaseItem item, string onValue, IEnumerable<string>? dropStreamTagsOnName = null)
    {
        if (dropStreamTagsOnName == null)
        {
            return new Dictionary<MediaStream, ReadOnlyDictionary<string, string>>().AsReadOnly();
        }

        var streamTags = ExtractStreamTagsFromFile(item);
        var streams = item.GetMediaStreams()
            .Select(stream => new KeyValuePair<MediaStream, IReadOnlyDictionary<string, string>?>(
                stream, streamTags?.First(tags => tags.Index == stream.Index).Tags))
            .OfType<KeyValuePair<MediaStream, IReadOnlyDictionary<string, string>>>()
            .Select(pair => new KeyValuePair<MediaStream, IEnumerable<KeyValuePair<string, string>>>(
                pair.Key, MatchTagsToValue(pair.Value, onValue, dropStreamTagsOnName, true)
                    .MapTagsToValue(string.Empty)))
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToDictionary().AsReadOnly()).AsReadOnly();

        return streams;
    }

    /// <summary>
    /// Check whether an item has been processed by checking whether it has been tagged as having been processed.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>Bool indicating whether the item has been processed.</returns>
    public bool ItemHasBeenProcessed(BaseItem item) => ExtractFormatTagsFromFile(item)?
        .FirstOrDefault(pair => pair.Key.Equals(ProcessedTag.Key, StringComparison.OrdinalIgnoreCase))
        .Value == ProcessedTag.Value;

    private Dictionary<string, string>? ExtractFormatTagsFromFile(BaseItem item)
    {
        var output = ExtractEntriesFromFile(item, "format_tags");
        var deserialized = JsonSerializer.Deserialize<
            Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(output, _jsonSerializerOptions);
        return deserialized?.GetValueOrDefault("format")?.GetValueOrDefault("tags");
    }

    private List<StreamTags>? ExtractStreamTagsFromFile(BaseItem item)
    {
        var output = ExtractEntriesFromFile(item, "stream=index,specifier : stream_tags");
        var deserialized = JsonSerializer.Deserialize<
            Dictionary<string, List<StreamTags>>>(output, _jsonSerializerOptions);
        return deserialized?.GetValueOrDefault("streams");
    }

    private string ExtractEntriesFromFile(BaseItem item, string entries)
    {
        string[] args =
        [
            "-analyzeduration",
            "200M",
            "-probesize",
            "1G",
            "-i",
            $"\"file:{item.Path}\"",
            "-threads",
            "0",
            "-v",
            "warning",
            "-show_entries",
            $"\"{entries}\"",
            "-print_format",
            "json=compact=1"
        ];

        var processStartInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            FileName = Encoder.ProbePath,
            Arguments = string.Join(" ", args),
            WindowStyle = ProcessWindowStyle.Hidden,
            ErrorDialog = false,
        };

        var process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true,
        };

        var outputBuilder = new StringBuilder();
        process.OutputDataReceived += delegate(object _, DataReceivedEventArgs e)
        {
            outputBuilder.Append(e.Data);
        };

        var processDescription = string.Format(
            CultureInfo.InvariantCulture, "{0} {1}", processStartInfo.FileName, processStartInfo.Arguments);
        Logger.LogInformation("Probing file:\n{ProcessDescription:l}", processDescription);

        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
        process.CancelOutputRead();
        process.Close();

        var output = outputBuilder.ToString();

        return output;
    }

    private IEnumerable<string> MatchTagsToValue(
        IReadOnlyDictionary<string, string> tags, string value, IEnumerable<string> fields, bool clean = false) =>
        fields.Where(field => MatchTagToValue(tags, value, field, clean));

    private bool MatchTagToValue(IReadOnlyDictionary<string, string> tags, string value, string field, bool clean = false)
    {
        if (tags.Count == 0)
        {
            return false;
        }

        var actualValue = clean ? CleanTagValue(value) : value;
        var fieldValue = tags.GetValueCaseInsensitive(field);
        fieldValue = clean && fieldValue != null ? CleanTagValue(fieldValue) : fieldValue;

        return actualValue is { Length: > 0 } && fieldValue is { Length: > 0 } && fieldValue
            .Contains(actualValue, StringComparison.InvariantCultureIgnoreCase);
    }

    private string CleanTagValue(string value)
    {
        value = string.Join(string.Empty, value.Split("()?:<>'\"".ToArray()));
        value = string.Join(' ', value.Split("-_.|".ToArray()));
        value = value.Replace("&", "and", StringComparison.OrdinalIgnoreCase);
        return value;
    }

    /// <summary>
    /// Read the tag mapping from a JSON file.
    /// </summary>
    /// <param name="path">The path to the JSON file.</param>
    /// <returns>The deserialized tag mapping.</returns>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ReadTagMapping(string path)
    {
        if (!File.Exists(path))
        {
            Logger.LogWarning("Could not find mapping file at {Path}", path);
            return new Dictionary<string, IReadOnlyDictionary<string, string>>();
        }

        Logger.LogInformation("Loading mapping file from {Path}", path);
        string json;
        try
        {
            var streamReader = new StreamReader(path);
            json = streamReader.ReadToEnd();
        }
        catch (UnauthorizedAccessException e)
        {
            Logger.LogError(e, "Insufficient permissions to load {Path}", path);
            return new Dictionary<string, IReadOnlyDictionary<string, string>>();
        }

        try
        {
            var deserialized =
                JsonSerializer.Deserialize<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>(
                    json, _jsonSerializerOptions);
            return deserialized ?? new Dictionary<string, IReadOnlyDictionary<string, string>>();
        }
        catch (JsonException e)
        {
            Logger.LogError(e, "Could not load JSON file {Path}", path);
        }

        return new Dictionary<string, IReadOnlyDictionary<string, string>>();
    }
}