using CampusConnect.Application.Common;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Grades;

public record AddGradeCommand(Guid UserId, string ModuleName, decimal Value, int Ects);
public record GradeDto(Guid Id, string ModuleName, decimal Value, int Ects, DateTime CreatedAt);
public record GradeSummaryDto(IReadOnlyList<GradeDto> Grades, decimal WeightedAverage, int TotalEcts);

public class GradesService(IGradeRepository gradeRepo)
{
    public async Task<GradeSummaryDto> GetGradesAsync(Guid userId)
    {
        var grades = await gradeRepo.GetByUserAsync(userId);
        var dtos = grades.Select(g => new GradeDto(g.Id, g.ModuleName, g.Value, g.Ects, g.CreatedAt)).ToList();

        var totalEcts = dtos.Sum(g => g.Ects);
        var weightedAverage = totalEcts > 0
            ? dtos.Sum(g => g.Value * g.Ects) / totalEcts
            : 0m;

        return new GradeSummaryDto(dtos, Math.Round(weightedAverage, 2), totalEcts);
    }

    public async Task<Result<GradeDto>> AddGradeAsync(AddGradeCommand cmd)
    {
        if (cmd.Value < 1.0m || cmd.Value > 5.0m)
            return Result<GradeDto>.Failure("Note muss zwischen 1,0 und 5,0 liegen.");

        var grade = new Grade
        {
            UserId = cmd.UserId,
            ModuleName = cmd.ModuleName.Trim(),
            Value = cmd.Value,
            Ects = cmd.Ects
        };
        await gradeRepo.AddAsync(grade);
        return Result<GradeDto>.Success(new GradeDto(grade.Id, grade.ModuleName, grade.Value, grade.Ects, grade.CreatedAt));
    }
}
