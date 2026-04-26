using MaNoir.Core.Contracts.Models.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace MaNoir.Core.Files;

public static class FileStorageHelper
{
    private const string PrimaryRootEnvironmentVariableName = "MANOIR_FILE_STORAGE_FOLDER";
    private const string CompatibilityRootEnvironmentVariableName = "MANOIR_FILE_CACHE_FOLDER";
    private const string LegacyRootEnvironmentVariableName = "FILE_CACHE_FOLDER";
    private const string GeneralSpace = "general";
    private const string PublicSpace = "public";
    private const string UsersSpace = "users";
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new(JsonSerializerDefaults.Web);

    public static string GetGeneralFilePath(string scope, string relativePath)
    {
        return GetFilePath(GeneralSpace, scope, relativePath);
    }

    public static string GetPublicFilePath(string scope, string relativePath)
    {
        return GetFilePath(PublicSpace, scope, relativePath);
    }

    public static string GetUserFilePath(string userId, string scope, string relativePath)
    {
        string normalizedUserId = NormalizePathSegment(userId);
        if (normalizedUserId == null)
            return null;

        return GetFilePath(UsersSpace, normalizedUserId, scope, relativePath);
    }

    public static string GetRootPath()
    {
        string path = Environment.GetEnvironmentVariable(PrimaryRootEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(path))
            path = Environment.GetEnvironmentVariable(CompatibilityRootEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(path))
            path = Environment.GetEnvironmentVariable(LegacyRootEnvironmentVariableName);

        if (string.IsNullOrWhiteSpace(path))
        {
            path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "manoir",
                "files");
        }

        Directory.CreateDirectory(path);
        return path;
    }

    public static string GetContentType(string localFile)
    {
        string extension = Path.GetExtension(localFile)?.ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".json" => "application/json",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    public static StoredFileMetadata GetStoredFileMetadata(string localFile)
    {
        if (string.IsNullOrWhiteSpace(localFile) || !File.Exists(localFile))
            return null;

        string metadataFile = GetMetadataFilePath(localFile);
        if (File.Exists(metadataFile))
        {
            try
            {
                StoredFileMetadata storedMetadata = JsonSerializer.Deserialize<StoredFileMetadata>(File.ReadAllText(metadataFile), MetadataSerializerOptions);
                if (storedMetadata != null)
                {
                    storedMetadata.ContentType = NormalizeContentType(storedMetadata.ContentType) ?? GetContentType(localFile);
                    if (storedMetadata.Length > 0 && !string.IsNullOrWhiteSpace(storedMetadata.Sha256) && storedMetadata.LastModifiedUtc != default)
                        return storedMetadata;
                }
            }
            catch
            {
            }
        }

        return BuildMetadata(localFile, null);
    }

    public static StoredFileMetadata UpdateStoredFileMetadata(string localFile, string contentType)
    {
        if (string.IsNullOrWhiteSpace(localFile) || !File.Exists(localFile))
            return null;

        StoredFileMetadata metadata = BuildMetadata(localFile, contentType);
        string metadataFile = GetMetadataFilePath(localFile);
        string parentDirectory = Path.GetDirectoryName(metadataFile);
        if (!string.IsNullOrWhiteSpace(parentDirectory))
            Directory.CreateDirectory(parentDirectory);

        File.WriteAllText(metadataFile, JsonSerializer.Serialize(metadata, MetadataSerializerOptions));
        return metadata;
    }

    public static void DeleteStoredFileMetadata(string localFile)
    {
        string metadataFile = GetMetadataFilePath(localFile);
        if (File.Exists(metadataFile))
            File.Delete(metadataFile);
    }

    private static string GetFilePath(string space, params string[] pathSegments)
    {
        string normalizedSpace = NormalizePathSegment(space);
        if (normalizedSpace == null)
            return null;

        List<string> normalizedSegments = [GetRootPath(), normalizedSpace];
        for (int index = 0; index < pathSegments.Length; index++)
        {
            string pathSegment = pathSegments[index];
            bool isLast = index == pathSegments.Length - 1;
            string normalizedSegment = isLast
                ? NormalizeRelativePath(pathSegment)
                : NormalizePathSegment(pathSegment);

            if (normalizedSegment == null)
                return null;

            normalizedSegments.Add(normalizedSegment);
        }

        string filePath = Path.Combine([.. normalizedSegments]);
        string parentDirectory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(parentDirectory))
            return null;

        Directory.CreateDirectory(parentDirectory);
        return filePath;
    }

    private static StoredFileMetadata BuildMetadata(string localFile, string contentType)
    {
        FileInfo fileInfo = new(localFile);

        using FileStream stream = File.OpenRead(localFile);
        byte[] hash = SHA256.HashData(stream);

        return new StoredFileMetadata()
        {
            ContentType = NormalizeContentType(contentType) ?? GetContentType(localFile),
            Length = fileInfo.Length,
            Sha256 = Convert.ToHexString(hash),
            LastModifiedUtc = fileInfo.LastWriteTimeUtc
        };
    }

    private static string GetMetadataFilePath(string localFile)
    {
        return string.Concat(localFile, ".metadata.json");
    }

    private static string NormalizeContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return null;

        int separatorIndex = contentType.IndexOf(';');
        if (separatorIndex >= 0)
            contentType = contentType[..separatorIndex];

        contentType = contentType.Trim();
        return string.IsNullOrWhiteSpace(contentType) ? null : contentType;
    }

    private static string NormalizeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        string normalized = path.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        List<string> segments = [];
        foreach (string segment in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            string normalizedSegment = NormalizePathSegment(segment);
            if (normalizedSegment == null)
                return null;

            segments.Add(normalizedSegment);
        }

        return segments.Count == 0 ? null : Path.Combine([.. segments]);
    }

    private static string NormalizeFileName(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
            return null;

        if (file.Contains('/') || file.Contains('\\'))
            return null;

        return NormalizePathSegment(file);
    }

    private static string NormalizePathSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            return null;

        string normalized = segment.Trim();
        if (normalized == "." || normalized == "..")
            return null;

        if (normalized.Contains(':'))
            return null;

        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            if (normalized.Contains(invalidCharacter))
                return null;
        }

        return normalized;
    }
}

public static class FileCacheHelper
{
    public static string GetRootPath()
    {
        return FileStorageHelper.GetRootPath();
    }

    public static string GetLocalFolder(string scope, string folder)
    {
        string filePath = FileStorageHelper.GetGeneralFilePath(scope, string.Concat(folder?.Trim('/'), "/placeholder.tmp"));
        return filePath == null ? null : Path.GetDirectoryName(filePath);
    }

    public static string GetLocalFilename(string scope, string folder, string file)
    {
        string relativePath = string.IsNullOrWhiteSpace(folder) ? file : string.Concat(folder.Trim('/'), "/", file);
        return FileStorageHelper.GetGeneralFilePath(scope, relativePath);
    }

    public static string GetLocalFilename(string scope, string relativePath)
    {
        return FileStorageHelper.GetGeneralFilePath(scope, relativePath);
    }

    public static string GetContentType(string localFile)
    {
        return FileStorageHelper.GetContentType(localFile);
    }
}