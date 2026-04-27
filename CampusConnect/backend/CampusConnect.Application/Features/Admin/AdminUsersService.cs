using CampusConnect.Application.Common;
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

public class AdminUsersService(IUserRepository userRepository)
{
    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.ListAsync(cancellationToken);
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

    public async Task<Result<bool>> DeleteUserAsync(Guid userId, Guid currentAdminId, CancellationToken cancellationToken = default)
    {
        if (userId == currentAdminId)
            return Result<bool>.Failure("Du kannst dein eigenes Admin-Konto nicht löschen.");

        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<bool>.Failure("Benutzer wurde nicht gefunden.");

        await userRepository.DeleteAsync(userId, cancellationToken);
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
}