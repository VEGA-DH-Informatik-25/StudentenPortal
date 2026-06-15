using CampusConnect.Application.Common.Security;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CampusConnect.Infrastructure.Persistence;

public sealed class DevelopmentDemoDataSeeder(
    CampusConnectDbContext dbContext,
    IOptions<DemoDataOptions> options,
    IGroupRepository groupRepository,
    IFeedRepository feedRepository,
    IGradeRepository gradeRepository,
    IExamRepository examRepository)
{
    private static readonly DateTime SeedNow = new(2026, 4, 28, 8, 0, 0, DateTimeKind.Utc);

    private readonly DemoDataOptions _options = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        var demoCourses = ResolveDemoCourses();
        await SeedCoursesAsync(demoCourses, cancellationToken);
        var users = await SeedUsersAsync(demoCourses, cancellationToken);
        var groups = await SeedGroupsAsync(demoCourses, users, cancellationToken);
        await SeedFeedAsync(users, groups);
        await SeedPersonalDataAsync(users);
    }

    private async Task SeedCoursesAsync(IReadOnlyList<DemoCourse> demoCourses, CancellationToken cancellationToken)
    {
        foreach (var seed in demoCourses)
        {
            var course = await dbContext.Courses.FirstOrDefaultAsync(item => item.Code == seed.Code, cancellationToken);
            if (course is null)
            {
                dbContext.Courses.Add(new Course
                {
                    Code = seed.Code,
                    StudyProgram = seed.StudyProgram,
                    Semester = seed.Semester,
                    IsActive = true,
                    CreatedAt = SeedNow.AddDays(-20)
                });
                continue;
            }

            course.StudyProgram = seed.StudyProgram;
            course.Semester = seed.Semester;
            course.IsActive = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, User>> SeedUsersAsync(IReadOnlyList<DemoCourse> demoCourses, CancellationToken cancellationToken)
    {
        var users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in DemoUsers)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Email == seed.Email, cancellationToken);
            var course = demoCourses.FirstOrDefault(item => item.Code == seed.Course);
            var studyProgram = course?.StudyProgram ?? seed.StudyProgram;
            var semester = course?.Semester ?? seed.Semester;

            if (user is null)
            {
                user = new User
                {
                    Id = seed.Id,
                    Email = seed.Email,
                    PasswordHash = PasswordHasher.Hash(_options.Password),
                    DisplayName = seed.DisplayName,
                    StudyProgram = studyProgram,
                    Semester = semester,
                    Course = seed.Course,
                    Role = seed.Role,
                    CreatedAt = SeedNow.AddDays(-18)
                };
                dbContext.Users.Add(user);
            }
            else
            {
                user.DisplayName = seed.DisplayName;
                user.StudyProgram = studyProgram;
                user.Semester = semester;
                user.Course = seed.Course;
                user.Role = seed.Role;
            }

            users[seed.Key] = user;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return users;
    }

    private async Task<Dictionary<string, CampusGroup>> SeedGroupsAsync(IReadOnlyList<DemoCourse> demoCourses, IReadOnlyDictionary<string, User> users, CancellationToken cancellationToken)
    {
        var groups = new Dictionary<string, CampusGroup>(StringComparer.OrdinalIgnoreCase);
        var allUserIds = users.Values.Select(user => user.Id).ToHashSet();
        var studentUserIds = users.Values.Where(user => user.Role == UserRole.Student).Select(user => user.Id).ToHashSet();

        await AddGroupAsync(groups, "official-announcements", new CampusGroup
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "Official announcements",
            Description = "Central notices from management, program leadership, and university leadership.",
            Type = GroupType.Official,
            Audience = "All students",
            OwnerLabel = "DHBW Loerrach",
            IconLabel = "OF",
            AccentColor = "#a00014",
            AssignedUserIds = allUserIds,
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = true, IsDiscoverable = true }
        });

        await AddGroupAsync(groups, "exam-office", new CampusGroup
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Name = "Exam office and deadlines",
            Description = "Exam regulations, recognition requests, submissions, deadlines, and organizational notices.",
            Type = GroupType.Official,
            Audience = "Across study programs",
            OwnerLabel = "Exam office",
            IconLabel = "PF",
            AccentColor = "#6b1f2a",
            AssignedUserIds = allUserIds,
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = true, IsDiscoverable = true }
        });

        await AddGroupAsync(groups, "mensa-campus", new CampusGroup
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            Name = "Mensa and Hangstrasse campus",
            Description = "Menu, campus service, and notices for the Hangstrasse site.",
            Type = GroupType.Official,
            Audience = "Hangstrasse campus",
            OwnerLabel = "Campusservice",
            IconLabel = "ME",
            AccentColor = "#047857",
            AssignedUserIds = allUserIds,
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
        });

        await AddGroupAsync(groups, "library-learning", new CampusGroup
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            Name = "Library and research",
            Description = "Library services, databases, e-books, and research tips for academic work.",
            Type = GroupType.Official,
            Audience = "All study programs",
            OwnerLabel = "Library",
            IconLabel = "BI",
            AccentColor = "#315f72",
            AssignedUserIds = allUserIds,
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
        });

        await AddGroupAsync(groups, "stuv-events", new CampusGroup
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Name = "StuV, events, and university activities",
            Description = "Engagement, university sports, leisure, and events for campus life in Loerrach.",
            Type = GroupType.Campus,
            Audience = "All students",
            OwnerUserId = users["student-tif"].Id,
            OwnerLabel = users["student-tif"].DisplayName,
            IconLabel = "SV",
            AccentColor = "#2563eb",
            AssignedUserIds = studentUserIds,
            Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
        });

        await AddGroupAsync(groups, "housing", new CampusGroup
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
            Name = "Housing in Loerrach",
            Description = "Exchange about rooms, shared flats, commuting, and living in the Loerrach region.",
            Type = GroupType.Campus,
            Audience = "Students in and around Loerrach",
            OwnerUserId = users["student-wwi"].Id,
            OwnerLabel = users["student-wwi"].DisplayName,
            IconLabel = "WG",
            AccentColor = "#7c3aed",
            AssignedUserIds = [users["student-wwi"].Id],
            Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
        });

        await AddGroupAsync(groups, "tech-projects", new CampusGroup
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            Name = "Tech projects and labs",
            Description = "Project ideas, lab hours, tooling, and practical questions for technical study programs.",
            Type = GroupType.Campus,
            Audience = "Technology and computer science",
            OwnerUserId = users["lecturer-tech"].Id,
            OwnerLabel = users["lecturer-tech"].DisplayName,
            IconLabel = "TP",
            AccentColor = "#0f766e",
            AssignedUserIds = users.Values.Where(IsTechnicalDemoCourse).Select(user => user.Id).ToHashSet(),
            Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = false }
        });

        await AddGroupAsync(groups, "moodle-help", new CampusGroup
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000004"),
            Name = "Moodle, webmail, and campus app help",
            Description = "Peer support for digital tools, timetable, email, and learning platforms.",
            Type = GroupType.Campus,
            Audience = "All accounts",
            OwnerUserId = users["lecturer-business"].Id,
            OwnerLabel = users["lecturer-business"].DisplayName,
            IconLabel = "IT",
            AccentColor = "#475569",
            AssignedUserIds = allUserIds,
            Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
        });

        foreach (var course in demoCourses.Where(course => course.Semester.HasValue))
        {
            var group = await groupRepository.EnsureCourseGroupAsync(course.Code, course.StudyProgram);
            var assignedIds = users.Values
                .Where(user => string.Equals(user.Course, course.Code, StringComparison.OrdinalIgnoreCase))
                .Select(user => user.Id)
                .ToList();

            await groupRepository.SyncCourseAssignmentsAsync(course.Code, assignedIds);
            groups[course.Code] = group;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return groups;
    }

    private async Task AddGroupAsync(IDictionary<string, CampusGroup> groups, string key, CampusGroup group)
    {
        await groupRepository.AddAsync(group);
        groups[key] = group;
    }

    private async Task SeedFeedAsync(IReadOnlyDictionary<string, User> users, IReadOnlyDictionary<string, CampusGroup> groups)
    {
        var studentWwiGroup = DemoCourseGroupFor("student-wwi", users, groups);
        var posts = new[]
        {
            new FeedPost
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                AuthorId = users["admin"].Id,
                AuthorName = users["admin"].DisplayName,
                GroupId = groups["official-announcements"].Id,
                Content = "Welcome to the CampusConnect demo area. Official notices, course groups, and campus groups come together here.",
                CreatedAt = SeedNow.AddHours(-6),
                Reactions =
                [
                    new FeedReaction { Emoji = "👍", UserIds = [users["student-tif"].Id, users["student-wwi"].Id, users["student-wdb"].Id] },
                    new FeedReaction { Emoji = "💡", UserIds = [users["lecturer-tech"].Id] }
                ]
            },
            new FeedPost
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000002"),
                AuthorId = users["admin"].Id,
                AuthorName = users["admin"].DisplayName,
                GroupId = groups["exam-office"].Id,
                Content = "Reminder: Check your personal exam dates and add submissions to the calendar early enough.",
                CreatedAt = SeedNow.AddHours(-5)
            },
            new FeedPost
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000003"),
                AuthorId = users["student-tif"].Id,
                AuthorName = users["student-tif"].DisplayName,
                GroupId = groups["stuv-events"].Id,
                Content = "Tonight we meet for StuV planning. Topics: university sports, first-year questions, and the event calendar.",
                CreatedAt = SeedNow.AddHours(-4)
            },
            new FeedPost
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                AuthorId = users["lecturer-tech"].Id,
                AuthorName = users["lecturer-tech"].DisplayName,
                GroupId = groups["tech-projects"].Id,
                Content = "Lab spaces are reserved for the next project assignments. Please coordinate across courses in the group.",
                CreatedAt = SeedNow.AddHours(-3)
            },
            new FeedPost
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000005"),
                AuthorId = users["student-wwi"].Id,
                AuthorName = users["student-wwi"].DisplayName,
                GroupId = studentWwiGroup.Id,
                Content = $"{users["student-wwi"].Course}: Study group for databases after lecture on Thursday?",
                CreatedAt = SeedNow.AddHours(-2),
                Comments =
                [
                    new FeedComment
                    {
                        Id = Guid.Parse("41000000-0000-0000-0000-000000000002"),
                        AuthorId = users["student-wdb"].Id,
                        AuthorName = users["student-wdb"].DisplayName,
                        Content = "I can join after 4 p.m.",
                        CreatedAt = SeedNow.AddHours(-1).AddMinutes(-35)
                    }
                ],
                Reactions =
                [
                    new FeedReaction { Emoji = "🎉", UserIds = [users["student-tif"].Id, users["student-wdb"].Id] }
                ]
            },
            new FeedPost
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000006"),
                AuthorId = users["student-wwi"].Id,
                AuthorName = users["student-wwi"].DisplayName,
                GroupId = groups["housing"].Id,
                Content = "If anyone is looking for a room near campus from June: one spot in our shared flat is opening up.",
                CreatedAt = SeedNow.AddHours(-1)
            }
        };

        foreach (var post in posts)
            await feedRepository.AddAsync(post);
    }

    private async Task SeedPersonalDataAsync(IReadOnlyDictionary<string, User> users)
    {
        await SeedGradesAsync(users["student-tif"].Id, "Programming I", "Mathematics I");
        await SeedGradesAsync(users["student-wwi"].Id, "Databases", "Business administration basics");
        await SeedGradesAsync(users["student-wdb"].Id, "Digital Business Models", "Project management");

        foreach (var user in users.Values.Where(user => user.Role == UserRole.Student))
        {
            await examRepository.AddAsync(new ExamEntry
            {
                Id = StableGuid("exam", user.Id, 1),
                UserId = user.Id,
                ModuleName = "Exam phase module assessment",
                ExamDate = new DateTime(2026, 6, 18, 9, 0, 0, DateTimeKind.Utc),
                Location = "Hangstrasse campus",
                Notes = "Demo date for calendar and reminder features.",
                CreatedAt = SeedNow.AddDays(-2)
            });
        }
    }

    private async Task SeedGradesAsync(Guid userId, string firstModule, string secondModule)
    {
        await gradeRepository.AddAsync(new Grade
        {
            Id = StableGuid("grade", userId, 1),
            UserId = userId,
            ModuleName = firstModule,
            Value = 1.7m,
            Ects = 5,
            CreatedAt = SeedNow.AddDays(-12)
        });

        await gradeRepository.AddAsync(new Grade
        {
            Id = StableGuid("grade", userId, 2),
            UserId = userId,
            ModuleName = secondModule,
            Value = 2.3m,
            Ects = 5,
            CreatedAt = SeedNow.AddDays(-8)
        });
    }

    private static Guid StableGuid(string area, Guid userId, int index)
    {
        var source = System.Text.Encoding.UTF8.GetBytes($"{area}:{userId:N}:{index}");
        Span<byte> hash = stackalloc byte[16];
        System.Security.Cryptography.MD5.HashData(source, hash);
        return new Guid(hash);
    }

    private static CampusGroup DemoCourseGroupFor(string userKey, IReadOnlyDictionary<string, User> users, IReadOnlyDictionary<string, CampusGroup> groups) =>
        groups.TryGetValue(users[userKey].Course, out var courseGroup) ? courseGroup : groups["stuv-events"];

    private IReadOnlyList<DemoCourse> ResolveDemoCourses() => _options.Courses
        .Where(course => !string.IsNullOrWhiteSpace(course.Code) && !string.IsNullOrWhiteSpace(course.StudyProgram))
        .Select(course => new DemoCourse(course.Code.Trim().ToUpperInvariant(), course.StudyProgram.Trim(), NormalizeSemester(course.Semester)))
        .Concat(SystemRoleCourses)
        .GroupBy(course => course.Code, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First())
        .ToList();

    private static int? NormalizeSemester(int? semester) => semester is null ? null : Math.Clamp(semester.Value, 1, 6);

    private bool IsTechnicalDemoCourse(User user) => _options.TechnicalCoursePrefixes
        .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
        .Select(prefix => prefix.Trim())
        .Any(prefix => user.Course.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static readonly DemoUser[] DemoUsers =
    [
        new("admin", Guid.Parse("50000000-0000-0000-0000-000000000001"), "demo.admin@dhbw-loerrach.de", "Demo Administration", "ADMIN", "Administration", null, UserRole.Admin),
        new("lecturer-tech", Guid.Parse("50000000-0000-0000-0000-000000000002"), "demo.technik@dhbw-loerrach.de", "Prof. Technology Demo", "LECTURER", "Lehrende", null, UserRole.Lecturer),
        new("lecturer-business", Guid.Parse("50000000-0000-0000-0000-000000000003"), "demo.wirtschaft@dhbw-loerrach.de", "Prof. Business Demo", "LECTURER", "Lehrende", null, UserRole.Lecturer),
        new("student-tif", Guid.Parse("50000000-0000-0000-0000-000000000011"), "lena.tif25a@dhbw-loerrach.de", "Lena Computer Science", "TIF25A", "Computer Science", 2, UserRole.Student),
        new("student-wwi", Guid.Parse("50000000-0000-0000-0000-000000000012"), "noah.wwi25a@dhbw-loerrach.de", "Noah Business Informatics", "WWI25A", "Business Informatics", 2, UserRole.Student),
        new("student-wdb", Guid.Parse("50000000-0000-0000-0000-000000000013"), "mia.wdb25a@dhbw-loerrach.de", "Mia Digital Business", "WDB25A", "Business Administration - Digital Business Management", 2, UserRole.Student),
        new("student-tmb", Guid.Parse("50000000-0000-0000-0000-000000000014"), "jonas.tmb25a@dhbw-loerrach.de", "Jonas Mechanical Engineering", "TMB25A", "Mechanical Engineering", 2, UserRole.Student),
        new("student-wgm", Guid.Parse("50000000-0000-0000-0000-000000000015"), "sara.wgm24a@dhbw-loerrach.de", "Sara Health Management", "WGM24A", "Business Health Management", 4, UserRole.Student),
        new("student-gig", Guid.Parse("50000000-0000-0000-0000-000000000016"), "emil.gig25a@dhbw-loerrach.de", "Emil Health Care", "GIG25A", "Interprofessional Health Care", 2, UserRole.Student)
    ];

    private static readonly DemoCourse[] SystemRoleCourses =
    [
        new("ADMIN", "Administration", null),
        new("LECTURER", "Lehrende", null),
        new("MANAGEMENT", "Verwaltung", null)
    ];

    private sealed record DemoCourse(string Code, string StudyProgram, int? Semester);

    private sealed record DemoUser(string Key, Guid Id, string Email, string DisplayName, string Course, string StudyProgram, int? Semester, UserRole Role);
}
