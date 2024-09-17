using System.Collections.Generic;

namespace Jellyfin.Plugin.MetadataOrganiser.Setter.Tags;

/// <summary>
/// Stores the stream tags from a JSON response.
/// </summary>
public class StreamTags
{
    /// <summary>
    /// Gets or sets stream index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets stream tags.
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
}