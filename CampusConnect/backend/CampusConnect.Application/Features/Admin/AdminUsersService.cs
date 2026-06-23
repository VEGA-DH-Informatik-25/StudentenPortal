using CampusConnect.Application.Common;
using CampusConnect.Application.Common.Security;
using CampusConnect.Application.Features.Courses;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;
using System.Net.Mail;

namespace CampusConnect.Application.Features.Admin;

public record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string StudyProgram,
    string Course,
    string Role,
    bool IsActive,
    DateTime CreatedAt);

public record UpdateUserRoleCommand(Guid UserId, string Role, Guid CurrentAdminId);
public record UpdateUserCourseCommand(Guid UserId, string CourseCode);
public record CreateAdminUserCommand(string FirstName, string LastName, string Email, string Role, string CourseCode, string InitialPassword, bool IsActive = true);
public record UpdateAdminUserCommand(Guid UserId, string DisplayName, string Email, string Role, string CourseCode, bool IsActive, Guid CurrentAdminId);
public record UpdateUserStatusCommand(Guid UserId, bool IsActive, Guid CurrentAdminId);

public class AdminUsersService(IUserRepository userRepository, ICourseRepository courseRepository, IGroupRepository groupRepository)
{
    private const string InvalidCourseError = "Choose a valid course.";
    private const string InvalidRoleError = "This role is invalid.";

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        foreach (var user in users)
            await SyncProfileMetadataFromCourseAsync(user, cancellationToken);

