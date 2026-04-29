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

        await SeedCoursesAsync(cancellationToken);
        var users = await SeedUsersAsync(cancellationToken);
        var groups = await SeedGroupsAsync(users, cancellationToken);
        await SeedFeedAsync(users, groups);
        await SeedPersonalDataAsync(users);
    }

    private async Task SeedCoursesAsync(CancellationToken cancellationToken)
    {
        foreach (var seed in DemoCourses)
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

    private async Task<Dictionary<string, User>> SeedUsersAsync(CancellationToken cancellationToken)
    {
        var users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in DemoUsers)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Email == seed.Email, cancellationToken);
            var course = DemoCourses.FirstOrDefault(item => item.Code == seed.Course);
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

    private async Task<Dictionary<string, CampusGroup>> SeedGroupsAsync(IReadOnlyDictionary<string, User> users, CancellationToken cancellationToken)
    {
        var groups = new Dictionary<string, CampusGroup>(StringComparer.OrdinalIgnoreCase);
        var allUserIds = users.Values.Select(user => user.Id).ToHashSet();
        var studentUserIds = users.Values.Where(user => user.Role == UserRole.Student).Select(user => user.Id).ToHashSet();

        await AddGroupAsync(groups, "official-announcements", new CampusGroup
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "Offizielle Mitteilungen",
            Description = "Zentrale Hinweise von Verwaltung, Studiengangsleitung und Hochschulleitung.",
            Type = GroupType.Official,
            Audience = "Alle Studierenden",
            OwnerLabel = "DHBW Lörrach",
            IconLabel = "OF",
            AccentColor = "#a00014",
            AssignedUserIds = allUserIds,
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = true, IsDiscoverable = true }
        });

        await AddGroupAsync(groups, "exam-office", new CampusGroup
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Name = "Prüfungsamt und Fristen",
            Description = "Prüfungsordnungen, Anerkennungen, Abgaben, Fristen und organisatorische Hinweise.",
            Type = GroupType.Official,
            Audience = "Studienbereichsübergreifend",
            OwnerLabel = "Prüfungsamt",
            IconLabel = "PF",
            AccentColor = "#6b1f2a",
            AssignedUserIds = allUserIds,
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = false, RequiresApproval = true, IsDiscoverable = true }
        });

        await AddGroupAsync(groups, "mensa-campus", new CampusGroup
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            Name = "Mensa und Campus Hangstraße",
            Description = "Speiseplan, Campusservice und Hinweise rund um den Standort Hangstraße.",
            Type = GroupType.Official,
            Audience = "Campus Hangstraße",
            OwnerLabel = "Campusservice",
            IconLabel = "ME",
            AccentColor = "#047857",
            AssignedUserIds = allUserIds,
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
        });

        await AddGroupAsync(groups, "library-learning", new CampusGroup
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            Name = "Bibliothek und Recherche",
            Description = "Bibliotheksangebote, Datenbanken, E-Books und Recherchetipps für Studienarbeiten.",
            Type = GroupType.Official,
            Audience = "Alle Studiengänge",
            OwnerLabel = "Bibliothek",
            IconLabel = "BI",
            AccentColor = "#315f72",
            AssignedUserIds = allUserIds,
            Settings = new GroupSettings { AllowStudentPosts = false, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
        });

        await AddGroupAsync(groups, "stuv-events", new CampusGroup
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Name = "StuV, Events und Hochschulaktivitäten",
            Description = "Engagement, Hochschulsport, Freizeit und Veranstaltungen für das Campusleben in Lörrach.",
            Type = GroupType.Social,
            Audience = "Alle Studierenden",
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
            Name = "Wohnungssuche Lörrach",
            Description = "Austausch zu Zimmern, WGs, Pendeln und Wohnen in der Region Lörrach.",
            Type = GroupType.Social,
            Audience = "Studierende in und um Lörrach",
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
            Name = "Tech-Projekte und Labore",
            Description = "Projektideen, Laborzeiten, Tooling und Praxisfragen für technische Studiengänge.",
            Type = GroupType.Social,
            Audience = "Technik und Informatik",
            OwnerUserId = users["lecturer-tech"].Id,
            OwnerLabel = users["lecturer-tech"].DisplayName,
            IconLabel = "TP",
            AccentColor = "#0f766e",
            AssignedUserIds = users.Values.Where(user => user.Course.StartsWith('T') || user.Course.StartsWith("WWI", StringComparison.OrdinalIgnoreCase)).Select(user => user.Id).ToHashSet(),
            Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = false }
        });

        await AddGroupAsync(groups, "moodle-help", new CampusGroup
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000004"),
            Name = "Moodle, Webmail und Campus App Hilfe",
            Description = "Peer-Support für digitale Werkzeuge, Stundenplan, Mail und Lernplattform.",
            Type = GroupType.Social,
            Audience = "Alle Accounts",
            OwnerUserId = users["lecturer-business"].Id,
            OwnerLabel = users["lecturer-business"].DisplayName,
            IconLabel = "IT",
            AccentColor = "#475569",
            AssignedUserIds = allUserIds,
            Settings = new GroupSettings { AllowStudentPosts = true, AllowComments = true, RequiresApproval = false, IsDiscoverable = true }
        });

        foreach (var course in DemoCourses)
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
        var posts = new[]
        {
            new FeedPost
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                AuthorId = users["admin"].Id,
                AuthorName = users["admin"].DisplayName,
                GroupId = groups["official-announcements"].Id,
                Content = "Willkommen im CampusConnect-Demobereich. Hier laufen offizielle Hinweise, Kursgruppen und Campusgruppen an einem Ort zusammen.",
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
                Content = "Reminder: Prüft eure persönlichen Prüfungstermine und hinterlegt Abgaben mit ausreichend Vorlauf im Kalender.",
                CreatedAt = SeedNow.AddHours(-5)
            },
            new FeedPost
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000003"),
                AuthorId = users["student-tif"].Id,
                AuthorName = users["student-tif"].DisplayName,
                GroupId = groups["stuv-events"].Id,
                Content = "Heute Abend treffen wir uns zur StuV-Planung. Themen: Hochschulsport, Erstsemesterfragen und Eventkalender.",
                CreatedAt = SeedNow.AddHours(-4)
            },
            new FeedPost
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                AuthorId = users["lecturer-tech"].Id,
                AuthorName = users["lecturer-tech"].DisplayName,
                GroupId = groups["tech-projects"].Id,
                Content = "Für die nächsten Projektarbeiten sind Laborplätze reserviert. Bitte stimmt euch kursübergreifend in der Gruppe ab.",
                CreatedAt = SeedNow.AddHours(-3)
            },
            new FeedPost
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000005"),
                AuthorId = users["student-wwi"].Id,
                AuthorName = users["student-wwi"].DisplayName,
                GroupId = groups["WWI25A"].Id,
                Content = "WWI25A: Lerngruppe für Datenbanken am Donnerstag nach der Vorlesung?",
                CreatedAt = SeedNow.AddHours(-2),
                Comments =
                [
                    new FeedComment
                    {
                        Id = Guid.Parse("41000000-0000-0000-0000-000000000002"),
                        AuthorId = users["student-wdb"].Id,
                        AuthorName = users["student-wdb"].DisplayName,
                        Content = "Ich kann nach 16 Uhr dazukommen.",
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
                Content = "Falls jemand ab Juni ein WG-Zimmer in Campusnähe sucht: In unserer WG wird ein Platz frei.",
                CreatedAt = SeedNow.AddHours(-1)
            }
        };

        foreach (var post in posts)
            await feedRepository.AddAsync(post);
    }

    private async Task SeedPersonalDataAsync(IReadOnlyDictionary<string, User> users)
    {
        await SeedGradesAsync(users["student-tif"].Id, "Programmieren I", "Mathematik I");
        await SeedGradesAsync(users["student-wwi"].Id, "Datenbanken", "BWL Grundlagen");
        await SeedGradesAsync(users["student-wdb"].Id, "Digital Business Models", "Projektmanagement");

        foreach (var user in users.Values.Where(user => user.Role == UserRole.Student))
        {
            await examRepository.AddAsync(new ExamEntry
            {
                Id = StableGuid("exam", user.Id, 1),
                UserId = user.Id,
                ModuleName = "Klausurphase Modulprüfung",
                ExamDate = new DateTime(2026, 6, 18, 9, 0, 0, DateTimeKind.Utc),
                Location = "Campus Hangstraße",
                Notes = "Demo-Termin für Kalender- und Erinnerungsfunktionen.",
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

    private static readonly DemoCourse[] DemoCourses =
    [
        new("TIF25A", "Informatik", 2),
        new("TIF24A", "Informatik", 4),
        new("WWI25A", "Wirtschaftsinformatik", 2),
        new("TMB25A", "Maschinenbau", 2),
        new("TMT25A", "Mechatronik Trinational", 2),
        new("TEI25A", "Elektrotechnik und Informationstechnik", 2),
        new("TWI25A", "Wirtschaftsingenieurwesen", 2),
        new("TAR25A", "Architektur", 2),
        new("WDB25A", "BWL-Digital Business Management", 2),
        new("WFD25A", "BWL-Finanzdienstleistungen", 2),
        new("WGM24A", "BWL-Gesundheitsmanagement", 4),
        new("WHM25A", "BWL-Handelsmanagement", 2),
        new("WIN25A", "BWL-Industrie (Industrial Management)", 2),
        new("WIB25A", "BWL-International Business", 2),
        new("WST25A", "BWL-Spedition, Transport und Logistik", 2),
        new("WTH25A", "BWL-Tourismus, Hotellerie und Gastronomie", 2),
        new("WDS25A", "Data Science und Künstliche Intelligenz - Business Management", 2),
        new("IBM25A", "International Business Management Trinational", 2),
        new("GIG25A", "Interprofessionelle Gesundheitsversorgung", 2)
    ];

    private static readonly DemoUser[] DemoUsers =
    [
        new("admin", Guid.Parse("50000000-0000-0000-0000-000000000001"), "demo.admin@dhbw-loerrach.de", "Demo Administration", "TIF25A", "Campusverwaltung", 2, UserRole.Admin),
        new("lecturer-tech", Guid.Parse("50000000-0000-0000-0000-000000000002"), "demo.technik@dhbw-loerrach.de", "Prof. Technik Demo", "TIF25A", "Informatik", 2, UserRole.Lecturer),
        new("lecturer-business", Guid.Parse("50000000-0000-0000-0000-000000000003"), "demo.wirtschaft@dhbw-loerrach.de", "Prof. Wirtschaft Demo", "WDB25A", "BWL-Digital Business Management", 2, UserRole.Lecturer),
        new("student-tif", Guid.Parse("50000000-0000-0000-0000-000000000011"), "lena.tif25a@dhbw-loerrach.de", "Lena Informatik", "TIF25A", "Informatik", 2, UserRole.Student),
        new("student-wwi", Guid.Parse("50000000-0000-0000-0000-000000000012"), "noah.wwi25a@dhbw-loerrach.de", "Noah Wirtschaftsinformatik", "WWI25A", "Wirtschaftsinformatik", 2, UserRole.Student),
        new("student-wdb", Guid.Parse("50000000-0000-0000-0000-000000000013"), "mia.wdb25a@dhbw-loerrach.de", "Mia Digital Business", "WDB25A", "BWL-Digital Business Management", 2, UserRole.Student),
        new("student-tmb", Guid.Parse("50000000-0000-0000-0000-000000000014"), "jonas.tmb25a@dhbw-loerrach.de", "Jonas Maschinenbau", "TMB25A", "Maschinenbau", 2, UserRole.Student),
        new("student-wgm", Guid.Parse("50000000-0000-0000-0000-000000000015"), "sara.wgm24a@dhbw-loerrach.de", "Sara Gesundheitsmanagement", "WGM24A", "BWL-Gesundheitsmanagement", 4, UserRole.Student),
        new("student-gig", Guid.Parse("50000000-0000-0000-0000-000000000016"), "emil.gig25a@dhbw-loerrach.de", "Emil Gesundheitsversorgung", "GIG25A", "Interprofessionelle Gesundheitsversorgung", 2, UserRole.Student)
    ];

    private sealed record DemoCourse(string Code, string StudyProgram, int Semester);

    private sealed record DemoUser(string Key, Guid Id, string Email, string DisplayName, string Course, string StudyProgram, int Semester, UserRole Role);
}