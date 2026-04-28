using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Infrastructure.Persistence;

public sealed class CampusConnectDbContext(DbContextOptions<CampusConnectDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();

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
    }
}
