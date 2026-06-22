using CampusConnect.Application.Common;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Features.Courses;

public record CourseDto(string Code, string StudyProgram, bool IsActive, DateTime CreatedAt);
public record CreateCourseCommand(string Code, string StudyProgram);

public class CoursesService(ICourseRepository courseRepository, IGroupRepository groupRepository)
{
    private static readonly HashSet<string> SystemCourseCodes = ["ADMIN", "LECTURER", "MANAGEMENT"];

    public async Task<IReadOnlyList<CourseDto>> GetCoursesAsync(bool includeSystemCourses = true, CancellationToken cancellationToken = default)
    {
        var courses = await courseRepository.GetAllAsync(cancellationToken);
        return courses
            .Where(course => course.IsActive)
            .Where(course => includeSystemCourses || IsStudentCourse(course.Code))
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
            IsActive = true
        };

        await courseRepository.AddAsync(course, cancellationToken);
        if (IsStudentCourse(course.Code))
            await groupRepository.EnsureCourseGroupAsync(course.Code, course.StudyProgram);

        return Result<CourseDto>.Success(ToDto(course));
    }

    public static string NormalizeCourseCode(string courseCode) => courseCode.Trim().ToUpperInvariant();

    public static CourseDto ToDto(Course course) => new(course.Code, course.StudyProgram, course.IsActive, course.CreatedAt);

    public static bool IsStudentCourse(string courseCode) => !SystemCourseCodes.Contains(NormalizeCourseCode(courseCode));

    private static string? Validate(CreateCourseCommand command, string normalizedCode)
    {
        if (string.IsNullOrWhiteSpace(normalizedCode) || string.IsNullOrWhiteSpace(command.StudyProgram))
            return "Fill in all course fields.";

        if (normalizedCode.Length > 40)
            return "Course code must be at most 40 characters long.";

        if (command.StudyProgram.Trim().Length > 120)
            return "Study program must be at most 120 characters long.";

        return null;
    }
}
