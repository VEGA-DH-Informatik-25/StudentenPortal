using System.Security.Claims;

namespace CampusConnect.API.Common;

internal static class CurrentUser
{
    public static Guid? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
