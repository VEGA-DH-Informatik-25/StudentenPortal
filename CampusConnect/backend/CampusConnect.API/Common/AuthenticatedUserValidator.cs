using CampusConnect.Domain.Interfaces;
using System.Security.Claims;

namespace CampusConnect.API.Common;

internal static class AuthenticatedUserValidator
{
    public static async Task<bool> IsActiveUserAsync(ClaimsPrincipal? principal, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (principal is null)
            return false;

        var userId = CurrentUser.GetUserId(principal);
        if (userId is null)
            return false;

        var userRepository = services.GetRequiredService<IUserRepository>();
        var user = await userRepository.FindByIdAsync(userId.Value, cancellationToken);
        return user is { IsActive: true };
    }
}
