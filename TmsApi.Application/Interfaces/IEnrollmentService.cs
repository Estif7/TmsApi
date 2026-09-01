using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface IEnrollmentService
{
    Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct);

    Task<EnrollmentResponseDto> CreateAsync(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct);

    Task<List<EnrollmentResponseDto>> GetByCourseAsync(
        int courseId,
        CancellationToken ct);

    Task<bool> ExistsAsync(
        int studentId,
        string courseCode,
        CancellationToken ct);

    Task AddAsync(
        Enrollment enrollment,
        CancellationToken ct);

    Task<List<Enrollment>> GetByStudentIdAsync(
        int studentId,
        CancellationToken ct);

    // --- Added for Module 9 Angular State Management ---

    Task<List<EnrollmentResponseDto>> GetAllAsync(
        CancellationToken ct);

    Task<bool> ApproveAsync(
        int id,
        CancellationToken ct);

    Task<bool> RejectAsync(
        int id,
        CancellationToken ct);
}