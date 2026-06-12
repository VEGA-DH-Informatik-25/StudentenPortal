using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace CampusConnect.Infrastructure.Persistence;

public sealed class CampusConnectDbContext(DbContextOptions<CampusConnectDbContext> options) : DbContext(options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CampusGroup> CampusGroups => Set<CampusGroup>();
    public DbSet<FeedPost> FeedPosts => Set<FeedPost>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<ExamEntry> ExamEntries => Set<ExamEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.ToTable("Users");
        user.HasKey(entity => entity.Id);
        user.HasIndex(entity => entity.Email).IsUnique();
        user.Property(entity => entity.Email).HasMaxLength(256).IsRequired();
        user.Property(entity => entity.PasswordHash).HasMaxLength(256).IsRequired();
        user.Property(entity => entity.DisplayName).HasMaxLength(120).IsRequired();
        user.Property(entity => entity.StudyProgram).HasMaxLength(120).IsRequired();
        user.Property(entity => entity.Course).HasMaxLength(40).IsRequired();
        user.Property(entity => entity.PhoneNumber).HasMaxLength(40).IsRequired();
        user.Property(entity => entity.Location).HasMaxLength(120).IsRequired();
        user.Property(entity => entity.ProfileNote).HasMaxLength(280).IsRequired();
        user.Property(entity => entity.Role)
            .HasConversion(role => role.ToString(), value => Enum.Parse<UserRole>(value))
            .HasMaxLength(32)
            .IsRequired();
        user.Property(entity => entity.CreatedAt).IsRequired();

        var course = modelBuilder.Entity<Course>();
        course.ToTable("Courses");
        course.HasKey(entity => entity.Code);
        course.Property(entity => entity.Code).HasMaxLength(40).IsRequired();
        course.Property(entity => entity.StudyProgram).HasMaxLength(120).IsRequired();
        course.Property(entity => entity.Semester).IsRequired();
        course.Property(entity => entity.IsActive).IsRequired();
        course.Property(entity => entity.CreatedAt).IsRequired();

        var group = modelBuilder.Entity<CampusGroup>();
        group.ToTable("CampusGroups");
        group.HasKey(entity => entity.Id);
        group.HasIndex(entity => entity.CourseCode);
        group.Property(entity => entity.Name).HasMaxLength(80).IsRequired();
        group.Property(entity => entity.Description).HasMaxLength(240).IsRequired();
        group.Property(entity => entity.Type)
            .HasConversion(type => type.ToString(), value => Enum.Parse<GroupType>(value))
            .HasMaxLength(32)
            .IsRequired();
        group.Property(entity => entity.Audience).HasMaxLength(80).IsRequired();
        group.Property(entity => entity.CourseCode).HasMaxLength(40);
        group.Property(entity => entity.OfficialCategory).HasMaxLength(80);
        group.Property(entity => entity.OwnerLabel).HasMaxLength(120).IsRequired();
        group.Property(entity => entity.IconLabel).HasMaxLength(8).IsRequired();
        group.Property(entity => entity.AccentColor).HasMaxLength(16).IsRequired();
        group.Property(entity => entity.Settings)
            .HasConversion(
                value => Serialize(value),
                value => Deserialize(value, () => new GroupSettings()))
            .Metadata.SetValueComparer(JsonComparer<GroupSettings>());
        group.Property(entity => entity.AssignedUserIds)
            .HasConversion(
                value => Serialize(value),
                value => Deserialize(value, () => new HashSet<Guid>()))
            .Metadata.SetValueComparer(JsonComparer<HashSet<Guid>>());
        group.Property(entity => entity.MemberRoles)
            .HasColumnName("MemberPermissions")
            .HasConversion(
                value => Serialize(value),
                value => Deserialize(value, () => new Dictionary<Guid, GroupRole>()))
            .Metadata.SetValueComparer(JsonComparer<Dictionary<Guid, GroupRole>>());

        group.Property(entity => entity.PendingJoinRequests)
            .HasConversion(
                value => Serialize(value),
                value => Deserialize(value, () => new HashSet<Guid>()))
            .Metadata.SetValueComparer(JsonComparer<HashSet<Guid>>());
        group.Property(entity => entity.Invitations)
            .HasConversion(
                value => Serialize(value),
                value => Deserialize(value, () => new HashSet<Guid>()))
            .Metadata.SetValueComparer(JsonComparer<HashSet<Guid>>());

        var feedPost = modelBuilder.Entity<FeedPost>();
        feedPost.ToTable("FeedPosts");
        feedPost.HasKey(entity => entity.Id);
        feedPost.HasIndex(entity => entity.CreatedAt);
        feedPost.HasIndex(entity => entity.GroupId);
        feedPost.Property(entity => entity.AuthorName).HasMaxLength(120).IsRequired();
        feedPost.Property(entity => entity.Content).HasMaxLength(4000).IsRequired();
        feedPost.Property(entity => entity.Status)
            .HasConversion(status => status.ToString(), value => Enum.Parse<FeedPostStatus>(value))
            .HasMaxLength(16)
            .IsRequired();
        feedPost.Property(entity => entity.AllowComments).IsRequired();
        feedPost.Property(entity => entity.CreatedAt).IsRequired();
        feedPost.Property(entity => entity.Comments)
            .HasConversion(
                value => Serialize(value),
                value => Deserialize(value, () => new List<FeedComment>()))
            .Metadata.SetValueComparer(JsonComparer<List<FeedComment>>());
        feedPost.Property(entity => entity.Reactions)
            .HasConversion(
                value => Serialize(value),
                value => Deserialize(value, () => new List<FeedReaction>()))
            .Metadata.SetValueComparer(JsonComparer<List<FeedReaction>>());

        var grade = modelBuilder.Entity<Grade>();
        grade.ToTable("Grades");
        grade.HasKey(entity => entity.Id);
        grade.HasIndex(entity => entity.UserId);
        grade.Property(entity => entity.ModuleCode).HasMaxLength(40).IsRequired();
        grade.Property(entity => entity.ModuleName).HasMaxLength(160).IsRequired();
        grade.Property(entity => entity.Value).HasPrecision(3, 1).IsRequired();
        grade.Property(entity => entity.Ects).IsRequired();
        grade.Property(entity => entity.CreatedAt).IsRequired();

        var exam = modelBuilder.Entity<ExamEntry>();
        exam.ToTable("ExamEntries");
        exam.HasKey(entity => entity.Id);
        exam.HasIndex(entity => entity.UserId);
        exam.Property(entity => entity.ModuleName).HasMaxLength(160).IsRequired();
        exam.Property(entity => entity.Location).HasMaxLength(160);
        exam.Property(entity => entity.Notes).HasMaxLength(500);
        exam.Property(entity => entity.ExamDate).IsRequired();
        exam.Property(entity => entity.CreatedAt).IsRequired();
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static T Deserialize<T>(string? value, Func<T> fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback() : JsonSerializer.Deserialize<T>(value, JsonOptions) ?? fallback();

    private static ValueComparer<T> JsonComparer<T>() => new(
        (left, right) => Serialize(left) == Serialize(right),
        value => Serialize(value).GetHashCode(),
        value => Deserialize(Serialize(value), () => value));
}
