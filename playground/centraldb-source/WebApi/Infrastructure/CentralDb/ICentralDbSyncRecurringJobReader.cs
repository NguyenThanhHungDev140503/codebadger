using Hangfire.Storage;

namespace WebApi.Infrastructure.CentralDb;

/// <summary>
/// Narrow read seam for Central DB sync recurring jobs.
/// Implementations read from Hangfire storage; tests supply a fake.
/// </summary>
public interface ICentralDbSyncRecurringJobReader
{
    /// <summary>Returns the recurring-job DTO, or <c>null</c> if it does not exist.</summary>
    RecurringJobDto? Get(string recurringJobId);
}
