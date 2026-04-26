using System;

namespace MaNoir.Core.Contracts.Models.Files;

/// <summary>
/// Describes one stored file exposed by the Core file API.
/// </summary>
public sealed class StoredFileMetadata
{
    /// <summary>
    /// Gets or sets the resolved content type.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the file length in bytes.
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 digest as an uppercase hexadecimal string.
    /// </summary>
    public string Sha256 { get; set; }

    /// <summary>
    /// Gets or sets the last file update timestamp in UTC.
    /// </summary>
    public DateTimeOffset LastModifiedUtc { get; set; }
}