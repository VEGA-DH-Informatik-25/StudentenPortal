namespace CampusConnect.Application.Common.Interfaces;

public record TimetableEventDto(
    string Id,
    string Title,
    DateTimeOffset Start,
    DateTimeOffset End,
    string? Location,
    string? Description,
    bool IsAllDay,
    bool IsOnline);

public record TimetableDayDto(DateOnly Date, IReadOnlyList<TimetableEventDto> Events);

public record TimetableDto(string Course, string Timezone, IReadOnlyList<TimetableDayDto> Days);

public interface ITimetableService
{
    Task<TimetableDto> GetTimetableAsync(string course, int days, CancellationToken cancellationToken = default);
}