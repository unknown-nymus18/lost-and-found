using CampusLostAndFound.DTOs;
using CampusLostAndFound.Models;
using CampusLostAndFound.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers;

[ApiController]
[Route("api/matches")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MatchesController : ApiControllerBase
{
    private readonly MatchQueryService _matches;

    public MatchesController(MatchQueryService matches) => _matches = matches;

    /// <summary>Matches involving the authenticated user's reports.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<MatchDto>>> Mine() =>
        Ok(await _matches.ForUserAsync(CurrentUserId));

    /// <summary>All matches across the system (admin only).</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<List<MatchDto>>> All() =>
        Ok(await _matches.AllAsync());
}
