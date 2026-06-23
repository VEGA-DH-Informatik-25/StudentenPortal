using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CampusConnect.Application.Common.Interfaces;

namespace CampusConnect.Application.Features.Auth;

public sealed class InMemoryLoginRateLimiter(TimeProvider? timeProvider = null) : ILoginRateLimiter
{
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan[] LockoutDurations =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(60)
    ];

    private const int FailureThreshold = 5;
    private readonly ConcurrentDictionary<string, LoginRateLimitBucket> _buckets = new();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public LoginRateLimitResult CheckAndEscalateIfLimited(LoginRateLimitContext context)
    {
        var now = _timeProvider.GetUtcNow();
        DateTimeOffset? lockedUntil = null;

        foreach (var key in BuildKeys(context))
        {
            if (!_buckets.TryGetValue(key, out var bucket))
                continue;

            lock (bucket)
            {
                bucket.Prune(now, AttemptWindow);

                if (!bucket.IsLocked(now))
                    continue;

                lockedUntil = Max(lockedUntil, bucket.ApplyNextLockout(now, LockoutDurations));
            }
        }

        return ToResult(lockedUntil);
    }

    public LoginRateLimitResult RegisterFailedAttempt(LoginRateLimitContext context)
    {
        var now = _timeProvider.GetUtcNow();
        DateTimeOffset? lockedUntil = null;

        foreach (var key in BuildKeys(context))
        {
            var bucket = _buckets.GetOrAdd(key, _ => new LoginRateLimitBucket());

            lock (bucket)
            {
                bucket.Prune(now, AttemptWindow);

                if (bucket.IsLocked(now))
                {
                    lockedUntil = Max(lockedUntil, bucket.ApplyNextLockout(now, LockoutDurations));
                    continue;
                }

                bucket.FailedAttempts.Enqueue(now);

                if (bucket.FailedAttempts.Count >= FailureThreshold)
                    lockedUntil = Max(lockedUntil, bucket.ApplyNextLockout(now, LockoutDurations));
            }
        }

        return ToResult(lockedUntil);
    }

    public void Reset(LoginRateLimitContext context)
    {
        foreach (var key in BuildKeys(context))
            _buckets.TryRemove(key, out _);
    }

    private static LoginRateLimitResult ToResult(DateTimeOffset? lockedUntil) =>
        lockedUntil is null
            ? new LoginRateLimitResult(false)
            : new LoginRateLimitResult(true, lockedUntil);

    private static DateTimeOffset? Max(DateTimeOffset? current, DateTimeOffset candidate) =>
        current is null || candidate > current.Value ? candidate : current;

    private static IEnumerable<string> BuildKeys(LoginRateLimitContext context)
    {
        var account = Normalize(context.Account);
        if (!string.IsNullOrWhiteSpace(account))
            yield return $"account:{Hash(account)}";

        yield return $"ip:{Hash(Normalize(context.IpAddress, "unknown-ip"))}";
        yield return $"device:{Hash(Normalize(context.Device, "unknown-device"))}";
    }

    private static string Normalize(string value, string fallback = "") =>
        string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToLowerInvariant();

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private sealed class LoginRateLimitBucket
    {
        public Queue<DateTimeOffset> FailedAttempts { get; } = new();
        private DateTimeOffset? LockedUntil { get; set; }
        private int LockoutLevel { get; set; }

        public bool IsLocked(DateTimeOffset now) => LockedUntil > now;

        public DateTimeOffset ApplyNextLockout(DateTimeOffset now, IReadOnlyList<TimeSpan> lockoutDurations)
        {
            var duration = lockoutDurations[Math.Min(LockoutLevel, lockoutDurations.Count - 1)];
            if (LockoutLevel < lockoutDurations.Count - 1)
                LockoutLevel++;

            LockedUntil = now.Add(duration);
            return LockedUntil.Value;
        }

        public void Prune(DateTimeOffset now, TimeSpan attemptWindow)
        {
            var cutoff = now.Subtract(attemptWindow);
            while (FailedAttempts.TryPeek(out var failedAt) && failedAt <= cutoff)
                FailedAttempts.Dequeue();
        }
    }
}
