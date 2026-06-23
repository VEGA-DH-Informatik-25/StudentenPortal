namespace CampusConnect.Application.Common.Interfaces;

public sealed record LoginRateLimitContext(string Account, string IpAddress, string Device);

public sealed record LoginRateLimitResult(bool IsLimited, DateTimeOffset? LockedUntil = null);

public interface ILoginRateLimiter
{
    LoginRateLimitResult CheckAndEscalateIfLimited(LoginRateLimitContext context);
    LoginRateLimitResult RegisterFailedAttempt(LoginRateLimitContext context);
    void Reset(LoginRateLimitContext context);
}
