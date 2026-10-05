using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("Missing user id claim."));

    /// <summary>The signed-in user's ID on endpoints that also allow anonymous callers.</summary>
    protected int? CurrentUserIdOrNull =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
