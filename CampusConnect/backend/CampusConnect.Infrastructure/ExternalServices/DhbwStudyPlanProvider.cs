using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CampusConnect.Infrastructure.ExternalServices;

public sealed partial class DhbwStudyPlanProvider(
    HttpClient httpClient,
    IOptions<DhbwStudyPlanOptions> options,
    IMemoryCache cache,
    DhbwStudyPlanParser parser) : IStudyPlanProvider
{
    private readonly DhbwStudyPlanOptions _options = options.Value;

    public async Task<StudyPlan?> GetPlanForCourseAsync(Course course, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(course.StudyProgram))
            return null;

        var source = await ResolveSourceAsync(course, cancellationToken);
        if (source is null)
            return null;

        var cacheKey = $"dhbw-study-plan:{source.Url}";
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(5, _options.CacheMinutes));
            await using var stream = await httpClient.GetStreamAsync(source.Url, cancellationToken);
            return parser.Parse(stream, source.PlanName, source.Url, DateTime.UtcNow);
        });
    }

    private async Task<DhbwStudyPlanSource?> ResolveSourceAsync(Course course, CancellationToken cancellationToken)
    {
        var sources = await GetSourcesAsync(cancellationToken);
        var candidates = sources
            .Select(source => new { Source = source, Score = Score(course.StudyProgram, source) })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Source.StudyArea)
            .ThenBy(candidate => candidate.Source.PlanName)
            .ToList();

        if (candidates.Count == 0)
            return null;

        var bestScore = candidates[0].Score;
        var bestCandidates = candidates.Where(candidate => candidate.Score == bestScore).ToList();

        return bestCandidates.Count == 1 ? bestCandidates[0].Source : null;
    }

    private async Task<IReadOnlyList<DhbwStudyPlanSource>> GetSourcesAsync(CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync("dhbw-study-plan:sources", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(5, _options.CacheMinutes));
            var sources = new List<DhbwStudyPlanSource>();

            foreach (var indexUrl in _options.IndexUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
            {
                var html = await httpClient.GetStringAsync(indexUrl, cancellationToken);
                sources.AddRange(DhbwStudyPlanIndexParser.Parse(html, new Uri(indexUrl), _options.CampusCode));
            }

            return sources;
        }) ?? [];
    }

    private static int Score(string studyProgram, DhbwStudyPlanSource source)
    {
        var courseTokens = CourseCandidates(studyProgram).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var plan = Normalize(source.PlanName);
        var area = Normalize(source.StudyArea);

        if (courseTokens.Contains(plan))
            return 100;

        if (courseTokens.Contains(area) && area == plan)
            return 95;

        if (courseTokens.Contains(area) && plan.Contains(area, StringComparison.OrdinalIgnoreCase))
            return 80;

        if (courseTokens.Contains(area))
            return 60;

        if (courseTokens.Any(token => token.Length > 4 && plan.Contains(token, StringComparison.OrdinalIgnoreCase)))
            return 50;

        return courseTokens.Any(token => token.Length > 4 && area.Contains(token, StringComparison.OrdinalIgnoreCase)) ? 40 : 0;
    }

    private static IEnumerable<string> CourseCandidates(string studyProgram)
    {
        var normalized = Normalize(studyProgram);
        if (normalized.Length > 0)
            yield return normalized;

        foreach (var part in CoursePartRegex().Split(studyProgram).Select(Normalize).Where(part => part.Length > 0))
            yield return part;

        const string businessPrefix = "bwl";
        if (normalized.StartsWith(businessPrefix, StringComparison.OrdinalIgnoreCase) && normalized.Length > businessPrefix.Length)
            yield return normalized[businessPrefix.Length..];
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"\s+-\s+|[-/()]")]
    private static partial Regex CoursePartRegex();
}