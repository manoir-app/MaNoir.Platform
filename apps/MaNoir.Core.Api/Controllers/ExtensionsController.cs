using MaNoir.Core.Api;
using MaNoir.Core.Contributions;
using MaNoir.Core.Contracts.Models.Contributions;
using Home.Common;
using Home.Common.Messages;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/core/system/extensions")]
public sealed class ExtensionsController : ControllerBase
{
    private readonly PluginRepositoryValidator _pluginRepositoryValidator;

    public ExtensionsController(PluginRepositoryValidator pluginRepositoryValidator)
    {
        _pluginRepositoryValidator = pluginRepositoryValidator;
    }

    [HttpGet("installed")]
    public async Task<ActionResult<List<InstalledPlugin>>> GetInstalledPlugins()
    {
        string currentUserId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        return Ok(await new ContributionLogic().GetInstalledPluginsAsync(HttpContext.RequestAborted));
    }

    [HttpGet("repositories/validate")]
    public async Task<ActionResult<PluginRepositoryValidation>> ValidateRepository([FromQuery] string repositoryUrl, CancellationToken cancellationToken)
    {
        string currentUserId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        return Ok(await _pluginRepositoryValidator.ValidateAsync(repositoryUrl, cancellationToken));
    }

    [HttpPost("install")]
    public async Task<ActionResult<PluginInstallationResponse>> InstallPlugin([FromBody] PluginInstallationRequest request, CancellationToken cancellationToken)
    {
        string currentUserId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        PluginRepositoryValidation validation = await _pluginRepositoryValidator.ValidateAsync(request?.RepositoryUrl, cancellationToken);
        if (!validation.CanInstall)
            return BadRequest(validation);

        MeshExtensionOperationMessage message = new MeshExtensionOperationMessage(MeshExtensionOperationMessage.TopicInstall)
        {
            RepositoryUrl = validation.RepositoryUrl,
            OperationId = Guid.NewGuid().ToString("N"),
            RequestedByUserId = currentUserId
        };

        try
        {
            PluginInstallationResponse response = await Task.Run(
                () => NatsInterprocess.Request<PluginInstallationResponse>(message.Topic, message, 5000),
                cancellationToken);
            return response?.Response == "accepted" ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
        catch (Exception exception) when (exception is NatsNoResponseException || exception is InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new PluginInstallationResponse()
            {
                Response = "fail",
                OperationId = message.OperationId,
                RepositoryUrl = message.RepositoryUrl,
                Status = "unavailable",
                Message = "Gaia could not be reached to start the installation."
            });
        }
    }
}

public sealed class PluginInstallationRequest
{
    public string RepositoryUrl { get; set; }
}