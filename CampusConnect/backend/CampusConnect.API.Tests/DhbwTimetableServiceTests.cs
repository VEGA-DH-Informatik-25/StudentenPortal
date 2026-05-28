using System.Net;
using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Infrastructure;
using CampusConnect.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CampusConnect.API.Tests;

public sealed class DhbwTimetableServiceTests
{
    [Fact]
    public async Task GetTimetableAsync_UsesConfiguredTemplateAndCourseAlias()
    {
        Uri? requestedUri = null;
        using var httpClient = new HttpClient(new StubHttpHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("BEGIN:VCALENDAR\r\nEND:VCALENDAR")
            };
        }));
        using var cache = CreateCache();
        var service = new DhbwTimetableService(
            httpClient,
            Options.Create(new DhbwTimetableOptions
            {
                CalendarUrlTemplate = "https://calendar.example.test/{course}/calendar.ics",
                CourseAliases = new Dictionary<string, string>
                {
                    ["WWI25A"] = "WWI25A-AM"
                }
            }),
            cache);

        var timetable = await service.GetTimetableAsync("wwi25a", 30);

        Assert.Equal("https://calendar.example.test/wwi25a-am/calendar.ics", requestedUri?.ToString());
        Assert.Equal("WWI25A", timetable.Course);
        Assert.Empty(timetable.Days);
    }

    [Fact]
    public async Task GetTimetableAsync_IncludesPastDaysFromCurrentWeek()
    {
        using var httpClient = new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                BEGIN:VCALENDAR
                BEGIN:VEVENT
                UID:past-day
                DTSTART;TZID=Europe/Berlin:20260511T090000
                DTEND;TZID=Europe/Berlin:20260511T103000
                SUMMARY:Mathematics
                LOCATION:R101
                END:VEVENT
                BEGIN:VEVENT
                UID:today
                DTSTART;TZID=Europe/Berlin:20260513T110000
                DTEND;TZID=Europe/Berlin:20260513T123000
                SUMMARY:Software Engineering
                LOCATION:R102
                END:VEVENT
                END:VCALENDAR
                """)
        }));
        using var cache = CreateCache();
        var service = new DhbwTimetableService(
            httpClient,
            Options.Create(new DhbwTimetableOptions
            {
                CalendarUrlTemplate = "https://calendar.example.test/{course}/calendar.ics"
            }),
            cache,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero)));

        var timetable = await service.GetTimetableAsync("tif25a", 30);

        Assert.Collection(
            timetable.Days,
            day =>
            {
                Assert.Equal(new DateOnly(2026, 5, 11), day.Date);
                Assert.Equal("Mathematics", day.Events.Single().Title);
            },
            day =>
            {
                Assert.Equal(new DateOnly(2026, 5, 13), day.Date);
                Assert.Equal("Software Engineering", day.Events.Single().Title);
            });
    }

    [Fact]
    public async Task GetTimetableAsync_UsesExplicitPastRangeStart()
    {
        using var httpClient = new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                BEGIN:VCALENDAR
                BEGIN:VEVENT
                UID:previous-week
                DTSTART;TZID=Europe/Berlin:20260505T090000
                DTEND;TZID=Europe/Berlin:20260505T103000
                SUMMARY:Databases
                LOCATION:R201
                END:VEVENT
                BEGIN:VEVENT
                UID:current-week
                DTSTART;TZID=Europe/Berlin:20260511T090000
                DTEND;TZID=Europe/Berlin:20260511T103000
                SUMMARY:Mathematics
                LOCATION:R101
                END:VEVENT
                END:VCALENDAR
                """)
        }));
        using var cache = CreateCache();
        var service = new DhbwTimetableService(
            httpClient,
            Options.Create(new DhbwTimetableOptions
            {
                CalendarUrlTemplate = "https://calendar.example.test/{course}/calendar.ics"
            }),
            cache,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero)));

        var timetable = await service.GetTimetableAsync("tif25a", 6, new DateOnly(2026, 5, 4));

        var day = Assert.Single(timetable.Days);
        Assert.Equal(new DateOnly(2026, 5, 5), day.Date);
        Assert.Equal("Databases", day.Events.Single().Title);
    }

    [Fact]
    public async Task GetTimetableAsync_UsesFreshCachedIcalWithoutSecondRequest()
    {
        var requestCount = 0;
        using var httpClient = new HttpClient(new StubHttpHandler(_ =>
        {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SingleEventIcal("Mathematics"))
            };
        }));
        using var cache = CreateCache();
        var service = new DhbwTimetableService(
            httpClient,
            Options.Create(new DhbwTimetableOptions
            {
                CalendarUrlTemplate = "https://calendar.example.test/{course}/calendar.ics",
                CacheMinutes = 60,
                StaleCacheMinutes = 120
            }),
            cache,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero)));

        var first = await service.GetTimetableAsync("tif25a", 30);
        var second = await service.GetTimetableAsync("tif25a", 30);

        Assert.Equal(1, requestCount);
        Assert.Equal("Mathematics", first.Days.Single().Events.Single().Title);
        Assert.Equal("Mathematics", second.Days.Single().Events.Single().Title);
    }

    [Fact]
    public async Task GetTimetableAsync_ReturnsStaleCachedIcalWhenUpstreamFails()
    {
        var requestCount = 0;
        using var httpClient = new HttpClient(new StubHttpHandler(_ =>
        {
            requestCount++;
            return requestCount == 1
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SingleEventIcal("Mathematics"))
                }
                : new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }));
        using var cache = CreateCache();
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero));
        var service = new DhbwTimetableService(
            httpClient,
            Options.Create(new DhbwTimetableOptions
            {
                CalendarUrlTemplate = "https://calendar.example.test/{course}/calendar.ics",
                CacheMinutes = 1,
                StaleCacheMinutes = 60
            }),
            cache,
            timeProvider);

        await service.GetTimetableAsync("tif25a", 30);
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 5, 13, 8, 2, 0, TimeSpan.Zero));

        var fallback = await service.GetTimetableAsync("tif25a", 30);

        Assert.Equal(2, requestCount);
        Assert.Equal("Mathematics", fallback.Days.Single().Events.Single().Title);
    }

    [Fact]
    public void AddInfrastructure_ResolvesTimetableServiceTypedHttpClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CampusConnect"] = "Data Source=:memory:",
                ["Timetable:CalendarUrlTemplate"] = "https://calendar.example.test/{course}/calendar.ics"
            })
            .Build();

        using var serviceProvider = new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();

        Assert.IsType<DhbwTimetableService>(serviceProvider.GetRequiredService<ITimetableService>());
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }

    private static MemoryCache CreateCache() => new(new MemoryCacheOptions());

    private static string SingleEventIcal(string summary) => $$"""
        BEGIN:VCALENDAR
        BEGIN:VEVENT
        UID:cached-event
        DTSTART;TZID=Europe/Berlin:20260513T090000
        DTEND;TZID=Europe/Berlin:20260513T103000
        SUMMARY:{{summary}}
        LOCATION:R101
        END:VEVENT
        END:VCALENDAR
        """;

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public void SetUtcNow(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
