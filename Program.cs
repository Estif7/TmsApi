var builder = WebApplication.CreateBuilder(args);

// Services: add authentication / authorization services
builder.Services
    .AddAuthentication("Training")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        TrainingAuthHandler>("Training", null);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

app.Run();