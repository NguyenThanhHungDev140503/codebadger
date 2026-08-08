namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Shared recovery constants used by both the Application and Infrastructure layers.
/// Values mirrored in SQL DDL and CAS guards must stay in sync.
/// </summary>
public static class BootstrapRecoveryConstants
{
    /// <summary>
    /// Maximum number of automatic recovery attempts before the request is marked failed.
    /// Mirrored in the SQL CAS guard: <c>reconcile_attempt_count &lt; 3</c>.
    /// </summary>
    public const int MaxRecoveryAttempts = 3;

    /// <summary>
    /// Maximum length of sanitized diagnostic text (exception messages, Hangfire state data)
    /// before truncation.
    /// </summary>
    public const int MaxDiagnosticLength = 1_000;

    /// <summary>
    /// Maximum length of error messages persisted in the database.
    /// Mirrored in SQL: <c>LEFT(@ErrorMessage, 4000)</c>.
    /// </summary>
    public const int MaxPersistedErrorLength = 4_000;

}
