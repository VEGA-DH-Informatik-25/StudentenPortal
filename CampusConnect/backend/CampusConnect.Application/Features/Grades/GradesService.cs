using CampusConnect.Application.Common;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Grades;

public record AddGradeCommand(Guid UserId, string? ModuleName, decimal Value, int? Ects, string? ModuleCode = null);
public record GradeDto(Guid Id, string ModuleName, string ModuleCode, decimal Value, int Ects, DateTime CreatedAt);
public record GradeSummaryDto(IReadOnlyList<GradeDto> Grades, decimal WeightedAverage, int TotalEcts);

public class GradesService(IGradeRepository gradeRepo)
{
    public async Task<GradeSummaryDto> GetGradesAsync(Guid userId)
    {
        var grades = await gradeRepo.GetByUserAsync(userId);
        var dtos = grades.Select(ToDto).ToList();

        var totalEcts = dtos.Sum(g => g.Ects);
        var weightedAverage = totalEcts > 0
            ? dtos.Sum(g => g.Value * g.Ects) / totalEcts
            : 0m;

        return new GradeSummaryDto(dtos, Math.Round(weightedAverage, 2), totalEcts);
    }

    public async Task<Result<GradeDto>> AddGradeAsync(AddGradeCommand cmd, CancellationToken cancellationToken = default)
    {
        if (cmd.Value < 1.0m || cmd.Value > 5.0m)
            return Result<GradeDto>.Failure("Grade must be between 1.0 and 5.0.");

        var resolvedModule = ResolveManualModule(cmd.ModuleName, cmd.Ects, cmd.ModuleCode);

        if (!resolvedModule.IsSuccess)
            return Result<GradeDto>.Failure(resolvedModule.Error!);

        var module = resolvedModule.Value!;
        if (module.Ects <= 0)
            return Result<GradeDto>.Failure("ECTS points must be greater than 0.");

        var grade = new Grade
        {
            UserId = cmd.UserId,
            ModuleCode = module.Code,
            ModuleName = module.Name,
            Value = cmd.Value,
            Ects = module.Ects
        };
        await gradeRepo.AddAsync(grade);
        return Result<GradeDto>.Success(ToDto(grade));
    }

    public async Task<Result<bool>> DeleteGradeAsync(Guid gradeId, Guid userId)
    {
        await gradeRepo.DeleteAsync(gradeId, userId);
        return Result<bool>.Success(true);
    }

    private static Result<ResolvedGradeModule> ResolveManualModule(string? moduleName, int? ects, string? moduleCode)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            return Result<ResolvedGradeModule>.Failure("Module name cannot be empty.");

        return Result<ResolvedGradeModule>.Success(new ResolvedGradeModule(moduleCode?.Trim() ?? string.Empty, moduleName.Trim(), ects ?? 0));
    }

    private static GradeDto ToDto(Grade grade) => new(grade.Id, grade.ModuleName, grade.ModuleCode, grade.Value, grade.Ects, grade.CreatedAt);

    private sealed record ResolvedGradeModule(string Code, string Name, int Ects);
}
