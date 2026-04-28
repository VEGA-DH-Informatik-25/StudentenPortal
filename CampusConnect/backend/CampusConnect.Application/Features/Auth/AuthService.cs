using CampusConnect.Application.Common;
using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Common.Security;
using CampusConnect.Application.Features.Courses;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Auth;

public record RegisterCommand(string Email, string Password, string DisplayName, string Course);
public record LoginCommand(string Email, string Password);
public record UpdateUserProfileCommand(string DisplayName, string Course);
public record UserProfileResult(Guid Id, string Email, string DisplayName, string StudyProgram, int Semester, string Course, string Role, DateTime CreatedAt);
public record AuthResult(string Token, UserProfileResult Profile);

public class AuthService(IUserRepository userRepo, IJwtService jwtService, ICourseRepository courseRepo, IGroupRepository groupRepo)
{
    public const string UserProfileNotFoundError = "Benutzerprofil wurde nicht gefunden.";
    private const string InvalidCourseError = "Bitte wähle einen gültigen Kurs aus.";

    public async Task<Result<AuthResult>> RegisterAsync(RegisterCommand cmd)
    {
        var email = cmd.Email.Trim().ToLowerInvariant();
        if (!email.EndsWith("@dhbw-loerrach.de", StringComparison.OrdinalIgnoreCase))
            return Result<AuthResult>.Failure("Nur @dhbw-loerrach.de E-Mail-Adressen sind erlaubt.");

        var validationError = ValidateDisplayName(cmd.DisplayName);
        if (validationError is not null)
            return Result<AuthResult>.Failure(validationError);

        var course = await ResolveCourseAsync(cmd.Course, requireActive: true);
        if (course is null)
            return Result<AuthResult>.Failure(InvalidCourseError);

        if (await userRepo.FindByEmailAsync(email) is not null)
            return Result<AuthResult>.Failure("Diese E-Mail-Adresse ist bereits registriert.");

        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHasher.Hash(cmd.Password),
            DisplayName = cmd.DisplayName.Trim(),
            StudyProgram = course.StudyProgram,
            Semester = course.Semester,
            Course = course.Code
        };

        await userRepo.AddAsync(user);
        await SyncCourseAssignmentsAsync(course.Code);
        var token = jwtService.GenerateToken(user);
        return Result<AuthResult>.Success(new AuthResult(token, ToProfileResult(user)));
    }

    public async Task<Result<AuthResult>> LoginAsync(LoginCommand cmd)
    {
        var user = await userRepo.FindByEmailAsync(cmd.Email.ToLowerInvariant());
        if (user is null || !PasswordHasher.Verify(cmd.Password, user.PasswordHash))
            return Result<AuthResult>.Failure("Ungültige E-Mail-Adresse oder Passwort.");

        await SyncProfileMetadataFromCourseAsync(user);
        var token = jwtService.GenerateToken(user);
        return Result<AuthResult>.Success(new AuthResult(token, ToProfileResult(user)));
    }

    public async Task<Result<UserProfileResult>> GetProfileAsync(Guid id)
    {
        var user = await userRepo.FindByIdAsync(id);
        return user is null
            ? Result<UserProfileResult>.Failure(UserProfileNotFoundError)
            : Result<UserProfileResult>.Success(await ToSynchronizedProfileResultAsync(user));
    }

    public async Task<Result<UserProfileResult>> UpdateProfileAsync(Guid id, UpdateUserProfileCommand cmd)
    {
        var validationError = ValidateDisplayName(cmd.DisplayName);
        if (validationError is not null)
            return Result<UserProfileResult>.Failure(validationError);

        var user = await userRepo.FindByIdAsync(id);
        if (user is null)
            return Result<UserProfileResult>.Failure(UserProfileNotFoundError);

        var course = await ResolveCourseAsync(cmd.Course, requireActive: !string.Equals(user.Course, cmd.Course, StringComparison.OrdinalIgnoreCase));
        if (course is null)
            return Result<UserProfileResult>.Failure(InvalidCourseError);

        var previousCourse = user.Course;
        user.DisplayName = cmd.DisplayName.Trim();
        user.StudyProgram = course.StudyProgram;
        user.Semester = course.Semester;
        user.Course = course.Code;

        await userRepo.UpdateAsync(user);
        await SyncCourseAssignmentsAsync(course.Code, previousCourse);
        return Result<UserProfileResult>.Success(ToProfileResult(user));
    }

    private static UserProfileResult ToProfileResult(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.StudyProgram, user.Semester, user.Course, user.Role.ToString(), user.CreatedAt);

    private async Task<Course?> ResolveCourseAsync(string courseCode, bool requireActive)
    {
        if (string.IsNullOrWhiteSpace(courseCode))
            return null;

        var course = await courseRepo.FindByCodeAsync(CoursesService.NormalizeCourseCode(courseCode));
        if (course is null || requireActive && !course.IsActive)
            return null;

        return course;
    }

    private async Task<UserProfileResult> ToSynchronizedProfileResultAsync(User user)
    {
        await SyncProfileMetadataFromCourseAsync(user);
        return ToProfileResult(user);
    }

    private async Task SyncProfileMetadataFromCourseAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Course))
            return;

        var course = await courseRepo.FindByCodeAsync(CoursesService.NormalizeCourseCode(user.Course));
        if (course is null)
            return;

        if (user.Course == course.Code && user.StudyProgram == course.StudyProgram && user.Semester == course.Semester)
            return;

        user.Course = course.Code;
        user.StudyProgram = course.StudyProgram;
        user.Semester = course.Semester;
        await userRepo.UpdateAsync(user);
    }

    private async Task SyncCourseAssignmentsAsync(params string[] courseCodes)
    {
        var users = await userRepo.ListAsync();
        foreach (var courseCode in courseCodes.Where(code => !string.IsNullOrWhiteSpace(code)).Select(CoursesService.NormalizeCourseCode).Distinct())
        {
            var course = await courseRepo.FindByCodeAsync(courseCode);
            if (course is null)
                continue;

            await groupRepo.EnsureCourseGroupAsync(course.Code, course.StudyProgram);
            await groupRepo.SyncCourseAssignmentsAsync(
                course.Code,
                users.Where(user => string.Equals(user.Course, course.Code, StringComparison.OrdinalIgnoreCase)).Select(user => user.Id).ToList());
        }
    }

    private static string? ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "Bitte fülle alle Profilfelder aus.";

        if (displayName.Trim().Length > 120)
            return "Der Anzeigename darf höchstens 120 Zeichen lang sein.";

        return null;
    }
}
