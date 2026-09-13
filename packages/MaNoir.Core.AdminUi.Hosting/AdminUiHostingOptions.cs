using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.AdminUi.Hosting;

/// <summary>
/// Configures how an Admin UI is exposed behind a public reverse proxy path.
/// </summary>
public sealed class AdminUiHostingOptions
{
    /// <summary>
    /// Gets or sets the public path prefix used to expose the Admin UI, for example '/home-automation'.
    /// </summary>
    public string PublicBasePath { get; set; }

    /// <summary>
    /// Gets or sets the SPA directories hosted below the web root.
    /// </summary>
    public string[] SpaFolders { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the SPA directory used for root fallback requests.
    /// An empty value serves the web root index.html directly.
    /// </summary>
    public string DefaultSpaFolder { get; set; }

    /// <summary>
    /// Gets or sets an optional asynchronous resolver for the default SPA directory.
    /// </summary>
    public Func<CancellationToken, Task<string>> DefaultSpaFolderResolver { get; set; }
}