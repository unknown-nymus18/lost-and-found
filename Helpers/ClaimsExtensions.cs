using System.Security.Claims;
using CampusLostAndFound.Models;

namespace CampusLostAndFound.Helpers;

public static class ClaimsExtensions
{
    public static bool IsLoggedIn(this ClaimsPrincipal? user) =>
        user?.Identity?.IsAuthenticated == true;

    public static int? UserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ?? "there";

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole(Roles.Admin);
}
