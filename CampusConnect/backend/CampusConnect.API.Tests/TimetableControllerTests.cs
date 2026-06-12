using System.Security.Claims;
using CampusConnect.API.Controllers;
using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Features.Auth;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Tests;

public sealed class TimetableControllerTests
{
    [Fact]
    public async Task GetTimetable_WithoutCourseQuery_UsesCurrentUsersCourse()
    {
        var user = new User
        {
            Email = "student@dhbw-loerrach.de",
            DisplayName = "Student",
            StudyProgram = "Computer Science",
            Semester = 2,
            Course = "TIF25A",
            Role = UserRole.Student
        };
        var course = new Course { Code = "TIF25A", StudyProgram = "Computer Science", Semester = 2, IsActive = true };
        var timetableService = new CapturingTimetableService();
        var authService = new AuthService(
            new FakeUserRepository(user),
            new FakeJwtService(),
            new FakeCourseRepository(course),
            new FakeGroupRepository());
        var controller = new TimetableController(timetableService, authService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                    ]))
                }
            }
        };

        var response = await controller.GetTimetable();

        Assert.IsType<OkObjectResult>(response);
        Assert.Equal("TIF25A", timetableService.RequestedCourse);
    }

    [Fact]
    public async Task GetTimetable_ForwardsExplicitRangeStart()
    {
        var timetableService = new CapturingTimetableService();
        var authService = new AuthService(
            new FakeUserRepository(),
            new FakeJwtService(),
            new FakeCourseRepository(),
            new FakeGroupRepository());
        var controller = new TimetableController(timetableService, authService);

        var response = await controller.GetTimetable("TIF25A", 6, new DateOnly(2026, 5, 4));

        Assert.IsType<OkObjectResult>(response);
        Assert.Equal("TIF25A", timetableService.RequestedCourse);
        Assert.Equal(6, timetableService.RequestedDays);
        Assert.Equal(new DateOnly(2026, 5, 4), timetableService.RequestedFrom);
    }

    private sealed class CapturingTimetableService : ITimetableService
    {
        public string? RequestedCourse { get; private set; }
        public int? RequestedDays { get; private set; }
        public DateOnly? RequestedFrom { get; private set; }

        public Task<TimetableDto> GetTimetableAsync(string course, int days, DateOnly? from = null, CancellationToken cancellationToken = default)
        {
            RequestedCourse = course;
            RequestedDays = days;
            RequestedFrom = from;
            return Task.FromResult(new TimetableDto(course, "Europe/Berlin", []));
        }
    }

    private sealed class FakeJwtService : IJwtService
    {
        public string GenerateToken(User user) => "test-token";
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        private readonly Dictionary<Guid, User> _usersById = users.ToDictionary(user => user.Id);
        private readonly Dictionary<string, User> _usersByEmail = users.ToDictionary(user => user.Email, StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(_usersById.Values.ToList());

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            _usersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _usersById.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _usersById[user.Id] = user;
            _usersByEmail[user.Email] = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _usersById[user.Id] = user;
            _usersByEmail[user.Email] = user;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_usersById.Remove(id, out var user))
                _usersByEmail.Remove(user.Email);

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

    private sealed class FakeGroupRepository : IGroupRepository
    {
        public Task<IReadOnlyList<CampusGroup>> GetAllAsync() => Task.FromResult<IReadOnlyList<CampusGroup>>([]);

        public Task<CampusGroup?> FindByIdAsync(Guid id) => Task.FromResult<CampusGroup?>(null);

        public Task<CampusGroup> EnsureCourseGroupAsync(string courseCode, string? studyProgram = null) =>
            Task.FromResult(new CampusGroup { CourseCode = courseCode, Name = $"Course {courseCode}" });

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
