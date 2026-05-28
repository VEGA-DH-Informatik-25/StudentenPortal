using CampusConnect.Application.Common;
using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Grades;

public record AddGradeCommand(Guid UserId, string? ModuleName, decimal Value, int? Ects, string? ModuleCode = null);
public record GradeDto(Guid Id, string ModuleName, string ModuleCode, decimal Value, int Ects, DateTime CreatedAt);
public record GradeSummaryDto(IReadOnlyList<GradeDto> Grades, decimal WeightedAverage, int TotalEcts);
public record GradePlanDto(string CourseCode, string StudyProgram, string SourceUrl, DateTime RetrievedAt, IReadOnlyList<GradePlanModuleDto> Modules);
public record GradePlanModuleDto(string Code, string Name, int? StudyYear, int Ects, bool IsRequired, bool IsCompleted, decimal? Grade, IReadOnlyList<GradePlanExamDto> Exams);
public record GradePlanExamDto(string Name, string Scope, bool? IsGraded);

public class GradesService(
    IGradeRepository gradeRepo,
    IUserRepository userRepo,
    ICourseRepository courseRepo,
    IStudyPlanProvider studyPlanProvider)
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

    public async Task<Result<GradePlanDto>> GetPlanAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepo.FindByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<GradePlanDto>.Failure("User profile was not found.");

        var course = await courseRepo.FindByCodeAsync(user.Course, cancellationToken);
        if (course is null)
            return Result<GradePlanDto>.Failure("No valid course is assigned to your profile.");

        var plan = await studyPlanProvider.GetPlanForCourseAsync(course, cancellationToken);
        if (plan is null)
            return Result<GradePlanDto>.Failure("No DHBW study plan was found for your course.");

        var grades = await gradeRepo.GetByUserAsync(userId);
        var gradesByModule = grades
            .Where(grade => !string.IsNullOrWhiteSpace(grade.ModuleCode))
            .GroupBy(grade => grade.ModuleCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(grade => grade.CreatedAt).First(), StringComparer.OrdinalIgnoreCase);
        var gradesByModuleName = grades
            .Where(grade => string.IsNullOrWhiteSpace(grade.ModuleCode))
            .GroupBy(grade => NormalizeModuleName(grade.ModuleName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(grade => grade.CreatedAt).First(), StringComparer.OrdinalIgnoreCase);

        var modules = plan.Modules.Select(module =>
        {
            if (!gradesByModule.TryGetValue(module.Code, out var grade))
                gradesByModuleName.TryGetValue(NormalizeModuleName(module.Name), out grade);

            return new GradePlanModuleDto(
                module.Code,
                module.Name,
                module.StudyYear,
                module.Ects,
                module.IsRequired,
                grade is not null,
                grade?.Value,
                module.Exams.Select(exam => new GradePlanExamDto(exam.Name, exam.Scope, exam.IsGraded)).ToList());
        }).ToList();

        return Result<GradePlanDto>.Success(new GradePlanDto(course.Code, plan.StudyProgram, plan.SourceUrl, plan.RetrievedAt, modules));
    }

    public async Task<Result<GradeDto>> AddGradeAsync(AddGradeCommand cmd, CancellationToken cancellationToken = default)
    {
        if (cmd.Value < 1.0m || cmd.Value > 5.0m)
            return Result<GradeDto>.Failure("Grade must be between 1.0 and 5.0.");

        var resolvedModule = string.IsNullOrWhiteSpace(cmd.ModuleCode)
            ? ResolveManualModule(cmd.ModuleName, cmd.Ects)
            : await ResolvePlannedModuleAsync(cmd.UserId, cmd.ModuleCode, cancellationToken);

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

    private async Task<Result<ResolvedGradeModule>> ResolvePlannedModuleAsync(Guid userId, string moduleCode, CancellationToken cancellationToken)
    {
        var planResult = await GetPlanAsync(userId, cancellationToken);
        if (!planResult.IsSuccess)
            return Result<ResolvedGradeModule>.Failure(planResult.Error!);

        var normalizedCode = moduleCode.Trim();
        var module = planResult.Value!.Modules.FirstOrDefault(item => item.Code.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase));
        if (module is null)
            return Result<ResolvedGradeModule>.Failure("This module does not belong to the assigned course plan.");

        return Result<ResolvedGradeModule>.Success(new ResolvedGradeModule(module.Code, module.Name, module.Ects));
    }

    private static Result<ResolvedGradeModule> ResolveManualModule(string? moduleName, int? ects)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            return Result<ResolvedGradeModule>.Failure("Module name cannot be empty.");

        return Result<ResolvedGradeModule>.Success(new ResolvedGradeModule(string.Empty, moduleName.Trim(), ects ?? 0));
    }

    private static GradeDto ToDto(Grade grade) => new(grade.Id, grade.ModuleName, grade.ModuleCode, grade.Value, grade.Ects, grade.CreatedAt);

    private static string NormalizeModuleName(string moduleName) => moduleName.Trim();

    private sealed record ResolvedGradeModule(string Code, string Name, int Ects);
}
