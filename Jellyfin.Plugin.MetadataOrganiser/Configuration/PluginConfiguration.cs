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
}