        return users.Select(ToDto).ToList();
    }

    public async Task<Result<AdminUserDto>> CreateUserAsync(CreateAdminUserCommand command, CancellationToken cancellationToken = default)
    {
        var firstName = command.FirstName.Trim();
        var lastName = command.LastName.Trim();
        var email = command.Email.Trim().ToLowerInvariant();
        var password = command.InitialPassword.Trim();

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return Result<AdminUserDto>.Failure("First name and last name are required.");

        if (firstName.Length > 60 || lastName.Length > 60)
            return Result<AdminUserDto>.Failure("First name and last name must be at most 60 characters long.");

        var emailValidationError = ValidateEmail(email);
        if (emailValidationError is not null)
            return Result<AdminUserDto>.Failure(emailValidationError);

        if (string.IsNullOrWhiteSpace(password))
            return Result<AdminUserDto>.Failure("Initial password is required.");

        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
            return Result<AdminUserDto>.Failure(InvalidRoleError);

        var course = await ResolveActiveCourseAsync(command.CourseCode, cancellationToken);
        if (course is null)
            return Result<AdminUserDto>.Failure(InvalidCourseError);

        if (await userRepository.FindByEmailAsync(email, cancellationToken) is not null)
            return Result<AdminUserDto>.Failure("This email address is already registered.");

        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHasher.Hash(password),
            DisplayName = $"{firstName} {lastName}",
            StudyProgram = course.StudyProgram,
            Course = course.Code,
            Role = role,
            IsActive = command.IsActive,
            MustChangePassword = true,
            OnboardingCompleted = false
        };

        await userRepository.AddAsync(user, cancellationToken);
        await SyncCourseAssignmentsAsync(course.Code, course.Code, cancellationToken);

        return Result<AdminUserDto>.Success(ToDto(user));
    }

    public async Task<Result<AdminUserDto>> UpdateUserAsync(UpdateAdminUserCommand command, CancellationToken cancellationToken = default)
    {
        var displayName = command.DisplayName.Trim();
        var email = command.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(displayName))
            return Result<AdminUserDto>.Failure("Display name is required.");

        if (displayName.Length > 120)
            return Result<AdminUserDto>.Failure("Display name must be at most 120 characters long.");

        var emailValidationError = ValidateEmail(email);
        if (emailValidationError is not null)
            return Result<AdminUserDto>.Failure(emailValidationError);

        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
            return Result<AdminUserDto>.Failure(InvalidRoleError);

        var course = await ResolveActiveCourseAsync(command.CourseCode, cancellationToken);
        if (course is null)
            return Result<AdminUserDto>.Failure(InvalidCourseError);

        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result<AdminUserDto>.Failure("User was not found.");

        if (user.Id == command.CurrentAdminId && role != UserRole.Admin)
            return Result<AdminUserDto>.Failure("You cannot remove your own admin role.");

        if (user.Id == command.CurrentAdminId && !command.IsActive)
            return Result<AdminUserDto>.Failure("You cannot deactivate your own admin account.");

        var existingUser = await userRepository.FindByEmailAsync(email, cancellationToken);
        if (existingUser is not null && existingUser.Id != user.Id)
            return Result<AdminUserDto>.Failure("This email address is already registered.");

        var previousCourse = user.Course;
        user.DisplayName = displayName;
        user.Email = email;
        user.Role = role;
        user.Course = course.Code;
        user.StudyProgram = course.StudyProgram;
        user.IsActive = command.IsActive;

        await userRepository.UpdateAsync(user, cancellationToken);
        await SyncCourseAssignmentsAsync(course.Code, previousCourse, cancellationToken);

        return Result<AdminUserDto>.Success(ToDto(user));
    }

    public async Task<Result<AdminUserDto>> UpdateStatusAsync(UpdateUserStatusCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result<AdminUserDto>.Failure("User was not found.");

        if (user.Id == command.CurrentAdminId && !command.IsActive)
            return Result<AdminUserDto>.Failure("You cannot deactivate your own admin account.");

        user.IsActive = command.IsActive;
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result<AdminUserDto>.Success(ToDto(user));
    }

    public async Task<Result<AdminUserDto>> UpdateRoleAsync(UpdateUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
            return Result<AdminUserDto>.Failure(InvalidRoleError);

        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result<AdminUserDto>.Failure("User was not found.");

        if (user.Id == command.CurrentAdminId && role != UserRole.Admin)
            return Result<AdminUserDto>.Failure("You cannot remove your own admin role.");

        user.Role = role;
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result<AdminUserDto>.Success(ToDto(user));
    }

    public async Task<Result<AdminUserDto>> UpdateCourseAsync(UpdateUserCourseCommand command, CancellationToken cancellationToken = default)
    {
        var courseCode = CoursesService.NormalizeCourseCode(command.CourseCode);
        var course = await ResolveActiveCourseAsync(courseCode, cancellationToken);
        if (course is null)
            return Result<AdminUserDto>.Failure(InvalidCourseError);

        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result<AdminUserDto>.Failure("User was not found.");

        var previousCourse = user.Course;
        user.Course = course.Code;
        user.StudyProgram = course.StudyProgram;

        await userRepository.UpdateAsync(user, cancellationToken);
        await SyncCourseAssignmentsAsync(course.Code, previousCourse, cancellationToken);

        return Result<AdminUserDto>.Success(ToDto(user));
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid userId, Guid currentAdminId, CancellationToken cancellationToken = default)
    {
        if (userId == currentAdminId)
            return Result<bool>.Failure("You cannot delete your own admin account.");

        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<bool>.Failure("User was not found.");

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
        user.Course,
        user.Role.ToString(),
        user.IsActive,
        user.CreatedAt);

    private async Task<Course?> ResolveActiveCourseAsync(string courseCode, CancellationToken cancellationToken)
    {
        var normalizedCourseCode = CoursesService.NormalizeCourseCode(courseCode);
        if (string.IsNullOrWhiteSpace(normalizedCourseCode))
            return null;

        var course = await courseRepository.FindByCodeAsync(normalizedCourseCode, cancellationToken);
        return course is { IsActive: true } ? course : null;
    }

    private static string? ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Email address is required.";

        if (!email.EndsWith("@dhbw-loerrach.de", StringComparison.OrdinalIgnoreCase))
            return "Only @dhbw-loerrach.de email addresses are allowed.";

        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase)
                ? null
                : "Enter a valid email address.";
        }
        catch (FormatException)
        {
            return "Enter a valid email address.";
        }
    }

    private async Task SyncProfileMetadataFromCourseAsync(User user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Course))
            return;

        var course = await courseRepository.FindByCodeAsync(CoursesService.NormalizeCourseCode(user.Course), cancellationToken);
        if (course is null)
            return;

        if (user.Course == course.Code && user.StudyProgram == course.StudyProgram)
            return;

        user.Course = course.Code;
        user.StudyProgram = course.StudyProgram;
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

            if (CoursesService.IsStudentCourse(course.Code))
            {
                await groupRepository.EnsureCourseGroupAsync(course.Code, course.StudyProgram);
                await groupRepository.SyncCourseAssignmentsAsync(
                    course.Code,
                    users.Where(user => string.Equals(user.Course, course.Code, StringComparison.OrdinalIgnoreCase)).Select(user => user.Id).ToList());
            }
        }
    }
}
