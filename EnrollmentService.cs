// --- The contract ---
public interface IEnrollmentService
{
    Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode);
    Task<EnrollmentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}

// --- The implementation (scoped service, singleton-backed store) ---
public class EnrollmentService : IEnrollmentService
{
    private readonly EnrollmentStore _store;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(EnrollmentStore store, ILogger<EnrollmentService> logger)
    {
        _store = store;
        _logger = logger;
    }

    public Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode)
    {
        // Check for duplicate enrollment
        var existing = _store.Data.Values
            .FirstOrDefault(e => e.StudentId == studentId && e.CourseCode == courseCode);

        if (existing is not null)
        {
            _logger.LogWarning(
                "Duplicate enrollment attempt {StudentId} already in {CourseCode} (record {EnrollmentId})",
                studentId, courseCode, existing.Id);
            return Task.FromResult(existing);
        }

        var id = Guid.NewGuid().ToString("N")[..8];
        var record = new EnrollmentRecord(id, studentId, courseCode, DateTime.UtcNow);
        _store.Data[id] = record;

        _logger.LogInformation(
            "Enrolled {StudentId} in {CourseCode} record {EnrollmentId}",
            studentId, courseCode, id);

        return Task.FromResult(record);
    }

    public Task<EnrollmentRecord?> GetByIdAsync(string id)
    {
        _store.Data.TryGetValue(id, out var record);

        if (record is null)
        {
            _logger.LogWarning("Enrollment {EnrollmentId} not found", id);
        }

        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
    {
        IReadOnlyList<EnrollmentRecord> all = _store.Data.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Data.Remove(id);

        if (removed)
            _logger.LogInformation("Deleted enrollment {EnrollmentId}", id);
        else
            _logger.LogWarning("Delete failed enrollment {EnrollmentId} not found", id);

        return Task.FromResult(removed);
    }
}

// --- The data shape ---
public record EnrollmentRecord(
    string Id, string StudentId, string CourseCode, DateTime EnrolledAt);