using CampusLostAndFound.DTOs;
using CampusLostAndFound.Models;
using CampusLostAndFound.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ClaimsController : ApiControllerBase
{
    private readonly ClaimService _claims;

    public ClaimsController(ClaimService claims) => _claims = claims;

    /// <summary>Raise a claim against a found item with proof of ownership.</summary>
    [HttpPost]
    public async Task<ActionResult<ClaimDto>> Create(CreateClaimRequest req)
    {
        var result = await _claims.CreateAsync(CurrentUserId, req);
        if (!result.Succeeded) return BadRequest(new { error = result.Error });
        return Ok(ClaimService.Map(result.Claim!));
    }

    /// <summary>Claims raised by the authenticated user.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<ClaimDto>>> Mine() =>
        Ok(await _claims.ForUserAsync(CurrentUserId));

    /// <summary>All claims awaiting or past decision (admin only).</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<List<ClaimDto>>> All() =>
        Ok(await _claims.AllAsync());

    /// <summary>Approve or reject a claim (admin only).</summary>
    [HttpPost("{id:int}/decide")]
    [Authorize(Roles = Roles.Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ClaimDto>> Decide(int id, DecideClaimRequest req)
    {
        var result = await _claims.DecideAsync(id, req.Approve, req.Note);
        if (!result.Succeeded) return BadRequest(new { error = result.Error });
        return Ok(ClaimService.Map(result.Claim!));
    }
}
