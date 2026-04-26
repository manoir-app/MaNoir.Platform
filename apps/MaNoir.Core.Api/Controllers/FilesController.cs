using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Files;
using MaNoir.Core.Files;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[Route("api/core/files")]
public sealed class FilesController : ControllerBase
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("general/{scope}/{**relativePath}")]
    public IActionResult GetGeneralFile(string scope, string relativePath)
    {
        return GetFileCore(FileStorageHelper.GetGeneralFilePath(scope, relativePath));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("general/{scope}/metadata/{**relativePath}")]
    public ActionResult<StoredFileMetadata> GetGeneralFileMetadata(string scope, string relativePath)
    {
        return GetMetadataCore(FileStorageHelper.GetGeneralFilePath(scope, relativePath));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("general/{scope}/{**relativePath}")]
    public async Task<IActionResult> PutGeneralFile(string scope, string relativePath, [FromQuery] string contentType = null, [FromQuery] string sha256 = null, [FromQuery] long? length = null)
    {
        await EnsureCurrentUserAccessAsync(CoreAccessZones.CoreGeneralFilesWrite, AccessLevel.Contribute);
        return await PutFileCoreAsync(FileStorageHelper.GetGeneralFilePath(scope, relativePath), contentType, sha256, length);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpDelete("general/{scope}/{**relativePath}")]
    public async Task<IActionResult> DeleteGeneralFile(string scope, string relativePath)
    {
        await EnsureCurrentUserAccessAsync(CoreAccessZones.CoreGeneralFilesWrite, AccessLevel.Contribute);
        return DeleteFileCore(FileStorageHelper.GetGeneralFilePath(scope, relativePath));
    }

    [AllowAnonymous]
    [HttpGet("public/{scope}/{**relativePath}")]
    public IActionResult GetPublicFile(string scope, string relativePath)
    {
        return GetFileCore(FileStorageHelper.GetPublicFilePath(scope, relativePath));
    }

    [AllowAnonymous]
    [HttpGet("public/{scope}/metadata/{**relativePath}")]
    public ActionResult<StoredFileMetadata> GetPublicFileMetadata(string scope, string relativePath)
    {
        return GetMetadataCore(FileStorageHelper.GetPublicFilePath(scope, relativePath));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("public/{scope}/{**relativePath}")]
    public async Task<IActionResult> PutPublicFile(string scope, string relativePath, [FromQuery] string contentType = null, [FromQuery] string sha256 = null, [FromQuery] long? length = null)
    {
        await EnsureCurrentUserAccessAsync(CoreAccessZones.CorePublicFilesWrite, AccessLevel.Contribute);
        return await PutFileCoreAsync(FileStorageHelper.GetPublicFilePath(scope, relativePath), contentType, sha256, length);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpDelete("public/{scope}/{**relativePath}")]
    public async Task<IActionResult> DeletePublicFile(string scope, string relativePath)
    {
        await EnsureCurrentUserAccessAsync(CoreAccessZones.CorePublicFilesWrite, AccessLevel.Contribute);
        return DeleteFileCore(FileStorageHelper.GetPublicFilePath(scope, relativePath));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("users/me/{scope}/{**relativePath}")]
    public IActionResult GetCurrentUserFile(string scope, string relativePath)
    {
        string userId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        return GetFileCore(FileStorageHelper.GetUserFilePath(userId, scope, relativePath));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("users/me/{scope}/metadata/{**relativePath}")]
    public ActionResult<StoredFileMetadata> GetCurrentUserFileMetadata(string scope, string relativePath)
    {
        string userId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        return GetMetadataCore(FileStorageHelper.GetUserFilePath(userId, scope, relativePath));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("users/me/{scope}/{**relativePath}")]
    public async Task<IActionResult> PutCurrentUserFile(string scope, string relativePath, [FromQuery] string contentType = null, [FromQuery] string sha256 = null, [FromQuery] long? length = null)
    {
        string userId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        return await PutFileCoreAsync(FileStorageHelper.GetUserFilePath(userId, scope, relativePath), contentType, sha256, length);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpDelete("users/me/{scope}/{**relativePath}")]
    public IActionResult DeleteCurrentUserFile(string scope, string relativePath)
    {
        string userId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        return DeleteFileCore(FileStorageHelper.GetUserFilePath(userId, scope, relativePath));
    }

    private IActionResult GetFileCore(string localFile)
    {
        if (localFile == null)
            return CreateInvalidPathResponse();

        if (!System.IO.File.Exists(localFile))
            return NotFound();

        StoredFileMetadata metadata = FileStorageHelper.GetStoredFileMetadata(localFile);
        return PhysicalFile(localFile, metadata?.ContentType ?? FileStorageHelper.GetContentType(localFile));
    }

    private ActionResult<StoredFileMetadata> GetMetadataCore(string localFile)
    {
        if (localFile == null)
            return CreateInvalidPathResponse();

        if (!System.IO.File.Exists(localFile))
            return NotFound();

        return Ok(FileStorageHelper.GetStoredFileMetadata(localFile));
    }

    private async Task<IActionResult> PutFileCoreAsync(string localFile, string declaredContentType, string declaredSha256, long? declaredLength)
    {
        if (localFile == null)
            return CreateInvalidPathResponse();

        if (Request.ContentLength is null or <= 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
            {
                ["content"] = ["The request body must contain file content."]
            }));
        }

        string requestContentType = NormalizeContentType(Request.ContentType);
        string expectedContentType = NormalizeContentType(declaredContentType);
        if (expectedContentType != null && requestContentType != null && !string.Equals(expectedContentType, requestContentType, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
            {
                ["contentType"] = ["The declared content type does not match the request content type."]
            }));
        }

        await using (FileStream stream = new(localFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await Request.Body.CopyToAsync(stream, HttpContext.RequestAborted);
        }

        StoredFileMetadata metadata = FileStorageHelper.UpdateStoredFileMetadata(localFile, expectedContentType ?? requestContentType);
        if (declaredLength.HasValue && metadata.Length != declaredLength.Value)
        {
            DeleteLocalFileArtifacts(localFile);
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
            {
                ["length"] = ["The declared file length does not match the stored file length."]
            }));
        }

        string normalizedDeclaredSha256 = NormalizeSha256(declaredSha256);
        if (normalizedDeclaredSha256 != null && !string.Equals(normalizedDeclaredSha256, metadata.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            DeleteLocalFileArtifacts(localFile);
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
            {
                ["sha256"] = ["The declared SHA-256 digest does not match the stored file digest."]
            }));
        }

        return NoContent();
    }

    private IActionResult DeleteFileCore(string localFile)
    {
        if (localFile == null)
            return CreateInvalidPathResponse();

        if (!System.IO.File.Exists(localFile))
            return NotFound();

        DeleteLocalFileArtifacts(localFile);
        return NoContent();
    }

    private void DeleteLocalFileArtifacts(string localFile)
    {
        if (System.IO.File.Exists(localFile))
            System.IO.File.Delete(localFile);

        FileStorageHelper.DeleteStoredFileMetadata(localFile);
    }

    private async Task EnsureCurrentUserAccessAsync(string zoneId, AccessLevel requiredLevel)
    {
        string currentUserId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new UnauthorizedAccessException("An authenticated user is required.");

        await new AuthorizationLogic().EnsureAccessAsync(currentUserId, zoneId, requiredLevel, HttpContext.RequestAborted);
    }

    private BadRequestObjectResult CreateInvalidPathResponse()
    {
        return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
        {
            ["path"] = ["The file scope or relative path is invalid."]
        }));
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

    private static string NormalizeSha256(string sha256)
    {
        if (string.IsNullOrWhiteSpace(sha256))
            return null;

        return sha256.Trim().ToUpperInvariant();
    }
}