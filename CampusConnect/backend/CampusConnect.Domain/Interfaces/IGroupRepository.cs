using CampusConnect.Domain.Entities;

namespace CampusConnect.Domain.Interfaces;

public interface IGroupRepository
{
    Task<IReadOnlyList<CampusGroup>> GetAllAsync();
    Task<CampusGroup?> FindByIdAsync(Guid id);
    Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null);
    Task AddAsync(CampusGroup group);
    Task UpdateSettingsAsync(Guid id, GroupSettings settings);
    Task UpdateAssignmentsAsync(Guid id, IReadOnlyCollection<Guid> assignedUserIds);
}
