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
                new Course { Code = "TIF25A", StudyProgram = "Computer Science", IsActive = true },
                new Course { Code = "OLD", StudyProgram = "Archive", IsActive = false }),
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

        var result = await service.CreateCourseAsync(new CreateCourseCommand(" tif25a ", "Computer Science"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(await courses.FindByCodeAsync("TIF25A"));
        Assert.Equal("TIF25A", groups.CreatedCourseCode);
        Assert.Equal("Computer Science", groups.CreatedStudyProgram);
    }

    [Fact]
    public async Task GetCoursesAsync_ShouldExcludeSystemCoursesFromPublicList()
    {
        var service = new CoursesService(
            new FakeCourseRepository(
                new Course { Code = "TIF25A", StudyProgram = "Computer Science" },
                new Course { Code = "ADMIN", StudyProgram = "Administration" }),
            new FakeGroupRepository());

        var courses = await service.GetCoursesAsync(includeSystemCourses: false);

        var course = Assert.Single(courses);
        Assert.Equal("TIF25A", course.Code);
    }

    [Fact]
    public async Task CreateCourseAsync_ShouldRejectDuplicateCourseCodes()
    {
        var service = new CoursesService(
            new FakeCourseRepository(new Course { Code = "TIF25A", StudyProgram = "Computer Science" }),
            new FakeGroupRepository());

        var result = await service.CreateCourseAsync(new CreateCourseCommand("tif25a", "Computer Science"));

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
            return Task.FromResult(new CampusGroup { CourseCode = courseCode, Name = $"Course {courseCode}" });
        }

        public Task AddAsync(CampusGroup group) => Task.CompletedTask;

        public Task UpdateSettingsAsync(Guid id, GroupSettings settings) => Task.CompletedTask;

        public Task AddMembersAsync(Guid id, IReadOnlyCollection<Guid> userIds) => Task.CompletedTask;

        public Task RemoveMemberAsync(Guid id, Guid userId) => Task.CompletedTask;

        public Task SetMemberRoleAsync(Guid id, Guid userId, GroupRole role) => Task.CompletedTask;

        public Task AddJoinRequestAsync(Guid id, Guid userId) => Task.CompletedTask;

        public Task RemoveJoinRequestAsync(Guid id, Guid userId) => Task.CompletedTask;

        public Task AddInvitationsAsync(Guid id, IReadOnlyCollection<Guid> userIds) => Task.CompletedTask;

        public Task RemoveInvitationAsync(Guid id, Guid userId) => Task.CompletedTask;

        public Task SyncCourseAssignmentsAsync(string courseCode, IReadOnlyCollection<Guid> assignedUserIds) => Task.CompletedTask;
    }
}
