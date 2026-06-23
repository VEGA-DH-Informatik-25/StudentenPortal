using CampusConnect.Application.Common;
using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Common.Security;
using CampusConnect.Application.Features.Courses;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Auth;

public record LoginCommand(string Email, string Password);
public record UpdateUserProfileCommand(string DisplayName, string Course, string? PhoneNumber, string? Location, string? ProfileNote);
public record ChangeInitialPasswordCommand(string CurrentPassword, string NewPassword);
public record UserProfileResult(Guid Id, string Email, string DisplayName, string StudyProgram, string Course, string PhoneNumber, string Location, string ProfileNote, string Role, bool MustChangePassword, bool OnboardingCompleted, DateTime? OnboardingCompletedAt, DateTime CreatedAt);
public record AuthResult(string Token, UserProfileResult Profile);

public class AuthService(IUserRepository userRepo, IJwtService jwtService, ICourseRepository courseRepo, IGroupRepository groupRepo)
{
    public const string UserProfileNotFoundError = "User profile was not found.";
    private const string InvalidCourseError = "Choose a valid course.";
    private const string InvalidCredentialsError = "Invalid email address or password.";

    public async Task<Result<AuthResult>> LoginAsync(LoginCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Email) || string.IsNullOrEmpty(cmd.Password))
            return Result<AuthResult>.Failure(InvalidCredentialsError);

        var email = cmd.Email.Trim().ToLowerInvariant();
        var user = await userRepo.FindByEmailAsync(email);
        if (user is null || !PasswordHasher.Verify(cmd.Password, user.PasswordHash))
            return Result<AuthResult>.Failure(InvalidCredentialsError);

        if (!user.IsActive)
            return Result<AuthResult>.Failure(InvalidCredentialsError);

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

        validationError = ValidateContactFields(cmd.PhoneNumber, cmd.Location, cmd.ProfileNote);
        if (validationError is not null)
            return Result<UserProfileResult>.Failure(validationError);

        var user = await userRepo.FindByIdAsync(id);
        if (user is null)
            return Result<UserProfileResult>.Failure(UserProfileNotFoundError);

        var course = await ResolveCourseAsync(
            cmd.Course,
            requireActive: !string.Equals(user.Course, cmd.Course, StringComparison.OrdinalIgnoreCase),
            requireStudentCourse: user.Role == Domain.Enums.UserRole.Student);
        if (course is null)
            return Result<UserProfileResult>.Failure(InvalidCourseError);

        var previousCourse = user.Course;
        user.DisplayName = cmd.DisplayName.Trim();
        user.StudyProgram = course.StudyProgram;
        user.Course = course.Code;
        user.PhoneNumber = NormalizeOptional(cmd.PhoneNumber);
        user.Location = NormalizeOptional(cmd.Location);
        user.ProfileNote = NormalizeOptional(cmd.ProfileNote);

        await userRepo.UpdateAsync(user);
        await SyncCourseAssignmentsAsync(course.Code, previousCourse);
        return Result<UserProfileResult>.Success(ToProfileResult(user));
    }

    public async Task<Result<UserProfileResult>> ChangeInitialPasswordAsync(Guid id, ChangeInitialPasswordCommand cmd)
    {
        var user = await userRepo.FindByIdAsync(id);
        if (user is null)
            return Result<UserProfileResult>.Failure(UserProfileNotFoundError);

        if (!PasswordHasher.Verify(cmd.CurrentPassword, user.PasswordHash))
            return Result<UserProfileResult>.Failure("The current password is incorrect.");

        var passwordError = ValidatePassword(cmd.NewPassword);
        if (passwordError is not null)
            return Result<UserProfileResult>.Failure(passwordError);

        user.PasswordHash = PasswordHasher.Hash(cmd.NewPassword);
        user.MustChangePassword = false;
        await userRepo.UpdateAsync(user);
        return Result<UserProfileResult>.Success(ToProfileResult(user));
    }

    public async Task<Result<UserProfileResult>> CompleteOnboardingAsync(Guid id)
    {
        var user = await userRepo.FindByIdAsync(id);
        if (user is null)
            return Result<UserProfileResult>.Failure(UserProfileNotFoundError);

        if (user.MustChangePassword)
            return Result<UserProfileResult>.Failure("Change the initial password before completing onboarding.");

        if (!user.OnboardingCompleted)
        {
            user.OnboardingCompleted = true;
            user.OnboardingCompletedAt = DateTime.UtcNow;
            await userRepo.UpdateAsync(user);
        }

        return Result<UserProfileResult>.Success(ToProfileResult(user));
    }

    private static UserProfileResult ToProfileResult(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.StudyProgram, user.Course, user.PhoneNumber, user.Location, user.ProfileNote, user.Role.ToString(), user.MustChangePassword, user.OnboardingCompleted, user.OnboardingCompletedAt, user.CreatedAt);

    private async Task<Course?> ResolveCourseAsync(string courseCode, bool requireActive, bool requireStudentCourse)
    {
        if (string.IsNullOrWhiteSpace(courseCode))
            return null;

        var course = await courseRepo.FindByCodeAsync(CoursesService.NormalizeCourseCode(courseCode));
        if (course is null || requireActive && !course.IsActive || requireStudentCourse && !CoursesService.IsStudentCourse(course.Code))
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

        if (user.Course == course.Code && user.StudyProgram == course.StudyProgram)
            return;

        user.Course = course.Code;
        user.StudyProgram = course.StudyProgram;
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

            if (CoursesService.IsStudentCourse(course.Code))
            {
                await groupRepo.EnsureCourseGroupAsync(course.Code, course.StudyProgram);
                await groupRepo.SyncCourseAssignmentsAsync(
                    course.Code,
                    users.Where(user => string.Equals(user.Course, course.Code, StringComparison.OrdinalIgnoreCase)).Select(user => user.Id).ToList());
            }
        }
    }

    private static string? ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "Fill in all profile fields.";

        if (displayName.Trim().Length > 120)
            return "Display name must be at most 120 characters long.";

        return null;
    }

    private static string? ValidateContactFields(string? phoneNumber, string? location, string? profileNote)
    {
        if (NormalizeOptional(phoneNumber).Length > 40)
            return "Phone number must be at most 40 characters long.";

        if (NormalizeOptional(location).Length > 120)
            return "Location must be at most 120 characters long.";

        if (NormalizeOptional(profileNote).Length > 280)
            return "Profile note must be at most 280 characters long.";

        return null;
    }

    private static string NormalizeOptional(string? value) => value?.Trim() ?? string.Empty;

    private static string? ValidatePassword(string password)
    {
        if (password.Length < 8)
            return "Password must be at least 8 characters long.";
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(character => !char.IsLetterOrDigit(character)))
            return "Password must contain uppercase and lowercase letters, a number, and a special character.";

        return null;
    }
}
