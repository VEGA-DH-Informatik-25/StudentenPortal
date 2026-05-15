namespace CampusConnect.Infrastructure.ExternalServices;

public sealed class DhbwTimetableOptions
{
    public const string SectionName = "Timetable";

    public string CalendarUrlTemplate { get; set; } = "https://webmail.dhbw-loerrach.de/owa/calendar/kal-{course}@dhbw-loerrach.de/Kalender/calendar.ics";

    public int MaxLookaheadDays { get; set; } = 120;

    public int CacheMinutes { get; set; } = 240;

    public int StaleCacheMinutes { get; set; } = 1440;

    public Dictionary<string, string> CourseAliases { get; set; } = [];
}