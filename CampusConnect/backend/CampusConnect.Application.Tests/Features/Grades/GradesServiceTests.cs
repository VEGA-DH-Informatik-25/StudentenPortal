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
        var service = new GradesService(new FakeGradeRepository(
            new Grade { UserId = userId, ModuleName = "Mathematik", Value = 2.0m, Ects = 10 },
            new Grade { UserId = userId, ModuleName = "Programmieren", Value = 1.0m, Ects = 5 },
            new Grade { UserId = Guid.NewGuid(), ModuleName = "Andere Person", Value = 5.0m, Ects = 30 }));

        var summary = await service.GetGradesAsync(userId);

        Assert.Equal(2, summary.Grades.Count);
        Assert.Equal(15, summary.TotalEcts);
        Assert.Equal(1.67m, summary.WeightedAverage);
    }

    [Theory]
    [InlineData("", 2.0, 5)]
    [InlineData("Mathematik", 0.7, 5)]
    [InlineData("Mathematik", 5.3, 5)]
    [InlineData("Mathematik", 2.0, 0)]
    public async Task AddGradeAsync_ShouldRejectInvalidGradeInput(string moduleName, decimal value, int ects)
    {
        var service = new GradesService(new FakeGradeRepository());

        var result = await service.AddGradeAsync(new AddGradeCommand(Guid.NewGuid(), moduleName, value, ects));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteGradeAsync_ShouldRemoveOnlyCurrentUsersGrade()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var grade = new Grade { UserId = userId, ModuleName = "Mathematik", Value = 2.0m, Ects = 5 };
        var otherGrade = new Grade { UserId = otherUserId, ModuleName = "Mathematik", Value = 1.0m, Ects = 5 };
        var repository = new FakeGradeRepository(grade, otherGrade);
        var service = new GradesService(repository);

        await service.DeleteGradeAsync(grade.Id, userId);
        await service.DeleteGradeAsync(otherGrade.Id, userId);

        Assert.Empty(await repository.GetByUserAsync(userId));
        Assert.Single(await repository.GetByUserAsync(otherUserId));
    }

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