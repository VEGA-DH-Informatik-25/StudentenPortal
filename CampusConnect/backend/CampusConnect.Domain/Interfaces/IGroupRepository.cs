using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;

namespace CampusConnect.Domain.Interfaces;

public interface IGroupRepository
{
    Task<IReadOnlyList<CampusGroup>> GetAllAsync();
    Task<CampusGroup?> FindByIdAsync(Guid id);
    Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null);
    Task AddAsync(CampusGroup group);
    Task UpdateSettingsAsync(Guid id, GroupSettings settings);
    Task AddMembersAsync(Guid id, IReadOnlyCollection<Guid> userIds);
    Task RemoveMemberAsync(Guid id, Guid userId);
    Task SetMemberRoleAsync(Guid id, Guid userId, GroupRole role);
    Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds);
}
