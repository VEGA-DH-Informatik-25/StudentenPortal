using CampusConnect.Application.Common.Interfaces;
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
        var service = CreateService(new FakeGradeRepository());

        var result = await service.AddGradeAsync(new AddGradeCommand(Guid.NewGuid(), moduleName, value, ects));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task AddGradeAsync_WithPlannedModule_ShouldUsePlanModuleNameAndEcts()
    {
        var userId = Guid.NewGuid();
        var course = new Course { Code = "TIF25A", StudyProgram = "Informatik", Semester = 2 };
        var repository = new FakeGradeRepository();
        var service = CreateService(
            repository,
            new FakeUserRepository(new User { Id = userId, Course = course.Code, StudyProgram = course.StudyProgram, Semester = course.Semester }),
            new FakeCourseRepository(course),
            new FakeStudyPlanProvider(new StudyPlan(
                "Informatik",
                "https://example.invalid/Informatik.pdf",
                DateTime.UtcNow,
                [new StudyPlanModule("T4INF1001", "Mathematik I", 1, 5, true, [new StudyPlanExam("Klausur", "Siehe Pruefungsordnung", true)])])));

        var result = await service.AddGradeAsync(new AddGradeCommand(userId, null, 1.7m, null, "T4INF1001"));

        Assert.True(result.IsSuccess);
        Assert.Equal("T4INF1001", result.Value!.ModuleCode);
        Assert.Equal("Mathematik I", result.Value.ModuleName);
        Assert.Equal(5, result.Value.Ects);
        var saved = Assert.Single(await repository.GetByUserAsync(userId));
        Assert.Equal("T4INF1001", saved.ModuleCode);
    }

    [Fact]
    public async Task GetPlanAsync_ShouldMarkCompletedModulesFromExistingGrades()
    {
        var userId = Guid.NewGuid();
        var course = new Course { Code = "TIF25A", StudyProgram = "Informatik", Semester = 2 };
        var service = CreateService(
            new FakeGradeRepository(new Grade { UserId = userId, ModuleCode = "T4INF1001", ModuleName = "Mathematik I", Value = 2.0m, Ects = 5 }),
            new FakeUserRepository(new User { Id = userId, Course = course.Code, StudyProgram = course.StudyProgram, Semester = course.Semester }),
            new FakeCourseRepository(course),
            new FakeStudyPlanProvider(new StudyPlan(
                "Informatik",
                "https://example.invalid/Informatik.pdf",
                DateTime.UtcNow,
                [
                    new StudyPlanModule("T4INF1001", "Mathematik I", 1, 5, true, []),
                    new StudyPlanModule("T4INF1002", "Theoretische Informatik I", 1, 5, true, [])
                ])));

        var result = await service.GetPlanAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Modules.Count);
        Assert.True(result.Value.Modules.Single(module => module.Code == "T4INF1001").IsCompleted);
        Assert.False(result.Value.Modules.Single(module => module.Code == "T4INF1002").IsCompleted);
    }

    [Fact]
    public async Task DeleteGradeAsync_ShouldRemoveOnlyCurrentUsersGrade()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var grade = new Grade { UserId = userId, ModuleName = "Mathematik", Value = 2.0m, Ects = 5 };
        var otherGrade = new Grade { UserId = otherUserId, ModuleName = "Mathematik", Value = 1.0m, Ects = 5 };
        var repository = new FakeGradeRepository(grade, otherGrade);
        var service = CreateService(repository);

        await service.DeleteGradeAsync(grade.Id, userId);
        await service.DeleteGradeAsync(otherGrade.Id, userId);

        Assert.Empty(await repository.GetByUserAsync(userId));
        Assert.Single(await repository.GetByUserAsync(otherUserId));
    }

    private static GradesService CreateService(
        FakeGradeRepository gradeRepository,
        IUserRepository? userRepository = null,
        ICourseRepository? courseRepository = null,
        IStudyPlanProvider? studyPlanProvider = null) =>
        new(
            gradeRepository,
            userRepository ?? new FakeUserRepository(),
            courseRepository ?? new FakeCourseRepository(),
            studyPlanProvider ?? new FakeStudyPlanProvider(null));

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

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        private readonly Dictionary<Guid, User> _users = users.ToDictionary(user => user.Id);

        public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(_users.Values.ToList());

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Values.FirstOrDefault(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _users.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _users.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCourseRepository(params Course[] courses) : ICourseRepository
    {
        private readonly Dictionary<string, Course> _courses = courses.ToDictionary(course => course.Code, StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Course>>(_courses.Values.ToList());

        public Task<Course?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            _courses.TryGetValue(code, out var course);
            return Task.FromResult(course);
        }

        public Task AddAsync(Course course, CancellationToken cancellationToken = default)
        {
            _courses[course.Code] = course;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStudyPlanProvider(StudyPlan? plan) : IStudyPlanProvider
    {
        public Task<StudyPlan?> GetPlanForCourseAsync(Course course, CancellationToken cancellationToken = default) => Task.FromResult(plan);
    }
}