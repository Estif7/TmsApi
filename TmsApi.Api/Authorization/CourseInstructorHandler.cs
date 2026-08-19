using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Authorization;

public sealed class CourseInstructorHandler
    : AuthorizationHandler<CourseInstructorRequirement, Course>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CourseInstructorRequirement requirement,
        Course resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (context.User.IsInRole("Admin") ||
            (context.User.IsInRole("Instructor") && resource.InstructorId == userId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}