using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CampusConnect.API.Tests;

public sealed class ApiAuthorizationTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/feed")]
    [InlineData("/api/groups")]
    [InlineData("/api/grades")]
    [InlineData("/api/calendar")]
    [InlineData("/api/timetable?course=TIF25A")]
    [InlineData("/api/mensa")]
    [InlineData("/api/admin/users")]
    public async Task ProtectedEndpoints_WithoutToken_ReturnUnauthorized(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CoursesEndpoint_AllowsAnonymousRequests()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/courses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var courses = await response.Content.ReadFromJsonAsync<CourseResponse[]>();
        Assert.NotNull(courses);
        Assert.Contains(courses!, course => course.Code == "ADMIN" && course.IsActive);
    }

    [Fact]
    public async Task AdminEndpoint_WithStudentToken_ReturnsForbidden()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(Guid.NewGuid()));

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenMissingUserId_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateTokenWithoutUserId());

        var response = await client.GetAsync("/api/grades");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GradesEndpoint_WithStudentToken_ReturnsCurrentUserSummary()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(Guid.NewGuid()));

        var response = await client.GetAsync("/api/grades");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GradeSummaryResponse>();
        Assert.NotNull(body);
        Assert.Empty(body.Grades);
        Assert.Equal(0, body.TotalEcts);
    }

    private sealed record GradeSummaryResponse(IReadOnlyList<object> Grades, decimal WeightedAverage, int TotalEcts);
    private sealed record CourseResponse(string Code, string StudyProgram, int Semester, bool IsActive, DateTime CreatedAt);
}
