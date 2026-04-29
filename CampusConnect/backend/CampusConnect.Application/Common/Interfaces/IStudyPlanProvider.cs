using CampusConnect.Domain.Entities;

namespace CampusConnect.Application.Common.Interfaces;

public interface IStudyPlanProvider
{
    Task<StudyPlan?> GetPlanForCourseAsync(Course course, CancellationToken cancellationToken = default);
}