using System.Net;
using System.Net.Http.Json;
using TmsApi.Application.DTOs;
using Xunit;

namespace TmsApi.Tests;

public class CourseEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CourseEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(Skip = "Requires local Docker engine for Testcontainers PostgreSQL instance.")]
    public async Task GetCourses_ReturnsSuccessAndPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/courses?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<PagedResponse<CourseResponseDto>>();
        Assert.NotNull(content);
        Assert.True(content.Items.Count() <= 10);
    }
}