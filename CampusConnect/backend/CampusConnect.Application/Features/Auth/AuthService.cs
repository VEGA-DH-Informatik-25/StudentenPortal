using CampusConnect.Application.Common;
using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace CampusConnect.Application.Features.Auth;

public record RegisterCommand(string Email, string Password, string DisplayName, string StudyProgram, int Semester, string Course);
public record LoginCommand(string Email, string Password);
public record AuthResult(string Token, string DisplayName, string Email, string Role);

public class AuthService(IUserRepository userRepo, IJwtService jwtService)
{
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static bool VerifyPassword(string password, string hash) =>
        HashPassword(password) == hash;

    public async Task<Result<AuthResult>> RegisterAsync(RegisterCommand cmd)
    {
        if (!cmd.Email.EndsWith("@dhbw-loerrach.de", StringComparison.OrdinalIgnoreCase))
            return Result<AuthResult>.Failure("Nur @dhbw-loerrach.de E-Mail-Adressen sind erlaubt.");

        if (await userRepo.FindByEmailAsync(cmd.Email) is not null)
            return Result<AuthResult>.Failure("Diese E-Mail-Adresse ist bereits registriert.");

        var user = new User
        {
            Email = cmd.Email.ToLowerInvariant(),
            PasswordHash = HashPassword(cmd.Password),
            DisplayName = cmd.DisplayName,
            StudyProgram = cmd.StudyProgram,
            Semester = cmd.Semester,
            Course = cmd.Course
        };

        await userRepo.AddAsync(user);
        var token = jwtService.GenerateToken(user);
        return Result<AuthResult>.Success(new AuthResult(token, user.DisplayName, user.Email, user.Role.ToString()));
    }

    public async Task<Result<AuthResult>> LoginAsync(LoginCommand cmd)
    {
        var user = await userRepo.FindByEmailAsync(cmd.Email.ToLowerInvariant());
        if (user is null || !VerifyPassword(cmd.Password, user.PasswordHash))
            return Result<AuthResult>.Failure("Ungültige E-Mail-Adresse oder Passwort.");

        var token = jwtService.GenerateToken(user);
        return Result<AuthResult>.Success(new AuthResult(token, user.DisplayName, user.Email, user.Role.ToString()));
    }

    public async Task<User?> GetByIdAsync(Guid id) => await userRepo.FindByIdAsync(id);
}
