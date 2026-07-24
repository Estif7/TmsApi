using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/transcripts")]
[ApiVersion("2.0")]
public class TranscriptsController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public async Task<IActionResult> RequestTranscript([FromBody] object? _, CancellationToken ct)
    {
        // Stub: simulates the 5-15s transcript build job so the concurrency
        // limiter has something real to measure. Exercise 5 replaces this
        // with enqueue + 202 Accepted + status URL + background worker.
        await Task.Delay(TimeSpan.FromSeconds(3), ct);
        return Ok();
    }
}