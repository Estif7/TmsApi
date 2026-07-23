using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
[Tags("Courses")]
[Produces("application/json")]
public class CoursesController(ICourseService courseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await courseService.GetCoursesAsync(request, ct);

        return Ok(new
        {
            data = result.Items,
            meta = new
            {
                result.TotalCount,
                result.Page,
                result.PageSize,
                result.TotalPages,
                result.HasNext,
                result.HasPrevious
            },
            links = new
            {
                self = $"/api/v2/courses?page={result.Page}&pageSize={result.PageSize}",
                next = result.HasNext
                    ? $"/api/v2/courses?page={result.Page + 1}&pageSize={result.PageSize}"
                    : (string?)null,
                prev = result.HasPrevious
                    ? $"/api/v2/courses?page={result.Page - 1}&pageSize={result.PageSize}"
                    : (string?)null,
                enroll = "/api/v2/enrollments"
            }
        });
    }
}