using System;
using System.Collections.Generic;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

#nullable disable

namespace CampusConnect.Infrastructure.Migrations;

[DbContext(typeof(CampusConnectDbContext))]
partial class CampusConnectDbContextModelSnapshot : ModelSnapshot
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.7");

        modelBuilder.Entity("CampusConnect.Domain.Entities.CampusGroup", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<string>("AccentColor").IsRequired().HasMaxLength(16).HasColumnType("TEXT");
            b.Property<string>("Audience").IsRequired().HasMaxLength(80).HasColumnType("TEXT");
            b.Property<HashSet<Guid>>("AssignedUserIds")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasConversion(JsonConverter<HashSet<Guid>>(() => new HashSet<Guid>()));
            b.Property<string>("CourseCode").HasMaxLength(40).HasColumnType("TEXT");
            b.Property<string>("Description").IsRequired().HasMaxLength(240).HasColumnType("TEXT");
            b.Property<string>("IconLabel").IsRequired().HasMaxLength(8).HasColumnType("TEXT");
            b.Property<Dictionary<Guid, GroupMemberPermission>>("MemberPermissions")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasConversion(JsonConverter<Dictionary<Guid, GroupMemberPermission>>(() => new Dictionary<Guid, GroupMemberPermission>()));
            b.Property<string>("Name").IsRequired().HasMaxLength(80).HasColumnType("TEXT");
            b.Property<string>("OwnerLabel").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
            b.Property<Guid?>("OwnerUserId").HasColumnType("TEXT");
            b.Property<GroupSettings>("Settings")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasConversion(JsonConverter<GroupSettings>(() => new GroupSettings()));
            b.Property<string>("Type").IsRequired().HasMaxLength(32).HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("CourseCode");
            b.ToTable("CampusGroups");
        });

        modelBuilder.Entity("CampusConnect.Domain.Entities.Course", b =>
        {
            b.Property<string>("Code").HasMaxLength(40).HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<bool>("IsActive").HasColumnType("INTEGER");
            b.Property<int>("Semester").HasColumnType("INTEGER");
            b.Property<string>("StudyProgram").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
            b.HasKey("Code");
            b.ToTable("Courses");
        });

        modelBuilder.Entity("CampusConnect.Domain.Entities.ExamEntry", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<DateTime>("ExamDate").HasColumnType("TEXT");
            b.Property<string>("Location").HasMaxLength(160).HasColumnType("TEXT");
            b.Property<string>("ModuleName").IsRequired().HasMaxLength(160).HasColumnType("TEXT");
            b.Property<string>("Notes").HasMaxLength(500).HasColumnType("TEXT");
            b.Property<Guid>("UserId").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("UserId");
            b.ToTable("ExamEntries");
        });

        modelBuilder.Entity("CampusConnect.Domain.Entities.FeedPost", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<Guid>("AuthorId").HasColumnType("TEXT");
            b.Property<string>("AuthorName").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
            b.Property<List<FeedComment>>("Comments")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasConversion(JsonConverter<List<FeedComment>>(() => new List<FeedComment>()));
            b.Property<string>("Content").IsRequired().HasMaxLength(4000).HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<Guid>("GroupId").HasColumnType("TEXT");
            b.Property<List<FeedReaction>>("Reactions")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasConversion(JsonConverter<List<FeedReaction>>(() => new List<FeedReaction>()));
            b.HasKey("Id");
            b.HasIndex("CreatedAt");
            b.HasIndex("GroupId");
            b.ToTable("FeedPosts");
        });

        modelBuilder.Entity("CampusConnect.Domain.Entities.Grade", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<int>("Ects").HasColumnType("INTEGER");
            b.Property<string>("ModuleCode").IsRequired().HasMaxLength(40).HasColumnType("TEXT");
            b.Property<string>("ModuleName").IsRequired().HasMaxLength(160).HasColumnType("TEXT");
            b.Property<Guid>("UserId").HasColumnType("TEXT");
            b.Property<decimal>("Value").HasPrecision(3, 1).HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("UserId");
            b.ToTable("Grades");
        });

        modelBuilder.Entity("CampusConnect.Domain.Entities.User", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<string>("Course").IsRequired().HasMaxLength(40).HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<string>("DisplayName").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
            b.Property<string>("Email").IsRequired().HasMaxLength(256).HasColumnType("TEXT");
            b.Property<string>("Location").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
            b.Property<string>("PasswordHash").IsRequired().HasMaxLength(256).HasColumnType("TEXT");
            b.Property<string>("PhoneNumber").IsRequired().HasMaxLength(40).HasColumnType("TEXT");
            b.Property<string>("ProfileNote").IsRequired().HasMaxLength(280).HasColumnType("TEXT");
            b.Property<string>("Role").IsRequired().HasMaxLength(32).HasColumnType("TEXT");
            b.Property<int>("Semester").HasColumnType("INTEGER");
            b.Property<string>("StudyProgram").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("Email").IsUnique();
            b.ToTable("Users");
        });
#pragma warning restore 612, 618
    }

    private static ValueConverter<T, string> JsonConverter<T>(Func<T> fallback) => new(
        value => JsonSerializer.Serialize(value, JsonOptions),
        value => string.IsNullOrWhiteSpace(value) ? fallback() : JsonSerializer.Deserialize<T>(value, JsonOptions) ?? fallback());
}