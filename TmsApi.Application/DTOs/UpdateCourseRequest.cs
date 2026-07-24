namespace TmsApi.Application.DTOs;

public record UpdateCourseRequest
{
    public required string Title { get; init; }
}