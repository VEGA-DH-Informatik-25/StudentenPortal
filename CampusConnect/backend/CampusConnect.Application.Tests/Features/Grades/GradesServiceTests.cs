using CampusConnect.Application.Features.Grades;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Grades;

public sealed class GradesServiceTests
{
    [Fact]
    public async Task GetGradesAsync_ShouldCalculateWeightedAverageByEcts()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(new FakeGradeRepository(
            new Grade { UserId = userId, ModuleName = "Mathematics", Value = 2.0m, Ects = 10 },
            new Grade { UserId = userId, ModuleName = "Programming", Value = 1.0m, Ects = 5 },
            new Grade { UserId = Guid.NewGuid(), ModuleName = "Other person", Value = 5.0m, Ects = 30 }));

        var summary = await service.GetGradesAsync(userId);

        Assert.Equal(2, summary.Grades.Count);
        Assert.Equal(15, summary.TotalEcts);
        Assert.Equal(1.67m, summary.WeightedAverage);
    }

    [Theory]
    [InlineData("", 2.0, 5)]
    [InlineData("Mathematics", 0.7, 5)]
    [InlineData("Mathematics", 5.3, 5)]
    [InlineData("Mathematics", 2.0, 0)]
    public async Task AddGradeAsync_ShouldRejectInvalidGradeInput(string moduleName, decimal value, int ects)
    {
        var service = CreateService(new FakeGradeRepository());

        var result = await service.AddGradeAsync(new AddGradeCommand(Guid.NewGuid(), moduleName, value, ects));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task AddGradeAsync_ShouldSaveManualGrade()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeGradeRepository();
        var service = CreateService(repository);

        var result = await service.AddGradeAsync(new AddGradeCommand(userId, "Mathematics I", 1.7m, 5, "T4INF1001"));

        Assert.True(result.IsSuccess);
        Assert.Equal("T4INF1001", result.Value!.ModuleCode);
        Assert.Equal("Mathematics I", result.Value.ModuleName);
        Assert.Equal(5, result.Value.Ects);
        var saved = Assert.Single(await repository.GetByUserAsync(userId));
        Assert.Equal("T4INF1001", saved.ModuleCode);
    }

    [Fact]
    public async Task DeleteGradeAsync_ShouldRemoveOnlyCurrentUsersGrade()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var grade = new Grade { UserId = userId, ModuleName = "Mathematics", Value = 2.0m, Ects = 5 };
        var otherGrade = new Grade { UserId = otherUserId, ModuleName = "Mathematics", Value = 1.0m, Ects = 5 };
        var repository = new FakeGradeRepository(grade, otherGrade);
        var service = CreateService(repository);

        await service.DeleteGradeAsync(grade.Id, userId);
        await service.DeleteGradeAsync(otherGrade.Id, userId);

        Assert.Empty(await repository.GetByUserAsync(userId));
        Assert.Single(await repository.GetByUserAsync(otherUserId));
    }

    private static GradesService CreateService(FakeGradeRepository gradeRepository) => new(gradeRepository);

    private sealed class FakeGradeRepository(params Grade[] grades) : IGradeRepository
    {
        private readonly List<Grade> _grades = [.. grades];

        public Task<IReadOnlyList<Grade>> GetByUserAsync(Guid userId) =>
            Task.FromResult<IReadOnlyList<Grade>>(_grades.Where(grade => grade.UserId == userId).ToList());

        public Task AddAsync(Grade grade)
        {
            _grades.Add(grade);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, Guid userId)
        {
            _grades.RemoveAll(grade => grade.Id == id && grade.UserId == userId);
            return Task.CompletedTask;
        }
    }

}
