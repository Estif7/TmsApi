using NSubstitute;
using TmsApi.Application.DTOs;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using Xunit;

namespace TmsApi.Tests;

public class EnrollStudentHandlerTests
{
    private readonly IEnrollmentService _enrollmentService = Substitute.For<IEnrollmentService>();
    private readonly ICourseService _courseService = Substitute.For<ICourseService>();
    private readonly EnrollStudentHandler _handler;

    public EnrollStudentHandlerTests()
    {
        _handler = new EnrollStudentHandler(_enrollmentService, _courseService);
    }

    [Fact]
    public async Task Handle_WhenCourseNotFound_ReturnsCourseNotFoundFailure()
    {
        // Arrange
        var command = new EnrollStudentCommand(StudentId: 101, CourseCode: "CS101");

        _courseService.GetByCodeAsync(command.CourseCode, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult<CourseResponseDto?>(null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        await _enrollmentService.DidNotReceive()
            .AddAsync(Arg.Any<Enrollment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCourseIsAtCapacity_ReturnsCourseFullFailure()
    {
        // Arrange
        var command = new EnrollStudentCommand(StudentId: 101, CourseCode: "CS101");
        var fullCourse = new CourseResponseDto(
            Id: 1, 
            Code: "CS101", 
            Title: "Computer Science 101", 
            MaxCapacity: 5, 
            EnrollmentCount: 5
        );

        _courseService.GetByCodeAsync(command.CourseCode, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult<CourseResponseDto?>(fullCourse));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        await _enrollmentService.DidNotReceive()
            .AddAsync(Arg.Any<Enrollment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyEnrolled_ReturnsAlreadyEnrolledFailure()
    {
        // Arrange
        var command = new EnrollStudentCommand(StudentId: 101, CourseCode: "CS101");
        var course = new CourseResponseDto(
            Id: 1, 
            Code: "CS101", 
            Title: "Computer Science 101", 
            MaxCapacity: 10, 
            EnrollmentCount: 2
        );

        _courseService.GetByCodeAsync(command.CourseCode, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult<CourseResponseDto?>(course));

        _enrollmentService.ExistsAsync(command.StudentId, command.CourseCode, Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult(true));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        await _enrollmentService.DidNotReceive()
            .AddAsync(Arg.Any<Enrollment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_AddsEnrollmentAndReturnsSuccess()
    {
        // Arrange
        var command = new EnrollStudentCommand(StudentId: 101, CourseCode: "CS101");
        var course = new CourseResponseDto(
            Id: 1, 
            Code: "CS101", 
            Title: "Computer Science 101", 
            MaxCapacity: 10, 
            EnrollmentCount: 2
        );

        _courseService.GetByCodeAsync(command.CourseCode, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult<CourseResponseDto?>(course));

        _enrollmentService.ExistsAsync(command.StudentId, command.CourseCode, Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult(false));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(command.StudentId, result.Value.StudentId);
        Assert.Equal(command.CourseCode, result.Value.CourseCode);

        await _enrollmentService.Received(1)
            .AddAsync(Arg.Is<Enrollment>(e => e.StudentId == command.StudentId && e.CourseId == course.Id), Arg.Any<CancellationToken>());
    }
}