using CampusConnect.Application.Features.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.API.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(CoursesService coursesService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetCourses(CancellationToken cancellationToken)
    {
        var courses = await coursesService.GetCoursesAsync(cancellationToken);
        return Ok(courses);
    }
}
