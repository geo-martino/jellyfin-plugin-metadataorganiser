using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MetadataOrganiser.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to execute as a dry run.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to force processing all files.
    /// </summary>
    public bool Force { get; set; } = false;

    /// <summary>
    /// Gets or sets a value for the path of the JSON tag map file.
    /// </summary>
    public string TagMapPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating which stream tags to remove.
    /// </summary>
    public string DropStreamTags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating which stream tags to remove only if they contain the item name.
    /// </summary>
    public string DropStreamTagsOnItemName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value for the tag separator to use to split tag values up in Jellyfin's library.
    /// </summary>
    public string TagSeparator { get; set; } = string.Empty;
}
