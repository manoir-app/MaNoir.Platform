namespace MaNoir.Core.AdminUi.Hosting;

/// <summary>
/// Configures how the Core Admin UI host is exposed behind a public reverse proxy path.
/// </summary>
public sealed class AdminUiHostingOptions
{
    /// <summary>
    /// Gets or sets the public path prefix used to expose the Admin UI, for example '/home-automation'.
    /// </summary>
    public string PublicBasePath { get; set; }
}