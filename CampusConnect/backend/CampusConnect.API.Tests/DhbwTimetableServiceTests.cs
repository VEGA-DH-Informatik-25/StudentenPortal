using System.Net;
using CampusConnect.Infrastructure.ExternalServices;
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

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
