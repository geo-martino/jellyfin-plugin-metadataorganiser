using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.MetadataOrganiser.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.MetadataOrganiser;

/// <summary>
/// The main plugin.
/// </summary>
public class MetadataOrganiserPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataOrganiserPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public MetadataOrganiserPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static MetadataOrganiserPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("68210EB1-7275-40F6-9568-DF276A9F9A3B");

    /// <inheritdoc />
    public override string Name => "Metadata Organiser";

    /// <inheritdoc />
    public override string Description => "Automatically handle assignment and reading of metadata to/from files.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }
}
