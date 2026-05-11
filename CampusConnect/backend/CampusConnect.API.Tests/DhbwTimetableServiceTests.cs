using System.Net;
using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Infrastructure;
using CampusConnect.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
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
        var service = new DhbwTimetableService(
            httpClient,
            Options.Create(new DhbwTimetableOptions
            {
                CalendarUrlTemplate = "https://calendar.example.test/{course}/calendar.ics",
                CourseAliases = new Dictionary<string, string>
                {
                    ["WWI25A"] = "WWI25A-AM"
                }
            }));

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
                SUMMARY:Mathematik
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
        var service = new DhbwTimetableService(
            httpClient,
            Options.Create(new DhbwTimetableOptions
            {
                CalendarUrlTemplate = "https://calendar.example.test/{course}/calendar.ics"
            }),
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero)));

        var timetable = await service.GetTimetableAsync("tif25a", 30);

        Assert.Collection(
            timetable.Days,
            day =>
            {
                Assert.Equal(new DateOnly(2026, 5, 11), day.Date);
                Assert.Equal("Mathematik", day.Events.Single().Title);
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
                SUMMARY:Datenbanken
                LOCATION:R201
                END:VEVENT
                BEGIN:VEVENT
                UID:current-week
                DTSTART;TZID=Europe/Berlin:20260511T090000
                DTEND;TZID=Europe/Berlin:20260511T103000
                SUMMARY:Mathematik
                LOCATION:R101
                END:VEVENT
                END:VCALENDAR
                """)
        }));
        var service = new DhbwTimetableService(
            httpClient,
            Options.Create(new DhbwTimetableOptions
            {
                CalendarUrlTemplate = "https://calendar.example.test/{course}/calendar.ics"
            }),
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero)));

        var timetable = await service.GetTimetableAsync("tif25a", 6, new DateOnly(2026, 5, 4));

        var day = Assert.Single(timetable.Days);
        Assert.Equal(new DateOnly(2026, 5, 5), day.Date);
        Assert.Equal("Datenbanken", day.Events.Single().Title);
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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
