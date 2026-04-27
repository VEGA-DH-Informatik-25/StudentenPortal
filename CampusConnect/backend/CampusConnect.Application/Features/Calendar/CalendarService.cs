using CampusConnect.Application.Common;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Calendar;

public record AddExamCommand(Guid UserId, string ModuleName, DateTime ExamDate, string? Location, string? Notes);
public record ExamEntryDto(Guid Id, string ModuleName, DateTime ExamDate, string? Location, string? Notes);

public class CalendarService(IExamRepository examRepo)
{
    public async Task<IReadOnlyList<ExamEntryDto>> GetExamsAsync(Guid userId)
    {
        var exams = await examRepo.GetByUserAsync(userId);
        return exams
            .OrderBy(e => e.ExamDate)
            .Select(e => new ExamEntryDto(e.Id, e.ModuleName, e.ExamDate, e.Location, e.Notes))
            .ToList();
    }

    public async Task<Result<ExamEntryDto>> AddExamAsync(AddExamCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.ModuleName))
            return Result<ExamEntryDto>.Failure("Modulname darf nicht leer sein.");

        var entry = new ExamEntry
        {
            UserId = cmd.UserId,
            ModuleName = cmd.ModuleName.Trim(),
            ExamDate = cmd.ExamDate,
            Location = cmd.Location?.Trim(),
            Notes = cmd.Notes?.Trim()
        };
        await examRepo.AddAsync(entry);
        return Result<ExamEntryDto>.Success(new ExamEntryDto(entry.Id, entry.ModuleName, entry.ExamDate, entry.Location, entry.Notes));
    }

    public async Task<Result<bool>> DeleteExamAsync(Guid examId, Guid userId)
    {
        await examRepo.DeleteAsync(examId, userId);
        return Result<bool>.Success(true);
    }
}
