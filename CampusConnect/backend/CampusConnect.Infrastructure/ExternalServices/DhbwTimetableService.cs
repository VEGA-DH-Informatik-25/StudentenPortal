using System.Globalization;
using System.Net;
using CampusConnect.Application.Common.Interfaces;

namespace CampusConnect.Infrastructure.ExternalServices;

public class DhbwTimetableService(HttpClient httpClient) : ITimetableService
{
    private const string CalendarTimezoneName = "Europe/Berlin";

    private static readonly TimeZoneInfo CalendarTimeZone = ResolveTimeZone("Europe/Berlin");

    private static readonly IReadOnlyDictionary<string, string> CourseAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["wwi23a"] = "wwi23a-am",
        ["wwi23b"] = "wwi23b-am",
        ["wwi25a"] = "wwi25a-am",
        ["wwi25b"] = "wwi25b-am"
    };

    public async Task<TimetableDto> GetTimetableAsync(string course, int days, CancellationToken cancellationToken = default)
    {
        var normalizedCourse = NormalizeCourse(course);
        if (string.IsNullOrWhiteSpace(normalizedCourse))
            throw new InvalidOperationException("Bitte einen Kurs auswählen.");

        var boundedDays = Math.Clamp(days, 1, 120);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, CalendarTimeZone).DateTime);
        var windowStart = AtCalendarTime(today.AddDays(-1), TimeOnly.MinValue);
        var windowEndDate = today.AddDays(boundedDays);
        var windowEnd = AtCalendarTime(windowEndDate, TimeOnly.MinValue);

        using var response = await httpClient.GetAsync(BuildIcalUrl(normalizedCourse), cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new InvalidOperationException($"Der Kurs \"{normalizedCourse.ToUpperInvariant()}\" konnte nicht gefunden werden.");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Der Vorlesungsplan konnte gerade nicht geladen werden.");

        var icalText = await response.Content.ReadAsStringAsync(cancellationToken);
        var sourceEvents = IcalParser.Parse(icalText);
        var events = ExpandEvents(sourceEvents, windowStart, windowEnd)
            .Where(evt => evt.End > windowStart && evt.Start < windowEnd)
            .OrderBy(evt => evt.Start)
            .ToList();

        var groupedDays = events
            .GroupBy(evt => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(evt.Start, CalendarTimeZone).DateTime))
            .Where(group => group.Key >= today && group.Key < windowEndDate)
            .OrderBy(group => group.Key)
            .Select(group => new TimetableDayDto(group.Key, group.Select(ToDto).ToList()))
            .ToList();

        return new TimetableDto(normalizedCourse.ToUpperInvariant(), CalendarTimezoneName, groupedDays);
    }

    private static string NormalizeCourse(string course)
    {
        var normalized = course.Trim().ToLowerInvariant();
        return CourseAliases.TryGetValue(normalized, out var alias) ? alias : normalized;
    }

    private static string BuildIcalUrl(string normalizedCourse)
    {
        var mailbox = Uri.EscapeDataString(normalizedCourse.ToLowerInvariant());
        return $"https://webmail.dhbw-loerrach.de/owa/calendar/kal-{mailbox}@dhbw-loerrach.de/Kalender/calendar.ics";
    }

    private static TimetableEventDto ToDto(IcalEvent evt)
    {
        var location = string.IsNullOrWhiteSpace(evt.Location) ? null : evt.Location.Trim();
        var description = string.IsNullOrWhiteSpace(evt.Description) ? null : evt.Description.Trim();
        var isOnline = IsOnlineLocation(location) || IsOnlineLocation(description);

        return new TimetableEventDto(
            evt.Id,
            string.IsNullOrWhiteSpace(evt.Summary) ? "Vorlesung" : evt.Summary.Trim(),
            evt.Start,
            evt.End,
            location,
            description,
            evt.IsAllDay,
            isOnline);
    }

    private static bool IsOnlineLocation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains("http", StringComparison.OrdinalIgnoreCase)
            || value.Contains("zoom", StringComparison.OrdinalIgnoreCase)
            || value.Contains("teams", StringComparison.OrdinalIgnoreCase)
            || value.Contains("online", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<IcalEvent> ExpandEvents(IReadOnlyList<IcalEvent> sourceEvents, DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        var exceptionKeys = sourceEvents
            .Where(evt => evt.RecurrenceId is not null)
            .GroupBy(evt => evt.Uid, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(evt => OccurrenceKey(evt.RecurrenceId!.Value)).ToHashSet(StringComparer.Ordinal),
                StringComparer.OrdinalIgnoreCase);

        foreach (var sourceEvent in sourceEvents)
        {
            if (sourceEvent.IsCanceled)
                continue;

            if (sourceEvent.Recurrence is null || sourceEvent.RecurrenceId is not null)
            {
                if (sourceEvent.End > windowStart && sourceEvent.Start < windowEnd)
                    yield return sourceEvent with { Id = BuildEventId(sourceEvent.Uid, sourceEvent.Start) };

                continue;
            }

            var exceptionsForEvent = exceptionKeys.GetValueOrDefault(sourceEvent.Uid) ?? [];
            var duration = sourceEvent.End - sourceEvent.Start;

            foreach (var occurrenceStart in ExpandRecurringStarts(sourceEvent, windowStart, windowEnd))
            {
                var occurrenceKey = OccurrenceKey(occurrenceStart);
                if (sourceEvent.ExDates.Contains(occurrenceKey) || exceptionsForEvent.Contains(occurrenceKey))
                    continue;

                var occurrenceEnd = occurrenceStart.Add(duration);
                if (occurrenceEnd <= windowStart || occurrenceStart >= windowEnd)
                    continue;

                yield return sourceEvent with
                {
                    Id = BuildEventId(sourceEvent.Uid, occurrenceStart),
                    Start = occurrenceStart,
                    End = occurrenceEnd,
                    Recurrence = null
                };
            }
        }
    }

    private static IEnumerable<DateTimeOffset> ExpandRecurringStarts(IcalEvent sourceEvent, DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        var recurrence = sourceEvent.Recurrence!;
        var localStart = TimeZoneInfo.ConvertTime(sourceEvent.Start, CalendarTimeZone);
        var startDate = DateOnly.FromDateTime(localStart.DateTime);
        var startTime = TimeOnly.FromDateTime(localStart.DateTime);
        var lastDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(windowEnd, CalendarTimeZone).DateTime).AddDays(1);
        var generatedCount = 0;

        for (var cursor = startDate; cursor <= lastDate; cursor = cursor.AddDays(1))
        {
            if (!MatchesRecurrenceDate(cursor, startDate, recurrence))
                continue;

            var occurrenceStart = AtCalendarTime(cursor, startTime);
            if (occurrenceStart < sourceEvent.Start)
                continue;

            if (recurrence.Until is not null && occurrenceStart > recurrence.Until.Value)
                break;

            generatedCount++;
            if (recurrence.Count is not null && generatedCount > recurrence.Count.Value)
                break;

            if (occurrenceStart < windowEnd && occurrenceStart.Add(sourceEvent.End - sourceEvent.Start) > windowStart)
                yield return occurrenceStart;
        }
    }

    private static bool MatchesRecurrenceDate(DateOnly cursor, DateOnly startDate, IcalRecurrence recurrence)
    {
        if (cursor < startDate)
            return false;

        return recurrence.Frequency switch
        {
            "DAILY" => (cursor.DayNumber - startDate.DayNumber) % recurrence.Interval == 0,
            "WEEKLY" => MatchesWeekly(cursor, startDate, recurrence),
            "MONTHLY" => MatchesMonthly(cursor, startDate, recurrence),
            _ => false
        };
    }

    private static bool MatchesWeekly(DateOnly cursor, DateOnly startDate, IcalRecurrence recurrence)
    {
        var weekDelta = (WeekStartDayNumber(cursor) - WeekStartDayNumber(startDate)) / 7;
        if (weekDelta < 0 || weekDelta % recurrence.Interval != 0)
            return false;

        return recurrence.ByDays.Count == 0
            ? cursor.DayOfWeek == startDate.DayOfWeek
            : recurrence.ByDays.Contains(cursor.DayOfWeek);
    }

    private static bool MatchesMonthly(DateOnly cursor, DateOnly startDate, IcalRecurrence recurrence)
    {
        var monthDelta = (cursor.Year - startDate.Year) * 12 + cursor.Month - startDate.Month;
        if (monthDelta < 0 || monthDelta % recurrence.Interval != 0)
            return false;

        return recurrence.ByDays.Count > 0
            ? recurrence.ByDays.Contains(cursor.DayOfWeek)
            : cursor.Day == startDate.Day;
    }

    private static int WeekStartDayNumber(DateOnly date)
    {
        var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysFromMonday).DayNumber;
    }

    private static string BuildEventId(string uid, DateTimeOffset start) => $"{uid}-{start.UtcTicks}";

    private static string OccurrenceKey(DateTimeOffset value)
    {
        var local = TimeZoneInfo.ConvertTime(value, CalendarTimeZone);
        return local.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset AtCalendarTime(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, CalendarTimeZone.GetUtcOffset(local));
    }

    private static TimeZoneInfo ResolveTimeZone(string? tzid)
    {
        string[] candidates = (tzid ?? string.Empty) switch
        {
            "" => ["Europe/Berlin", "W. Europe Standard Time"],
            "W. Europe Standard Time" => ["W. Europe Standard Time", "Europe/Berlin"],
            "Central European Standard Time" => ["Central European Standard Time", "Europe/Warsaw", "Europe/Berlin", "W. Europe Standard Time"],
            _ => [tzid!, "Europe/Berlin", "W. Europe Standard Time"]
        };

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate.Trim('"'));
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }

    private sealed record IcalEvent(
        string Id,
        string Uid,
        string Summary,
        DateTimeOffset Start,
        DateTimeOffset End,
        string? Location,
        string? Description,
        bool IsAllDay,
        bool IsCanceled,
        IcalRecurrence? Recurrence,
        IReadOnlySet<string> ExDates,
        DateTimeOffset? RecurrenceId);

    private sealed record IcalRecurrence(
        string Frequency,
        int Interval,
        DateTimeOffset? Until,
        int? Count,
        IReadOnlySet<DayOfWeek> ByDays);

    private sealed record IcalProperty(string Name, IReadOnlyDictionary<string, string> Parameters, string Value);

    private static class IcalParser
    {
        public static IReadOnlyList<IcalEvent> Parse(string icalText)
        {
            var events = new List<IcalEvent>();
            List<IcalProperty>? currentEvent = null;

            foreach (var line in UnfoldLines(icalText))
            {
                if (line.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    currentEvent = [];
                    continue;
                }

                if (line.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentEvent is not null && TryBuildEvent(currentEvent, out var calendarEvent))
                        events.Add(calendarEvent);

                    currentEvent = null;
                    continue;
                }

                currentEvent?.Add(ParseProperty(line));
            }

            return events;
        }

        private static IEnumerable<string> UnfoldLines(string icalText)
        {
            var lines = icalText.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
            var buffer = string.Empty;

            foreach (var rawLine in lines)
            {
                if (rawLine.Length > 0 && (rawLine[0] == ' ' || rawLine[0] == '\t'))
                {
                    buffer += rawLine[1..];
                    continue;
                }

                if (buffer.Length > 0)
                    yield return buffer;

                buffer = rawLine;
            }

            if (buffer.Length > 0)
                yield return buffer;
        }

        private static IcalProperty ParseProperty(string line)
        {
            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator < 0)
                return new IcalProperty(line.ToUpperInvariant(), new Dictionary<string, string>(), string.Empty);

            var head = line[..separator];
            var value = line[(separator + 1)..];
            var parts = head.Split(';');
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in parts.Skip(1))
            {
                var equals = part.IndexOf('=', StringComparison.Ordinal);
                if (equals <= 0)
                    continue;

                parameters[part[..equals].ToUpperInvariant()] = part[(equals + 1)..].Trim('"');
            }

            return new IcalProperty(parts[0].ToUpperInvariant(), parameters, value);
        }

        private static bool TryBuildEvent(IReadOnlyList<IcalProperty> properties, out IcalEvent calendarEvent)
        {
            calendarEvent = default!;
            var startProperty = properties.FirstOrDefault(prop => prop.Name == "DTSTART");
            if (startProperty is null)
                return false;

            var start = ParseDateTime(startProperty, out var isAllDay);
            var endProperty = properties.FirstOrDefault(prop => prop.Name == "DTEND");
            var end = endProperty is null ? start.AddHours(isAllDay ? 24 : 1) : ParseDateTime(endProperty, out _);
            if (end <= start)
                end = start.AddHours(isAllDay ? 24 : 1);

            var uid = DecodeText(FirstValue(properties, "UID")) ?? Guid.NewGuid().ToString("N");
            var summary = DecodeText(FirstValue(properties, "SUMMARY")) ?? "Vorlesung";
            var status = FirstValue(properties, "STATUS") ?? string.Empty;
            var recurrenceIdProperty = properties.FirstOrDefault(prop => prop.Name == "RECURRENCE-ID");
            var recurrenceId = recurrenceIdProperty is null ? (DateTimeOffset?)null : ParseDateTime(recurrenceIdProperty, out _);

            calendarEvent = new IcalEvent(
                BuildEventId(uid, start),
                uid,
                summary,
                start,
                end,
                DecodeText(FirstValue(properties, "LOCATION")),
                DecodeText(FirstValue(properties, "DESCRIPTION")),
                isAllDay,
                status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase),
                ParseRecurrence(FirstValue(properties, "RRULE")),
                ParseExDates(properties.Where(prop => prop.Name == "EXDATE")),
                recurrenceId);

            return true;
        }

        private static string? FirstValue(IReadOnlyList<IcalProperty> properties, string name) =>
            properties.FirstOrDefault(prop => prop.Name == name)?.Value;

        private static IReadOnlySet<string> ParseExDates(IEnumerable<IcalProperty> properties)
        {
            var values = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in properties)
            {
                foreach (var value in property.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var dateProperty = property with { Value = value };
                    values.Add(OccurrenceKey(ParseDateTime(dateProperty, out _)));
                }
            }

            return values;
        }

        private static IcalRecurrence? ParseRecurrence(string? rrule)
        {
            if (string.IsNullOrWhiteSpace(rrule))
                return null;

            var values = rrule.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0].ToUpperInvariant(), parts => parts[1], StringComparer.OrdinalIgnoreCase);

            if (!values.TryGetValue("FREQ", out var frequency))
                return null;

            var interval = values.TryGetValue("INTERVAL", out var intervalValue) && int.TryParse(intervalValue, out var parsedInterval)
                ? Math.Max(1, parsedInterval)
                : 1;

            var count = values.TryGetValue("COUNT", out var countValue) && int.TryParse(countValue, out var parsedCount)
                ? parsedCount
                : (int?)null;

            var until = values.TryGetValue("UNTIL", out var untilValue) ? ParseUntil(untilValue) : null;
            var byDays = values.TryGetValue("BYDAY", out var byDayValue) ? ParseByDays(byDayValue) : new HashSet<DayOfWeek>();

            return new IcalRecurrence(frequency.ToUpperInvariant(), interval, until, count, byDays);
        }

        private static DateTimeOffset? ParseUntil(string value)
        {
            if (value.Length == 8 && DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return AtCalendarTime(date.AddDays(1), TimeOnly.MinValue).AddTicks(-1);

            return ParseDateTime(new IcalProperty("UNTIL", new Dictionary<string, string>(), value), out _);
        }

        private static HashSet<DayOfWeek> ParseByDays(string value)
        {
            var days = new HashSet<DayOfWeek>();
            foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var code = token.Length >= 2 ? token[^2..].ToUpperInvariant() : token.ToUpperInvariant();
                var day = code switch
                {
                    "MO" => DayOfWeek.Monday,
                    "TU" => DayOfWeek.Tuesday,
                    "WE" => DayOfWeek.Wednesday,
                    "TH" => DayOfWeek.Thursday,
                    "FR" => DayOfWeek.Friday,
                    "SA" => DayOfWeek.Saturday,
                    "SU" => DayOfWeek.Sunday,
                    _ => (DayOfWeek?)null
                };

                if (day is not null)
                    days.Add(day.Value);
            }

            return days;
        }

        private static DateTimeOffset ParseDateTime(IcalProperty property, out bool isDateOnly)
        {
            var value = property.Value.Trim();
            isDateOnly = value.Length == 8;
            if (isDateOnly && DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
                return AtCalendarTime(dateOnly, TimeOnly.MinValue);

            if (value.EndsWith('Z'))
            {
                var utc = DateTime.ParseExact(value, ["yyyyMMdd'T'HHmmss'Z'", "yyyyMMdd'T'HHmm'Z'"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                return new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
            }

            var local = DateTime.ParseExact(value, ["yyyyMMdd'T'HHmmss", "yyyyMMdd'T'HHmm"], CultureInfo.InvariantCulture, DateTimeStyles.None);
            var timezone = property.Parameters.TryGetValue("TZID", out var tzid) ? ResolveTimeZone(tzid) : CalendarTimeZone;
            return new DateTimeOffset(local, timezone.GetUtcOffset(local));
        }

        private static string? DecodeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value
                .Replace("\\n", "\n", StringComparison.OrdinalIgnoreCase)
                .Replace("\\,", ",", StringComparison.Ordinal)
                .Replace("\\;", ";", StringComparison.Ordinal)
                .Replace("\\\\", "\\", StringComparison.Ordinal)
                .Trim();
        }
    }
}