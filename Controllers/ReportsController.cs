using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(TmsDbContext context) : ControllerBase
{
    // 1. How many active students have GPA >= 3.0?
    [HttpGet("active-honor-count")]
    public async Task<IActionResult> ActiveHonorCount()
    {
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();

        return Ok(new { count });
    }

    // 2. Which courses have the most enrollments, sorted descending?
    [HttpGet("courses-by-enrollment")]
    public async Task<IActionResult> CoursesByEnrollment()
    {
        var list = await context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();

        return Ok(list);
    }

    // 3. What is the average GPA per course?
    [HttpGet("average-gpa-per-course")]
    public async Task<IActionResult> AverageGpaPerCourse()
    {
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Course = g.Key,
                AverageGPA = g.Average(e => e.Student.GPA)
            })
            .ToListAsync();

        return Ok(list);
    }

    // 4. Which students have zero enrollments? (both patterns)
    [HttpGet("students-without-enrollments-subquery")]
    public async Task<IActionResult> StudentsWithoutEnrollmentsSubquery()
    {
        var list = await context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("students-without-enrollments-leftjoin")]
    public async Task<IActionResult> StudentsWithoutEnrollmentsLeftJoin()
    {
        var list = await context.Students
            .LeftJoin(context.Enrollments,
                s => s.Id,
                e => e.StudentId,
                (s, e) => new { s, e })
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();

        return Ok(list);
    }

    // Paged student list — page size 20, stable sort by name before Skip/Take
    [HttpGet("students-paged")]
    public async Task<IActionResult> StudentsPaged(int page = 1, CancellationToken cancellationToken = default)
    {
        const int pageSize = 20;

        var students = await context.Students
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(students);
    }

    // Top 5 courses by enrollment count
    [HttpGet("top-courses")]
    public async Task<IActionResult> TopCourses(CancellationToken cancellationToken = default)
    {
        var list = await context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        return Ok(list);
    }
}
