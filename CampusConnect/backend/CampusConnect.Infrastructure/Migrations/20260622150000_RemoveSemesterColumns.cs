using CampusConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Infrastructure.Migrations;

[DbContext(typeof(CampusConnectDbContext))]
[Migration("20260622150000_RemoveSemesterColumns")]
public partial class RemoveSemesterColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            PRAGMA foreign_keys = OFF;

            CREATE TABLE Courses_new (
                Code TEXT NOT NULL CONSTRAINT PK_Courses PRIMARY KEY,
                StudyProgram TEXT NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            INSERT INTO Courses_new (Code, StudyProgram, IsActive, CreatedAt)
            SELECT Code, StudyProgram, IsActive, CreatedAt
            FROM Courses;

            DROP TABLE Courses;
            ALTER TABLE Courses_new RENAME TO Courses;

            CREATE TABLE Users_new (
                Id TEXT NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
                Email TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                StudyProgram TEXT NOT NULL,
                Course TEXT NOT NULL,
                Role TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                Location TEXT NOT NULL,
                PhoneNumber TEXT NOT NULL,
                ProfileNote TEXT NOT NULL,
                IsActive INTEGER NOT NULL
            );

            INSERT INTO Users_new (Id, Email, PasswordHash, DisplayName, StudyProgram, Course, Role, CreatedAt, Location, PhoneNumber, ProfileNote, IsActive)
            SELECT Id, Email, PasswordHash, DisplayName, StudyProgram, Course, Role, CreatedAt, Location, PhoneNumber, ProfileNote, IsActive
            FROM Users;

            DROP TABLE Users;
            ALTER TABLE Users_new RENAME TO Users;
            CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);

            PRAGMA foreign_keys = ON;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            PRAGMA foreign_keys = OFF;

            CREATE TABLE Courses_old (
                Code TEXT NOT NULL CONSTRAINT PK_Courses PRIMARY KEY,
                StudyProgram TEXT NOT NULL,
                Semester INTEGER NULL,
                IsActive INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            INSERT INTO Courses_old (Code, StudyProgram, Semester, IsActive, CreatedAt)
            SELECT Code, StudyProgram, NULL, IsActive, CreatedAt
            FROM Courses;

            DROP TABLE Courses;
            ALTER TABLE Courses_old RENAME TO Courses;

            CREATE TABLE Users_old (
                Id TEXT NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
                Email TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                StudyProgram TEXT NOT NULL,
                Semester INTEGER NULL,
                Course TEXT NOT NULL,
                Role TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                Location TEXT NOT NULL,
                PhoneNumber TEXT NOT NULL,
                ProfileNote TEXT NOT NULL,
                IsActive INTEGER NOT NULL
            );

            INSERT INTO Users_old (Id, Email, PasswordHash, DisplayName, StudyProgram, Semester, Course, Role, CreatedAt, Location, PhoneNumber, ProfileNote, IsActive)
            SELECT Id, Email, PasswordHash, DisplayName, StudyProgram, NULL, Course, Role, CreatedAt, Location, PhoneNumber, ProfileNote, IsActive
            FROM Users;

            DROP TABLE Users;
            ALTER TABLE Users_old RENAME TO Users;
            CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);

            PRAGMA foreign_keys = ON;
            """);
    }
}
