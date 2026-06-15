using CampusConnect.Application.Common;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Courses;

public record CourseDto(string Code, string StudyProgram, int? Semester, bool IsActive, DateTime CreatedAt);
public record CreateCourseCommand(string Code, string StudyProgram, int? Semester);

public class CoursesService(ICourseRepository courseRepository, IGroupRepository groupRepository)
{
    public async Task<IReadOnlyList<CourseDto>> GetCoursesAsync(bool includeSemesterlessCourses = true, CancellationToken cancellationToken = default)
    {
        var courses = await courseRepository.GetAllAsync(cancellationToken);
        return courses
            .Where(course => course.IsActive)
            .Where(course => includeSemesterlessCourses || course.Semester.HasValue)
            .Select(ToDto)
            .ToList();
    }

    public async Task<Result<CourseDto>> CreateCourseAsync(CreateCourseCommand command, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCourseCode(command.Code);
        var validationError = Validate(command, code);
        if (validationError is not null)
            return Result<CourseDto>.Failure(validationError);

        if (await courseRepository.FindByCodeAsync(code, cancellationToken) is not null)
            return Result<CourseDto>.Failure("This course already exists.");

        var course = new Course
        {
            Code = code,
            StudyProgram = command.StudyProgram.Trim(),
            Semester = command.Semester,
            IsActive = true
        };

        await courseRepository.AddAsync(course, cancellationToken);
        if (course.Semester.HasValue)
            await groupRepository.EnsureCourseGroupAsync(course.Code, course.StudyProgram);

        return Result<CourseDto>.Success(ToDto(course));
    }

    public static string NormalizeCourseCode(string courseCode) => courseCode.Trim().ToUpperInvariant();

    public static CourseDto ToDto(Course course) => new(course.Code, course.StudyProgram, course.Semester, course.IsActive, course.CreatedAt);

    private static string? Validate(CreateCourseCommand command, string normalizedCode)
    {
        if (string.IsNullOrWhiteSpace(normalizedCode) || string.IsNullOrWhiteSpace(command.StudyProgram))
            return "Fill in all course fields.";

        if (normalizedCode.Length > 40)
            return "Course code must be at most 40 characters long.";

        if (command.StudyProgram.Trim().Length > 120)
            return "Study program must be at most 120 characters long.";

        if (command.Semester is not null and (< 1 or > 6))
            return "Semester must be between 1 and 6.";

        return null;
    }
}
