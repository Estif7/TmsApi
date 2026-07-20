using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(TmsDbContext context) : ControllerBase
{
    // Bulk-archive enrollments older than a cutoff, in a single SQL UPDATE —
    // no rows are loaded into memory first.
    [HttpPost("archive-old-enrollments")]
    public async Task<IActionResult> ArchiveOldEnrollments([FromQuery] int olderThanDays, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);

        var rowsAffected = await context.Enrollments
            .Where(e => e.EnrolledAt < cutoff && !e.IsArchived)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsArchived, true), cancellationToken);

        return Ok(new { archivedCount = rowsAffected });
    }

    // Soft-delete a student (sets IsDeleted, doesn't remove the row).
    [HttpDelete("students/{id}/soft-delete")]
    public async Task<IActionResult> SoftDeleteStudent(int id)
    {
        var student = await context.Students.FindAsync(id);
        if (student is null) return NotFound();

        student.IsDeleted = true;
        context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return NoContent();
    }

    // Normal query — respects the HasQueryFilter, soft-deleted students never appear.
    [HttpGet("students/normal-count")]
    public async Task<IActionResult> NormalStudentCount()
    {
        var count = await context.Students.CountAsync();
        return Ok(new { count });
    }

    // Admin restore view — bypasses the query filter to see everything, including soft-deleted.
    [HttpGet("students/all-including-deleted")]
    public async Task<IActionResult> AllStudentsIncludingDeleted()
    {
        var students = await context.Students
            .IgnoreQueryFilters()
            .Select(s => new { s.Id, s.Name, s.IsDeleted })
            .ToListAsync();

        return Ok(students);
    }
}
