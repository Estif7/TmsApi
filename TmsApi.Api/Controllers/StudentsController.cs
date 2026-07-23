using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController(TmsDbContext context) : ControllerBase
{
    // GET returns the current Version (xmin) so the client can round-trip it.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var student = await context.Students.FindAsync(id);
        return student is not null ? Ok(student) : NotFound();
    }

    public record UpdateNameRequest(string Name, uint ExpectedVersion);
    public record UpdateGpaRequest(decimal Gpa, uint ExpectedVersion);

    [HttpPatch("{id}/name")]
    public async Task<IActionResult> UpdateName(int id, [FromBody] UpdateNameRequest request)
    {
        var student = await context.Students.FindAsync(id);
        if (student is null) return NotFound();

        student.Name = request.Name;
        context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

        // Tell EF "this is the version I loaded" — it will compare this
        // against the ACTUAL current value in the database on save.
        context.Entry(student).Property(s => s.Version).OriginalValue = request.ExpectedVersion;

        try
        {
            await context.SaveChangesAsync();
            return Ok(student);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Message = "This student was modified by someone else. Reload and try again." });
        }
    }

    [HttpPatch("{id}/gpa")]
    public async Task<IActionResult> UpdateGpa(int id, [FromBody] UpdateGpaRequest request)
    {
        var student = await context.Students.FindAsync(id);
        if (student is null) return NotFound();

        student.GPA = request.Gpa;
        context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

        context.Entry(student).Property(s => s.Version).OriginalValue = request.ExpectedVersion;

        try
        {
            await context.SaveChangesAsync();
            return Ok(student);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Message = "This student was modified by someone else. Reload and try again." });
        }
    }
}
