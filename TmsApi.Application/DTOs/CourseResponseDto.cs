namespace TmsApi.Application.DTOs;

public record CourseResponseDto(
    int Id,
    string Code,
    string Title,
    int Credits,
    int MaxCapacity,
    int EnrollmentCount);

public static class CourseResponseDtoFields
{
public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(CourseResponseDto.Id),
        nameof(CourseResponseDto.Code),
        nameof(CourseResponseDto.Title),
        nameof(CourseResponseDto.Credits),
        nameof(CourseResponseDto.MaxCapacity),
        nameof(CourseResponseDto.EnrollmentCount)
    };
}