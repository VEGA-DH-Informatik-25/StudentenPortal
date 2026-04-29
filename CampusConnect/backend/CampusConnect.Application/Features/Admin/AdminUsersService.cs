using CampusConnect.Application.Common;
using CampusConnect.Application.Features.Courses;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Admin;

public record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string StudyProgram,
    int Semester,
    string Course,
    string Role,
    DateTime CreatedAt);

public record UpdateUserRoleCommand(Guid UserId, string Role, Guid CurrentAdminId);
public record UpdateUserCourseCommand(Guid UserId, string CourseCode);

public class AdminUsersService(IUserRepository userRepository, ICourseRepository courseRepository, IGroupRepository groupRepository)
{
    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        foreach (var user in users)
            await SyncProfileMetadataFromCourseAsync(user, cancellationToken);

        return users.Select(ToDto).ToList();
    }

    public async Task<Result<AdminUserDto>> UpdateRoleAsync(UpdateUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
            return Result<AdminUserDto>.Failure("Diese Rolle ist nicht gültig.");

        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result<AdminUserDto>.Failure("Benutzer wurde nicht gefunden.");

        if (user.Id == command.CurrentAdminId && role != UserRole.Admin)
            return Result<AdminUserDto>.Failure("Du kannst deine eigene Admin-Rolle nicht entfernen.");

        user.Role = role;
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result<AdminUserDto>.Success(ToDto(user));
    }

    public async Task<Result<AdminUserDto>> UpdateCourseAsync(UpdateUserCourseCommand command, CancellationToken cancellationToken = default)
    {
        var courseCode = CoursesService.NormalizeCourseCode(command.CourseCode);
        var course = await courseRepository.FindByCodeAsync(courseCode, cancellationToken);
        if (course is null || !course.IsActive)
            return Result<AdminUserDto>.Failure("Bitte wähle einen gültigen Kurs aus.");

        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result<AdminUserDto>.Failure("Benutzer wurde nicht gefunden.");

        var previousCourse = user.Course;
        user.Course = course.Code;
        user.StudyProgram = course.StudyProgram;
        user.Semester = course.Semester;

        await userRepository.UpdateAsync(user, cancellationToken);
        await SyncCourseAssignmentsAsync(course.Code, previousCourse, cancellationToken);

        return Result<AdminUserDto>.Success(ToDto(user));
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid userId, Guid currentAdminId, CancellationToken cancellationToken = default)
    {
        if (userId == currentAdminId)
            return Result<bool>.Failure("Du kannst dein eigenes Admin-Konto nicht löschen.");

        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<bool>.Failure("Benutzer wurde nicht gefunden.");

        var previousCourse = user.Course;
        await userRepository.DeleteAsync(userId, cancellationToken);
        await SyncCourseAssignmentsAsync(previousCourse, previousCourse, cancellationToken);
        return Result<bool>.Success(true);
    }

    private static AdminUserDto ToDto(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.StudyProgram,
        user.Semester,
        user.Course,
        user.Role.ToString(),
        user.CreatedAt);

    private async Task SyncProfileMetadataFromCourseAsync(User user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Course))
            return;

        var course = await courseRepository.FindByCodeAsync(CoursesService.NormalizeCourseCode(user.Course), cancellationToken);
        if (course is null)
            return;

        if (user.Course == course.Code && user.StudyProgram == course.StudyProgram && user.Semester == course.Semester)
            return;

        user.Course = course.Code;
        user.StudyProgram = course.StudyProgram;
        user.Semester = course.Semester;
        await userRepository.UpdateAsync(user, cancellationToken);
    }

    private async Task SyncCourseAssignmentsAsync(string newCourseCode, string oldCourseCode, CancellationToken cancellationToken)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        foreach (var courseCode in new[] { newCourseCode, oldCourseCode }.Where(code => !string.IsNullOrWhiteSpace(code)).Select(CoursesService.NormalizeCourseCode).Distinct())
        {
            var course = await courseRepository.FindByCodeAsync(courseCode, cancellationToken);
            if (course is null)
                continue;

            await groupRepository.EnsureCourseGroupAsync(course.Code, course.StudyProgram);
            await groupRepository.SyncCourseAssignmentsAsync(
                course.Code,
                users.Where(user => string.Equals(user.Course, course.Code, StringComparison.OrdinalIgnoreCase)).Select(user => user.Id).ToList());
        }
    }
}
