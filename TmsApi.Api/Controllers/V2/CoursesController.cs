using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Utilities;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
[Tags("Courses")]
[Produces("application/json")]
public class CoursesController(
    ICachedCourseService cachedCourseService,
    ICourseService courseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string? fields,
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var result = await cachedCourseService.GetCoursesAsync(request, ct);

        var shaped = result.Items.ShapeData(fields, CourseResponseDtoFields.Allowed);

        var fieldsQuery = string.IsNullOrWhiteSpace(fields) ? "" : $"&fields={fields}";

        return Ok(new
        {
            data = shaped,
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
                self = $"/api/v2/courses?page={result.Page}&pageSize={result.PageSize}{fieldsQuery}",
                next = result.HasNext
                    ? $"/api/v2/courses?page={result.Page + 1}&pageSize={result.PageSize}{fieldsQuery}"
                    : (string?)null,
                prev = result.HasPrevious
                    ? $"/api/v2/courses?page={result.Page - 1}&pageSize={result.PageSize}{fieldsQuery}"
                    : (string?)null,
                enroll = "/api/v2/enrollments"
            }
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(
        int id, [FromBody] UpdateCourseRequest request, CancellationToken ct)
    {
        var updated = await courseService.UpdateAsync(id, request, ct);
        if (updated is null) return NotFound();

        await cachedCourseService.InvalidateCourseCacheAsync(ct);

        return Ok(updated);
    }
}