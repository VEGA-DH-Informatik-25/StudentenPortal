using CampusConnect.Domain.Entities;
using System.Net;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace CampusConnect.Infrastructure.ExternalServices;

public sealed partial class DhbwStudyPlanParser
{
    public StudyPlan Parse(Stream pdfStream, string studyProgram, string sourceUrl, DateTime retrievedAt)
    {
        using var document = PdfDocument.Open(pdfStream);
        var pages = document.GetPages()
            .Select(page => new DhbwPdfPage(ContentOrderTextExtractor.GetText(page), ExtractExamRows(page)))
            .ToList();

        return DhbwStudyPlanTextParser.Parse(studyProgram, sourceUrl, retrievedAt, pages);
    }

    private static IReadOnlyList<DhbwExamRow> ExtractExamRows(Page page)
    {
        var lines = page.GetWords(NearestNeighbourWordExtractor.Instance)
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .GroupBy(word => Math.Round(word.BoundingBox.Bottom / 3) * 3)
            .Select(group => new PositionedLine(group.Key, group.OrderBy(word => word.BoundingBox.Left).ToList()))
            .OrderByDescending(line => line.Y)
            .ToList();

        var header = lines.FirstOrDefault(line => line.Text.Contains("PRÜFUNGSLEISTUNG", StringComparison.OrdinalIgnoreCase)
            && line.Text.Contains("BENOTUNG", StringComparison.OrdinalIgnoreCase));
        var workload = lines.FirstOrDefault(line => line.Y < header?.Y
            && line.Text.Contains("WORKLOAD", StringComparison.OrdinalIgnoreCase));

        if (header is null || workload is null)
            return [];

        return lines
            .Where(line => line.Y < header.Y && line.Y > workload.Y)
            .Select(ToExamRow)
            .Where(row => !string.IsNullOrWhiteSpace(row.Name))
            .ToList();
    }

    private static DhbwExamRow ToExamRow(PositionedLine line)
    {
        var name = Join(line.Words.Where(word => word.BoundingBox.Left < 300));
        var scope = Join(line.Words.Where(word => word.BoundingBox.Left >= 300 && word.BoundingBox.Left < 450));
        var graded = Join(line.Words.Where(word => word.BoundingBox.Left >= 450));

        return new DhbwExamRow(name, scope, ParseGraded(graded));
    }

    private static string Join(IEnumerable<Word> words) => string.Join(' ', words.Select(word => word.Text)).Trim();

    private static bool? ParseGraded(string value)
    {
        if (value.Equals("ja", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("nein", StringComparison.OrdinalIgnoreCase))
            return false;

        return null;
    }

    private sealed record PositionedLine(double Y, IReadOnlyList<Word> Words)
    {
        public string Text => string.Join(' ', Words.Select(word => word.Text));
    }
}

public static partial class DhbwStudyPlanTextParser
{
    public static StudyPlan Parse(string studyProgram, string sourceUrl, DateTime retrievedAt, IReadOnlyList<DhbwPdfPage> pages)
    {
        var modules = ParseCurriculumModules(pages.SelectMany(page => SplitLines(page.Text)));
        var moduleDetails = ParseModuleDetails(pages);

        var enrichedModules = modules
            .Select(module => moduleDetails.TryGetValue(module.Code, out var exams)
                ? module with { Exams = exams }
                : module)
            .ToList();

        return new StudyPlan(studyProgram, sourceUrl, retrievedAt, enrichedModules);
    }

    private static IReadOnlyList<StudyPlanModule> ParseCurriculumModules(IEnumerable<string> lines)
    {
        var modules = new Dictionary<string, StudyPlanModule>(StringComparer.OrdinalIgnoreCase);
        var isInCurriculum = false;
        var isRequired = true;

        foreach (var rawLine in lines)
        {
            var line = NormalizeWhitespace(rawLine);
            if (line.Length == 0)
                continue;

            if (line.Contains("FESTGELEGTER MODULBEREICH", StringComparison.OrdinalIgnoreCase))
            {
                isInCurriculum = true;
                isRequired = true;
                continue;
            }

            if (line.Contains("VARIABLER MODULBEREICH", StringComparison.OrdinalIgnoreCase))
            {
                isInCurriculum = true;
                isRequired = false;
                continue;
            }

            if (!isInCurriculum || line.StartsWith("NUMMER ", StringComparison.OrdinalIgnoreCase))
                continue;

            if (line.Contains("Curriculum //", StringComparison.OrdinalIgnoreCase))
            {
                isInCurriculum = false;
                continue;
            }

            var match = CurriculumModuleRegex().Match(line);
            if (!match.Success)
                continue;

            var code = match.Groups["code"].Value.Trim();
            var name = match.Groups["name"].Value.Trim();
            var ects = int.Parse(match.Groups["ects"].Value);
            var studyYear = match.Groups["year"].Success ? int.Parse(match.Groups["year"].Value) : null as int?;

            modules.TryAdd(code, new StudyPlanModule(code, name, studyYear, ects, isRequired, []));
        }

        return modules.Values.ToList();
    }

