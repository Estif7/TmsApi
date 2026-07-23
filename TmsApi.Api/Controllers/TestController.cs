using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using TmsApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building the query object (no database contact)...");
        var query = context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy(s => s.Name);

        Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
        var results = orderedQuery.ToList(); // Execution is triggered here

        Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");

        return Ok(results);
    }

    // Non-translatable helper method
    private static bool IsHonorRoll(decimal gpa)
    {
        return gpa >= 3.5m;
    }

    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
        try
        {
            var students = context.Students
                .Where(s => IsHonorRoll(s.GPA)) // EF Core does not know how to map this method to SQL
                .ToList();

            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
            return BadRequest(new { Message = ex.Message });
        }
    }

    // Part A: Intentional N+1 (for learning) — one query for students,
    // then one extra query PER student inside the loop.
    [HttpGet("n-plus-one")]
    public async Task<IActionResult> NPlusOneDemo(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n>>> N+1 DEMO: Loading students, then querying enrollment count per student...");

        var students = await context.Students.AsNoTracking().ToListAsync(cancellationToken);
        var report = new List<string>();

        foreach (var s in students)
        {
            var count = await context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id, cancellationToken);

            report.Add($"{s.Name}: {count} enrollments");
        }

        Console.WriteLine(">>> N+1 DEMO finished.\n");
        return Ok(report);
    }

    // Part B: Fix with shaping — one single query, database does the counting.
    [HttpGet("n-plus-one-fixed")]
    public async Task<IActionResult> NPlusOneFixed(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n>>> FIXED DEMO: Single shaped query with projected count...");

        var report = await context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count
            })
            .ToListAsync(cancellationToken);

        Console.WriteLine(">>> FIXED DEMO finished.\n");
        return Ok(report);
    }
}
