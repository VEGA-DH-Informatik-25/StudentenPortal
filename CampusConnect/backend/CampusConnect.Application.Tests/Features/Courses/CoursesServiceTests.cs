using CampusConnect.Application.Features.Courses;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Courses;

public sealed class CoursesServiceTests
{
    [Fact]
    public async Task GetCoursesAsync_ShouldReturnOnlyActiveCourses()
    {
        var service = new CoursesService(
            new FakeCourseRepository(
                new Course { Code = "TIF25A", StudyProgram = "Informatik", Semester = 1, IsActive = true },
                new Course { Code = "OLD", StudyProgram = "Archiv", Semester = 1, IsActive = false }),
            new FakeGroupRepository());

        var courses = await service.GetCoursesAsync();

        var course = Assert.Single(courses);
        Assert.Equal("TIF25A", course.Code);
    }

    [Fact]
    public async Task CreateCourseAsync_ShouldNormalizeCodeAndCreateCourseGroup()
    {
        var courses = new FakeCourseRepository();
        var groups = new FakeGroupRepository();
        var service = new CoursesService(courses, groups);

        var result = await service.CreateCourseAsync(new CreateCourseCommand(" tif25a ", "Informatik", 1));

        Assert.True(result.IsSuccess);
        Assert.NotNull(await courses.FindByCodeAsync("TIF25A"));
        Assert.Equal("TIF25A", groups.CreatedCourseCode);
        Assert.Equal("Informatik", groups.CreatedStudyProgram);
    }

    [Fact]
    public async Task CreateCourseAsync_ShouldRejectDuplicateCourseCodes()
    {
        var service = new CoursesService(
            new FakeCourseRepository(new Course { Code = "TIF25A", StudyProgram = "Informatik", Semester = 1 }),
            new FakeGroupRepository());

        var result = await service.CreateCourseAsync(new CreateCourseCommand("tif25a", "Informatik", 1));

        Assert.False(result.IsSuccess);
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

    private sealed class FakeGroupRepository : IGroupRepository
    {
        public string? CreatedCourseCode { get; private set; }
        public string? CreatedStudyProgram { get; private set; }

        public Task<IReadOnlyList<CampusGroup>> GetAllAsync() => Task.FromResult<IReadOnlyList<CampusGroup>>([]);

        public Task<CampusGroup?> FindByIdAsync(Guid id) => Task.FromResult<CampusGroup?>(null);

        public Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null)
        {
            CreatedCourseCode = courseCode;
            CreatedStudyProgram = studyProgram;
            return Task.FromResult(new CampusGroup { CourseCode = courseCode, Name = $"Kurs {courseCode}" });
        }

        public Task AddAsync(CampusGroup group) => Task.CompletedTask;

        public Task UpdateSettingsAsync(Guid id, GroupSettings settings) => Task.CompletedTask;

        public Task UpdateAssignmentsAsync(Guid id, IReadOnlyCollection<Guid> assignedUserIds) => Task.CompletedTask;

        public Task UpdateMemberPermissionsAsync(Guid id, IReadOnlyDictionary<Guid, GroupMemberPermission> permissions) => Task.CompletedTask;

        public Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds) => Task.CompletedTask;
    }
}