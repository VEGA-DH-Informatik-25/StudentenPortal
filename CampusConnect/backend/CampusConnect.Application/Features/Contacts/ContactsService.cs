using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Contacts;

public record ContactProfileDto(
    Guid Id,
    string DisplayName,
    string Email,
    string StudyProgram,
    int Semester,
    string Course,
    string PhoneNumber,
    string Location,
    string ProfileNote,
    string Role);

public class ContactsService(IUserRepository userRepository)
{
    public async Task<IReadOnlyList<ContactProfileDto>> SearchAsync(Guid currentUserId, string? query, CancellationToken cancellationToken = default)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        var term = query?.Trim();
        var results = users
            .Where(user => user.Id != currentUserId)
            .Where(user => string.IsNullOrWhiteSpace(term) || Matches(user, term))
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .Take(50)
            .Select(ToDto)
            .ToList();

        return results;
    }

    public static ContactProfileDto ToDto(User user) => new(
        user.Id,
        user.DisplayName,
        user.Email,
        user.StudyProgram,
        user.Semester,
        user.Course,
        user.PhoneNumber,
        user.Location,
        user.ProfileNote,
        user.Role.ToString());

    private static bool Matches(User user, string term) =>
        Contains(user.DisplayName, term) ||
        Contains(user.Email, term) ||
        Contains(user.StudyProgram, term) ||
        Contains(user.Course, term) ||
        Contains(user.PhoneNumber, term) ||
        Contains(user.Location, term) ||
        Contains(user.ProfileNote, term) ||
        Contains(user.Role.ToString(), term);

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
