using Microsoft.Extensions.Caching.Hybrid;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;
using TmsApi.Domain.Exceptions;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Services;
using TmsApi.Api.Filters;
using TmsApi.Api.Middlewares;
using TmsApi.Api.Options;
using TmsApi.Api;
using Asp.Versioning;
using MediatR;
using FluentValidation;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Api.ExceptionHandlers;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Api.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Infrastructure.Transcripts;
using System.Threading.Channels;
using TmsApi.Application.Transcripts;
using TmsApi.Api.Workers;
using TmsApi.Api.Hubs;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using TmsApi.Infrastructure.ExternalServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

const string ServiceName = "tms-api";

var builder = WebApplication.CreateBuilder(args);
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.JsonWriterOptions = new() { Indented = false };
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (context.Exception is not null)
        {
            context.ProblemDetails.Detail = context.Exception.Message;
        }
    };
});

// Add Identity Core with enterprise password and lockout policies
builder.Services.AddIdentityCore<TmsUser>(options =>
{
    // Enterprise Password Policy
    options.Password.RequiredLength = 12;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    
    // Brute-Force Lockout Protection
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<TmsDbContext>();

builder.Services.AddAuthorization();

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description => description.GroupName == "v1";
});
builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description => description.GroupName == "v2";
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

// LoggingBehavior FIRST—it must wrap ValidationBehavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<TokenService>();

builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
        .LogTo(Console.WriteLine, LogLevel.Information) // Log SQL to output window
        .EnableSensitiveDataLogging()); // Show parameters in query logs (dev only)

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);

        return tier switch
        {
            ApiKeyTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"paid:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 200,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            ApiKeyTier.Free => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"free:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 30,
                    TokensPerPeriod = 10,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            _ => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"anon:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    TokensPerPeriod = 5,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                })
        };
    });

    options.AddConcurrencyLimiter("transcripts", opt =>
    {
        opt.PermitLimit = 5;      // 5 in-flight transcripts maximum
        opt.QueueLimit = 20;      // queue up to 20 more
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddTokenBucketLimiter("search", opt =>
    {
        opt.TokenLimit = 10;
        opt.TokensPerPeriod = 5;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 2;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts))
            retryAfter = ((int)ts.TotalSeconds).ToString();

        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        context.HttpContext.Response.ContentType = "application/problem+json";

        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Rate limit exceeded",
            Detail = $"Too many requests. Retry after {retryAfter} seconds.",
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://tms.local/errors/rate_limit_exceeded"
        }, ct);
    };
});

builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();

builder.Services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();

builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
    new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait }));

builder.Services.AddHostedService<TranscriptWorker>();
builder.Services.AddSignalR();

builder.Services.AddResiliencePipeline("certificate-api", pipeline =>
{
    pipeline
        // Outer: per-request hard timeout protects against hangs
        .AddTimeout(TimeSpan.FromSeconds(5))
        // Middle: circuit breaker protects against sustained outage
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15),
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>(),
            OnOpened = args =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("========== Circuit OPENED — certificate service unavailable ==========");
                Console.ResetColor();
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("========== Circuit CLOSED — certificate service recovered ==========");
                Console.ResetColor();
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("========== CIRCUIT HALF-OPEN ==========");
                Console.ResetColor();
                return ValueTask.CompletedTask;
            }
        })
        // Inner: retry with jitter only for transient failures
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>(),
            OnRetry = args =>
            {
                Console.WriteLine(
                    $"Retry #{args.AttemptNumber} after {args.RetryDelay.TotalMilliseconds:F0}ms ({args.Outcome.Exception?.GetType().Name})");
                return ValueTask.CompletedTask;
            }
        });
});

builder.Services.AddHttpClient<ICertificateService, CertificateService>((sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>().GetValue<string>("TmsApi:PublicBaseUrl")
        ?? "http://localhost:5121";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("alive"), tags: ["live"])
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("TmsDatabase")!,
        name: "postgres",
        tags: ["ready"]);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: ServiceName, serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddSource(ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddMeter(ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Extract JWT token from HttpOnly cookie if no Bearer header is present
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("tms_auth", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});

var app = builder.Build();

app.UseCors("AllowAngular");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
}).DisableRateLimiting();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).DisableRateLimiting();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<V1DeprecationMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true || context.Request.Cookies.ContainsKey("tms_auth"))
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false, // MUST be false so Angular JavaScript can read it
            Secure = !builder.Environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict
        });
    }
    await next(context);
});

app.MapControllers();
app.MapHub<TmsHub>("/hubs/tms");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TMS API Reference")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

        options
            .AddDocument("v1", "API Version 1.0")
            .AddDocument("v2", "API Version 2.0");
    });
}

app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});

// Apply migrations and seed data at startup (Development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await context.Database.MigrateAsync();

    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };
        context.Students.AddRange(students);
        await context.SaveChangesAsync();
    }

    await TmsApi.Infrastructure.Persistence.DataSeeder.SeedAsync(context);
}

// --- Lab-only fake certificate service ---
var attempts = 0;
app.MapPost("/fake/certificates", async () =>
{
    var n = Interlocked.Increment(ref attempts);

    if (n % 7 == 0)
    {
        await Task.Delay(TimeSpan.FromSeconds(20));
        return Results.Ok(new { Status = "issued", Attempt = n });
    }

    if (n % 3 != 0)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    if (n % 11 == 0)
    {
        return Results.BadRequest(new { error = "validation_failed" });
    }

    return Results.Ok(new { Status = "issued", Attempt = n });
}).WithTags("lab-fixtures").DisableRateLimiting();

app.MapHub<TmsApi.Api.Hubs.EnrollmentHub>("/hubs/enrollments");

app.Run();