using System.Security.Claims;

namespace HealthCasePlatform.Api.Common;

public static class UserExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "unknown";
}
