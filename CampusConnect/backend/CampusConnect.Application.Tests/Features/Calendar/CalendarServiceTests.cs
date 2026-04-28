using CampusConnect.Application.Features.Calendar;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Calendar;

public sealed class CalendarServiceTests
{
    [Fact]
    public async Task GetExamsAsync_ShouldReturnCurrentUsersExamsOrderedByDate()
    {
        var userId = Guid.NewGuid();
        var service = new CalendarService(new FakeExamRepository(
            new ExamEntry { UserId = userId, ModuleName = "Später", ExamDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc) },
            new ExamEntry { UserId = userId, ModuleName = "Früher", ExamDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc) },
            new ExamEntry { UserId = Guid.NewGuid(), ModuleName = "Andere Person", ExamDate = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc) }));

        var exams = await service.GetExamsAsync(userId);

        Assert.Collection(
            exams,
            exam => Assert.Equal("Früher", exam.ModuleName),
            exam => Assert.Equal("Später", exam.ModuleName));
    }

    [Fact]
    public async Task AddExamAsync_ShouldRejectMissingModuleName()
    {
        var service = new CalendarService(new FakeExamRepository());

        var result = await service.AddExamAsync(new AddExamCommand(Guid.NewGuid(), " ", DateTime.UtcNow, null, null));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteExamAsync_ShouldRemoveOnlyCurrentUsersExam()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var exam = new ExamEntry { UserId = userId, ModuleName = "Mathematik", ExamDate = DateTime.UtcNow };
        var otherExam = new ExamEntry { UserId = otherUserId, ModuleName = "Mathematik", ExamDate = DateTime.UtcNow };
        var repository = new FakeExamRepository(exam, otherExam);
        var service = new CalendarService(repository);

        await service.DeleteExamAsync(exam.Id, userId);
        await service.DeleteExamAsync(otherExam.Id, userId);

        Assert.Empty(await repository.GetByUserAsync(userId));
        Assert.Single(await repository.GetByUserAsync(otherUserId));
    }

    private sealed class FakeExamRepository(params ExamEntry[] exams) : IExamRepository
    {
        private readonly List<ExamEntry> _exams = [.. exams];

        public Task<IReadOnlyList<ExamEntry>> GetByUserAsync(Guid userId) =>
            Task.FromResult<IReadOnlyList<ExamEntry>>(_exams.Where(exam => exam.UserId == userId).ToList());

        public Task AddAsync(ExamEntry entry)
        {
            _exams.Add(entry);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, Guid userId)
        {
            _exams.RemoveAll(exam => exam.Id == id && exam.UserId == userId);
            return Task.CompletedTask;
        }
    }
}