using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Api.Hubs;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/enrollments")]
[Tags("Enrollments")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class EnrollmentsController(
    ICourseService courseService,
    IEnrollmentService enrollmentService) : ControllerBase
{
    [HttpGet(Name = "ListCourseEnrollments")]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List enrolments for a course")]
    public async Task<IActionResult> GetEnrollments(int courseId, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null) return NotFound();
        var enrollments = await enrollmentService.GetByCourseAsync(courseId, ct);
        return Ok(enrollments);
    }

    [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
    [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get one enrolment for a course")]
    public async Task<IActionResult> GetEnrollment(
        int courseId,
        int id,
        CancellationToken ct)
    {
        var enrollment = await enrollmentService.GetByIdAsync(courseId, id, ct);
        return enrollment is not null
            ? Ok(enrollment)
            : NotFound();
    }

    [HttpPost(Name = "CreateEnrollment")]
    [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Create an enrolment for a course")]
    public async Task<IActionResult> CreateEnrollment(
        int courseId,
        [FromBody] EnrollStudentRequest request,
        CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null) return NotFound();

        var created = await enrollmentService.CreateAsync(courseId, request, ct);

        return CreatedAtRoute(nameof(GetEnrollment), new { courseId = created.CourseId, id = created.Id }, created);
    }

    // --- Added Endpoints for Module 9 Angular Integration ---

    [HttpGet("/api/enrollments", Name = "GetAllEnrollments")]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List all enrolments across all courses")]
    public async Task<IActionResult> GetAllEnrollments(CancellationToken ct)
    {
        var enrollments = await enrollmentService.GetAllAsync(ct);
        return Ok(enrollments);
    }

    [HttpPost("/api/enrollments/{id:int}/approve", Name = "ApproveEnrollment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Approve an enrolment record")]
    public async Task<IActionResult> ApproveEnrollment(
        int id, 
        [FromServices] IHubContext<EnrollmentHub> hubContext, 
        CancellationToken ct)
    {
        var success = await enrollmentService.ApproveAsync(id, ct);
        if (!success) return NotFound();

        // Broadcast change to all connected Angular clients
        await hubContext.Clients.All.SendAsync("EnrollmentStatusUpdated", new { enrollmentId = id, status = "Approved" }, ct);

        return NoContent();
    }
}