    private static Dictionary<string, IReadOnlyList<StudyPlanExam>> ParseModuleDetails(IReadOnlyList<DhbwPdfPage> pages)
    {
        var examsByCode = new Dictionary<string, IReadOnlyList<StudyPlanExam>>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in pages)
        {
            var code = FindModuleCode(page.Text);
            if (code is null || page.ExamRows.Count == 0)
                continue;

            examsByCode[code] = page.ExamRows
                .Select(row => new StudyPlanExam(row.Name, row.Scope, row.IsGraded))
                .ToList();
        }

        return examsByCode;
    }

    private static string? FindModuleCode(string text)
    {
        foreach (var line in SplitLines(text).TakeWhile(line => !line.Contains("FORMALE ANGABEN", StringComparison.OrdinalIgnoreCase)))
        {
            var match = ModuleHeaderRegex().Match(NormalizeWhitespace(line));
            if (match.Success)
                return match.Groups["code"].Value.Trim();
        }

        return null;
    }

    private static IEnumerable<string> SplitLines(string text) => text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    private static string NormalizeWhitespace(string value) => WhitespaceRegex().Replace(value.Trim(), " ");

    [GeneratedRegex(@"^(?:(?<year>[1-3])\. Studienjahr|-)(?<code>[A-Z0-9_]+)\s+(?<name>.+?)\s+(?<ects>\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex CurriculumModuleRegex();

    [GeneratedRegex(@"\((?<code>[A-Z0-9_]+)\)$", RegexOptions.CultureInvariant)]
    private static partial Regex ModuleHeaderRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}

public sealed record DhbwPdfPage(string Text, IReadOnlyList<DhbwExamRow> ExamRows);

public sealed record DhbwExamRow(string Name, string Scope, bool? IsGraded);

public static partial class DhbwStudyPlanIndexParser
{
    public static IReadOnlyList<DhbwStudyPlanSource> Parse(string html, Uri indexUri, string campusCode)
    {
        var sources = new List<DhbwStudyPlanSource>();
        var headers = new List<string>();
        var currentStudyArea = string.Empty;
        var campusColumn = -1;

        foreach (Match rowMatch in RowRegex().Matches(html))
        {
            var cells = CellRegex().Matches(rowMatch.Groups["content"].Value)
                .Select(match => new HtmlCell(HtmlToText(match.Groups["content"].Value), ExtractHref(match.Groups["content"].Value, indexUri)))
                .ToList();

            if (cells.Count < 2)
                continue;

            var firstCell = cells[0].Text;
            var maybeHeaders = cells.Skip(1).Select(cell => cell.Text).ToList();
            var newCampusColumn = maybeHeaders.FindIndex(header => header.Equals(campusCode, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(firstCell) && newCampusColumn >= 0 && maybeHeaders.Any(IsCampusHeader))
            {
                currentStudyArea = firstCell;
                headers = maybeHeaders;
                campusColumn = newCampusColumn + 1;
                continue;
            }

            if (campusColumn <= 0 || campusColumn >= cells.Count)
                continue;

            var href = cells[campusColumn].Href;
            if (href is null)
                continue;

            var planName = string.IsNullOrWhiteSpace(firstCell) ? currentStudyArea : firstCell;
            if (string.IsNullOrWhiteSpace(planName) || string.IsNullOrWhiteSpace(currentStudyArea))
                continue;

            sources.Add(new DhbwStudyPlanSource(currentStudyArea, planName, href.ToString()));
        }

        return sources;
    }

    private static string HtmlToText(string html)
    {
        var withoutTags = TagRegex().Replace(html, string.Empty);
        return WebUtility.HtmlDecode(withoutTags).Replace("\u00a0", " ").Trim();
    }

    private static Uri? ExtractHref(string html, Uri indexUri)
    {
        var match = HrefRegex().Match(html);
        if (!match.Success)
            return null;

        return new Uri(indexUri, WebUtility.HtmlDecode(match.Groups["href"].Value));
    }

    private static bool IsCampusHeader(string value) => value.Length is >= 2 and <= 7 && value.Any(char.IsLetter);

    private sealed record HtmlCell(string Text, Uri? Href);

    [GeneratedRegex("<tr[^>]*>(?<content>.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RowRegex();

    [GeneratedRegex("<t[dh][^>]*>(?<content>.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex CellRegex();

    [GeneratedRegex("href=[\\\"'](?<href>[^\\\"']+)[\\\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HrefRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();
}

public sealed record DhbwStudyPlanSource(string StudyArea, string PlanName, string Url);