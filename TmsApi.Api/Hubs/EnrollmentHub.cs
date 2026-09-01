using Microsoft.AspNetCore.SignalR;

namespace TmsApi.Api.Hubs;

public class EnrollmentHub : Hub
{
    public async Task SendEnrollmentUpdate(int enrollmentId, string status)
    {
        await Clients.All.SendAsync("ReceiveEnrollmentUpdate", new { EnrollmentId = enrollmentId, Status = status });
    }
}