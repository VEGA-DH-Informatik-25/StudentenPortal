using CampusConnect.Application.Common;
using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Common.Security;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Auth;

public record RegisterCommand(string Email, string Password, string DisplayName, string StudyProgram, int Semester, string Course);
public record LoginCommand(string Email, string Password);
public record UpdateUserProfileCommand(string DisplayName, string StudyProgram, int Semester, string Course);
public record UserProfileResult(Guid Id, string Email, string DisplayName, string StudyProgram, int Semester, string Course, string Role, DateTime CreatedAt);
public record AuthResult(string Token, UserProfileResult Profile);

public class AuthService(IUserRepository userRepo, IJwtService jwtService)
{
    public const string UserProfileNotFoundError = "Benutzerprofil wurde nicht gefunden.";

    public async Task<Result<AuthResult>> RegisterAsync(RegisterCommand cmd)
    {
        var email = cmd.Email.Trim().ToLowerInvariant();
        if (!email.EndsWith("@dhbw-loerrach.de", StringComparison.OrdinalIgnoreCase))
            return Result<AuthResult>.Failure("Nur @dhbw-loerrach.de E-Mail-Adressen sind erlaubt.");

        var validationError = ValidateProfile(cmd.DisplayName, cmd.StudyProgram, cmd.Semester, cmd.Course);
        if (validationError is not null)
            return Result<AuthResult>.Failure(validationError);

        if (await userRepo.FindByEmailAsync(email) is not null)
            return Result<AuthResult>.Failure("Diese E-Mail-Adresse ist bereits registriert.");

        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHasher.Hash(cmd.Password),
            DisplayName = cmd.DisplayName.Trim(),
            StudyProgram = cmd.StudyProgram.Trim(),
            Semester = cmd.Semester,
            Course = cmd.Course.Trim()
        };

        await userRepo.AddAsync(user);
        var token = jwtService.GenerateToken(user);
        return Result<AuthResult>.Success(new AuthResult(token, ToProfileResult(user)));
    }

    public async Task<Result<AuthResult>> LoginAsync(LoginCommand cmd)
    {
        var user = await userRepo.FindByEmailAsync(cmd.Email.ToLowerInvariant());
        if (user is null || !PasswordHasher.Verify(cmd.Password, user.PasswordHash))
            return Result<AuthResult>.Failure("Ungültige E-Mail-Adresse oder Passwort.");

        var token = jwtService.GenerateToken(user);
        return Result<AuthResult>.Success(new AuthResult(token, ToProfileResult(user)));
    }

    public async Task<Result<UserProfileResult>> GetProfileAsync(Guid id)
    {
        var user = await userRepo.FindByIdAsync(id);
        return user is null
            ? Result<UserProfileResult>.Failure(UserProfileNotFoundError)
            : Result<UserProfileResult>.Success(ToProfileResult(user));
    }

    public async Task<Result<UserProfileResult>> UpdateProfileAsync(Guid id, UpdateUserProfileCommand cmd)
    {
        var validationError = ValidateProfile(cmd.DisplayName, cmd.StudyProgram, cmd.Semester, cmd.Course);
        if (validationError is not null)
            return Result<UserProfileResult>.Failure(validationError);

        var user = await userRepo.FindByIdAsync(id);
        if (user is null)
            return Result<UserProfileResult>.Failure(UserProfileNotFoundError);

        user.DisplayName = cmd.DisplayName.Trim();
        user.StudyProgram = cmd.StudyProgram.Trim();
        user.Semester = cmd.Semester;
        user.Course = cmd.Course.Trim();

        await userRepo.UpdateAsync(user);
        return Result<UserProfileResult>.Success(ToProfileResult(user));
    }

    private static UserProfileResult ToProfileResult(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.StudyProgram, user.Semester, user.Course, user.Role.ToString(), user.CreatedAt);

    private static string? ValidateProfile(string displayName, string studyProgram, int semester, string course)
    {
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(studyProgram) || string.IsNullOrWhiteSpace(course))
            return "Bitte fülle alle Profilfelder aus.";

        if (semester is < 1 or > 6)
            return "Das Semester muss zwischen 1 und 6 liegen.";

        if (displayName.Trim().Length > 120)
            return "Der Anzeigename darf höchstens 120 Zeichen lang sein.";

        if (studyProgram.Trim().Length > 120)
            return "Der Studiengang darf höchstens 120 Zeichen lang sein.";

        if (course.Trim().Length > 40)
            return "Der Kurs darf höchstens 40 Zeichen lang sein.";

        return null;
    }
}
