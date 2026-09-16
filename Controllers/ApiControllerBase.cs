using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("Missing user id claim."));
}
