using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Features.Auth;

namespace CampusConnect.Application.Tests.Features.Auth;

public sealed class InMemoryLoginRateLimiterTests
{
    private static readonly LoginRateLimitContext Context = new("alice@dhbw-loerrach.de", "127.0.0.1", "test-browser");

    [Fact]
    public void RegisterFailedAttempt_LocksOnFifthFailureWithinWindow()
    {
        var clock = new FakeTimeProvider();
        var limiter = new InMemoryLoginRateLimiter(clock);

        for (var attempt = 0; attempt < 4; attempt++)
            Assert.False(limiter.RegisterFailedAttempt(Context).IsLimited);

        var result = limiter.RegisterFailedAttempt(Context);

        Assert.True(result.IsLimited);
        Assert.Equal(clock.GetUtcNow().AddMinutes(1), result.LockedUntil);
    }

    [Fact]
    public void CheckAndEscalateIfLimited_IncreasesTemporaryLockoutUpToOneHour()
    {
        var clock = new FakeTimeProvider();
        var limiter = new InMemoryLoginRateLimiter(clock);

        for (var attempt = 0; attempt < 5; attempt++)
            limiter.RegisterFailedAttempt(Context);

        Assert.Equal(clock.GetUtcNow().AddMinutes(5), limiter.CheckAndEscalateIfLimited(Context).LockedUntil);
        Assert.Equal(clock.GetUtcNow().AddMinutes(15), limiter.CheckAndEscalateIfLimited(Context).LockedUntil);
        Assert.Equal(clock.GetUtcNow().AddMinutes(60), limiter.CheckAndEscalateIfLimited(Context).LockedUntil);
        Assert.Equal(clock.GetUtcNow().AddMinutes(60), limiter.CheckAndEscalateIfLimited(Context).LockedUntil);
    }

    [Fact]
    public void RegisterFailedAttempt_IgnoresAttemptsOutsideWindow()
    {
        var clock = new FakeTimeProvider();
        var limiter = new InMemoryLoginRateLimiter(clock);

        for (var attempt = 0; attempt < 4; attempt++)
            limiter.RegisterFailedAttempt(Context);

        clock.Advance(TimeSpan.FromMinutes(16));

        Assert.False(limiter.RegisterFailedAttempt(Context).IsLimited);
    }

    [Fact]
    public void Reset_ClearsFailureCountersAndLockout()
    {
        var clock = new FakeTimeProvider();
        var limiter = new InMemoryLoginRateLimiter(clock);

        for (var attempt = 0; attempt < 5; attempt++)
            limiter.RegisterFailedAttempt(Context);

        limiter.Reset(Context);

        Assert.False(limiter.CheckAndEscalateIfLimited(Context).IsLimited);
        for (var attempt = 0; attempt < 4; attempt++)
            Assert.False(limiter.RegisterFailedAttempt(Context).IsLimited);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 6, 23, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan timeSpan) =>
            _utcNow = _utcNow.Add(timeSpan);
    }
}
