using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CampusConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace CampusConnect.Infrastructure.ExternalServices;

public sealed class MensaApiClient(HttpClient httpClient, IOptions<MensaOptions> options) : IMensaService
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public async Task<IReadOnlyList<MensaDay>> GetWeekMenuAsync(CancellationToken cancellationToken = default)
    {
        var mensaOptions = options.Value;
        if (string.IsNullOrWhiteSpace(mensaOptions.ApiKey))
            throw new InvalidOperationException("Der Mensa-API-Key ist nicht konfiguriert.");

        using var response = await httpClient.GetAsync(BuildRequestUri(mensaOptions), cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new InvalidOperationException("Der Mensa-API-Key wurde abgelehnt.");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Der Speiseplan konnte gerade nicht geladen werden.");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

        return ParseMenu(document);
    }

    private static string BuildRequestUri(MensaOptions options)
    {
        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? "https://www.swfr.de/apispeiseplan"
            : options.BaseUrl.Trim();

        var query = new Dictionary<string, string>
        {
            ["type"] = "98",
            ["tx_speiseplan_pi1[apiKey]"] = options.ApiKey.Trim(),
            ["tx_speiseplan_pi1[ort]"] = string.IsNullOrWhiteSpace(options.OrtId) ? "677" : options.OrtId.Trim(),
            ["tx_speiseplan_pi1[tage]"] = Math.Clamp(options.Days, 1, 14).ToString(CultureInfo.InvariantCulture)
        };

        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return baseUrl + separator + string.Join("&", query.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
    }

    private static IReadOnlyList<MensaDay> ParseMenu(XDocument document)
    {
        var dishesByDate = new SortedDictionary<DateOnly, List<MensaDish>>();
        var dishElements = document.Descendants().Where(IsDishElement).ToList();

        foreach (var dishElement in dishElements)
        {
            if (!TryReadDate(dishElement, out var date) || !TryReadDish(dishElement, out var dish))
                continue;

            if (!dishesByDate.TryGetValue(date, out var dishes))
            {
                dishes = [];
                dishesByDate[date] = dishes;
            }

            if (!dishes.Any(existing => string.Equals(existing.Name, dish.Name, StringComparison.OrdinalIgnoreCase)))
                dishes.Add(dish);
        }

        return dishesByDate
            .Select(day => new MensaDay(day.Key, day.Value.OrderBy(dish => dish.Category).ThenBy(dish => dish.Name).ToList()))
            .ToList();
    }

    private static bool IsDishElement(XElement element)
    {
        var name = NormalizeName(element.Name.LocalName);
        if (name is "speiseplan" or "menuplan" or "plan" or "ort" or "tagesplan" or "tag" or "day" or "date" or "datum")
            return false;

        if (name is "menue" or "menu" or "meal" || name.Contains("gericht", StringComparison.Ordinal) || name.Contains("speise", StringComparison.Ordinal))
            return true;

        return FindDirectText(element, "name", "gericht", "speise", "essen", "bezeichnung", "titel") is not null
            && FindDirectText(element, "preis", "price", "kategorie", "category", "art", "menulinie") is not null;
    }

    private static bool TryReadDish(XElement element, out MensaDish dish)
    {
        dish = default!;
        var name = FindText(element, "name", "gericht", "speise", "essen", "bezeichnung", "titel");
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var nameLines = ReadNameLines(element, name);
        var category = FindText(element, "kategorie", "category", "art", "menulinie", "linie", "typ", "type") ?? "Speise";
        var allergens = FindText(element, "kennzeichnung", "kennzeichnungen", "zusatzstoffe", "hinweise");
        var tags = string.Join(' ', category, allergens, string.Join(' ', element.Attributes().Select(attribute => CleanText(attribute.Value))), string.Join(' ', element.Descendants().Select(node => CleanText(node.Value))));
        var isVegan = ContainsFoodTag(tags, "vegan") || ContainsFoodTag(tags, "pflanzlich") || ContainsFoodTag(tags, "plant-based") || ContainsFoodTag(tags, "plant based");
        var isVegetarian = isVegan || ContainsFoodTag(tags, "vegetarisch") || ContainsFoodTag(tags, "vegetarian") || ContainsFoodTag(tags, "veggie");

        dish = new MensaDish(
            name,
            nameLines,
            category,
            ParsePrice(FindText(element, "preis", "price", "student", "studierende")),
            string.IsNullOrWhiteSpace(allergens) ? null : allergens,
            isVegetarian,
            isVegan);

        return true;
    }

    private static bool TryReadDate(XElement element, out DateOnly date)
    {
        for (var cursor = element; cursor is not null; cursor = cursor.Parent)
        {
            foreach (var attribute in cursor.Attributes().Where(attribute => NameMatches(attribute.Name.LocalName, "datum", "date")))
            {
                if (TryParseDate(attribute.Value, out date))
                    return true;
            }

            foreach (var child in cursor.Elements().Where(child => NameMatches(child.Name.LocalName, "datum", "date")))
            {
                if (TryParseDate(child.Value, out date))
                    return true;
            }
        }

        date = default;
        return false;
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        var text = CleanText(value);
        var match = Regex.Match(text, @"\d{4}-\d{2}-\d{2}|\d{2}\.\d{2}\.\d{2,4}|\d{8}");
        var candidate = match.Success ? match.Value : text;

        return DateOnly.TryParseExact(candidate,
                ["yyyy-MM-dd", "dd.MM.yyyy", "dd.MM.yy", "yyyyMMdd"],
                GermanCulture,
                DateTimeStyles.None,
                out date)
            || DateOnly.TryParse(candidate, GermanCulture, DateTimeStyles.None, out date)
            || DateOnly.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static decimal ParsePrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        var match = Regex.Match(value, @"\d+([,.]\d{1,2})?");
        return match.Success && decimal.TryParse(match.Value.Replace('.', ','), NumberStyles.Number, GermanCulture, out var price)
            ? price
            : 0m;
    }

    private static string? FindText(XElement element, params string[] names)
    {
        var attribute = element.Attributes().FirstOrDefault(candidate => NameMatches(candidate.Name.LocalName, names));
        if (attribute is not null)
            return CleanText(attribute.Value);

        var child = element.Descendants().FirstOrDefault(candidate => NameMatches(candidate.Name.LocalName, names));
        if (child is not null)
            return CleanText(child.Value);

        if (NameMatches(element.Name.LocalName, names) && !element.HasElements)
            return CleanText(element.Value);

        return null;
    }

    private static string? FindDirectText(XElement element, params string[] names)
    {
        var attribute = element.Attributes().FirstOrDefault(candidate => NameMatches(candidate.Name.LocalName, names));
        if (attribute is not null)
            return CleanText(attribute.Value);

        var child = element.Elements().FirstOrDefault(candidate => NameMatches(candidate.Name.LocalName, names));
        return child is null ? null : CleanText(child.Value);
    }

    private static IReadOnlyList<string> ReadNameLines(XElement element, string fallbackName)
    {
        var nameWithBreaks = element.Descendants().FirstOrDefault(candidate => NameMatches(candidate.Name.LocalName, "nameMitUmbruch", "nameWithBreaks"));
        if (nameWithBreaks is null)
            return [fallbackName];

        var text = WebUtility.HtmlDecode(nameWithBreaks.Value);
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<.*?>", " ");

        var lines = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(CleanText)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return lines.Count > 0 ? lines : [fallbackName];
    }

    private static bool NameMatches(string name, params string[] candidates)
    {
        var normalized = NormalizeName(name);
        return candidates.Select(NormalizeName).Any(candidate => normalized == candidate || normalized.Contains(candidate, StringComparison.Ordinal));
    }

    private static string NormalizeName(string value) => Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]", string.Empty);

    private static string CleanText(string value)
    {
        var text = WebUtility.HtmlDecode(value);
        text = Regex.Replace(text, "<.*?>", " ");
        text = Regex.Replace(text, "\\s+", " ");
        return text.Trim();
    }

    private static bool ContainsFoodTag(string value, string tag) => value.Contains(tag, StringComparison.OrdinalIgnoreCase);
}