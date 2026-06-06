using MaNoir.Core.Contracts.Models.Locations;
using MaNoir.Core.Locations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/core/system/locations")]
public sealed class LocationsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Location>>> GetLocations()
    {
        return Ok(await new LocationLogic().GetAllAsync(HttpContext.RequestAborted));
    }

    [HttpGet("{locationId}")]
    public async Task<ActionResult<Location>> GetLocation(string locationId)
    {
        Location location = await new LocationLogic().GetByIdAsync(locationId, HttpContext.RequestAborted);
        return location == null ? NotFound() : Ok(location);
    }

    [HttpPut("{locationId}")]
    public async Task<ActionResult<Location>> PutLocation(string locationId, [FromBody] Location location)
    {
        string normalizedLocationId = LocationLogic.NormalizeLocationId(locationId);
        if (normalizedLocationId == null)
            return CreateValidationError("locationId", "The location identifier is required.");

        if (location == null)
            return CreateValidationError("body", "The location payload is required.");

        string payloadLocationId = LocationLogic.NormalizeLocationId(location.Id);
        if (payloadLocationId != null && payloadLocationId != normalizedLocationId)
            return CreateValidationError("id", "The payload identifier must match the route identifier.");

        location.Id = normalizedLocationId;
        Location savedLocation = await new LocationLogic().UpsertAsync(location, HttpContext.RequestAborted);
        return Ok(savedLocation);
    }

    private BadRequestObjectResult CreateValidationError(string fieldName, string message)
    {
        return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
        {
            [fieldName] = [message]
        }));
    }
}