namespace TmsApi.Application.DTOs;

public record EnrollmentResponseDto(
    int Id,
    int CourseId,
    int StudentId,
    DateTime EnrolledAt,
    string Status,
    string? CourseName = null,
    string? StudentName = null
);