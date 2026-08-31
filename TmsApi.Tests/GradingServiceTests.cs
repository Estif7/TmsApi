using TmsApi.Application.Grading;
using Xunit;

namespace TmsApi.Tests;

public class GradingServiceTests
{
    [Fact]
    public void CalculateLetterGrade_HighScore_ReturnsDistinction()
    {
        var service = new GradingService();
        var result = service.CalculateLetterGrade(score: 85m, maxScore: 100m);
        Assert.Equal(GradeLevel.Distinction, result);
    }

    [Theory]
    [InlineData(0, 100, GradeLevel.Fail)]           // Zero score boundary
    [InlineData(70, 100, GradeLevel.Distinction)]  // Distinction threshold boundary
    [InlineData(50, 100, GradeLevel.Pass)]         // Pass threshold boundary
    [InlineData(-1, 100, GradeLevel.Invalid)]      // Negative score boundary
    [InlineData(101, 100, GradeLevel.Invalid)]     // Score exceeds max boundary
    [InlineData(50, 0, GradeLevel.Invalid)]        // Zero max score boundary
    public void CalculateLetterGrade_VariousInputs_ReturnsExpectedLevel(
        decimal score, decimal maxScore, GradeLevel expected)
    {
        var service = new GradingService();
        var result = service.CalculateLetterGrade(score, maxScore);
        Assert.Equal(expected, result);
    }
}