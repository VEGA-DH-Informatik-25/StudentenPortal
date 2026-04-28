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
    Task UpdateAssignmentsAsync(Guid id, IReadOnlyCollection<Guid> assignedUserIds);
    Task UpdateMemberPermissionsAsync(Guid id, IReadOnlyDictionary<Guid, GroupMemberPermission> permissions);
    Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds);
}
