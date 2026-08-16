using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService(
    TmsDbContext context,
    ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt,
                e.Status ?? "Pending"))
            .FirstOrDefaultAsync(ct);

    public async Task<EnrollmentResponseDto> CreateAsync(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct)
    {
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Student {StudentId} enrolled in course {CourseId}",
            enrollment.StudentId,
            enrollment.CourseId);

        return (await GetByIdAsync(courseId, enrollment.Id, ct))!;
    }

    public Task<List<EnrollmentResponseDto>> GetByCourseAsync(
        int courseId,
        CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt,
                e.Status ?? "Pending"))
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct) =>
    context.Enrollments
        .AsNoTracking()
        .AnyAsync(e => e.StudentId == studentId && e.Course.Code == courseCode, ct);

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);
        logger.LogInformation(
            "Student {StudentId} enrolled in course {CourseId}",
            enrollment.StudentId,
            enrollment.CourseId);
    }

    public Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync(ct);

    // --- Added for Module 9 Angular State Management ---

    public Task<List<EnrollmentResponseDto>> GetAllAsync(CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt,
                e.Status ?? "Pending"))
            .ToListAsync(ct);

    public async Task<bool> ApproveAsync(int id, CancellationToken ct)
    {
        var enrollment = await context.Enrollments.FindAsync([id], ct);
        if (enrollment is null) return false;

        enrollment.Status = "Approved"; // Modify entity property

        // Force EF Core to mark the entity as modified (if state tracking is tricky)
        context.Entry(enrollment).Property(e => e.Status).IsModified = true;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Enrollment {Id} status set to Approved in DB", id);
        return true;
    }
}