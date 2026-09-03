using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/enrollments")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class EnrollmentsController(IEnrollmentService enrollmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEnrollments(CancellationToken ct)
    {
        var result = await enrollmentService.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpPost("/api/v{version:apiVersion}/courses/{courseId:int}/enrollments")]
    [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEnrollment(
        int courseId, 
        [FromBody] EnrollStudentRequest request, 
        CancellationToken ct)
    {
        var result = await enrollmentService.CreateAsync(courseId, request, ct);
        return CreatedAtAction(nameof(GetAllEnrollments), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveEnrollment(int id, CancellationToken ct)
    {
        var updated = await enrollmentService.ApproveAsync(id, ct);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpPut("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectEnrollment(int id, CancellationToken ct)
    {
        var updated = await enrollmentService.RejectAsync(id, ct);
        if (!updated) return NotFound();
        return NoContent();
    }
}