using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;

namespace CampusConnect.Domain.Interfaces;

public interface IGroupRepository
{
    Task<IReadOnlyList<CampusGroup>> GetAllAsync();
    Task<CampusGroup?> FindByIdAsync(Guid id);
    Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null);
    Task AddAsync(CampusGroup group);
    Task DeleteAsync(Guid id) =>
        throw new NotSupportedException();
    Task UpdateSettingsAsync(Guid id, GroupSettings settings);
    Task AddMembersAsync(Guid id, IReadOnlyCollection<Guid> userIds);
    Task RemoveMemberAsync(Guid id, Guid userId);
    Task SetOwnerAsync(Guid id, Guid ownerUserId, string ownerLabel) =>
        throw new NotSupportedException();
    Task SetMemberRoleAsync(Guid id, Guid userId, GroupRole role);
    Task AddJoinRequestAsync(Guid id, Guid userId);
    Task RemoveJoinRequestAsync(Guid id, Guid userId);
    Task AddInvitationsAsync(Guid id, IReadOnlyCollection<Guid> userIds);
    Task RemoveInvitationAsync(Guid id, Guid userId);
    Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds);
}
