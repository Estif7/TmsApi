public class EnrollmentWorker(IServiceScopeFactory scopeFactory)
{
    public void ProcessBatch()
    {
        // Create a short-lived scope for this batch run only.
        using var scope = scopeFactory.CreateScope();

        // Resolve the scoped service from the NEW scope's provider,
        // never from the singleton's own (root) provider.
        var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        // Placeholder for the hourly scholarship recalculation logic.
        // The 'using' block disposes the scope (and its scoped services)
        // automatically when ProcessBatch returns.
    }
}