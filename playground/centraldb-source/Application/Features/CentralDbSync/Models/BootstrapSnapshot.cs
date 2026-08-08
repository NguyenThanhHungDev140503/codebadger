namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Represents a point-in-time consistent snapshot of all rows from a source table
/// captured within a single SQL Server transaction together with the Change Tracking
/// version at that moment.
/// </summary>
/// <remarks>
/// <para>
/// <b>BaselineVersion</b> is the value returned by <c>CHANGE_TRACKING_CURRENT_VERSION()</c>
/// at the moment the snapshot was taken. It serves as the checkpoint for subsequent
/// incremental (ChangeTracking-based) sync runs — they will only read changes with
/// version &gt; BaselineVersion.
/// </para>
/// <para>
/// <b>Rows</b> contains every row currently present in the source table, each
/// represented as a <see cref="GenericSourceRow"/> (a column-name → value dictionary).
/// The applier uses this set to upsert all rows into the target (PostgreSQL) table
/// and to deactivate any target rows that no longer exist in the source (orphan cleanup).
/// </para>
/// <para>
/// <b>Consistency guarantee:</b>
/// <c>IBootstrapSnapshotReader.ReadAsync</c> captures both the version and
/// the full row set inside the same transaction, then re-reads the version after the
/// SELECT. If the version changed, the snapshot is discarded and retried (up to 3
/// times). Only when the version is stable is this record produced, ensuring that
/// Rows is consistent with BaselineVersion.
/// </para>
/// </remarks>
public sealed record BootstrapSnapshot(
    /// <param name="BaselineVersion">Change Tracking version at the moment the snapshot was captured.</param>
    long BaselineVersion,
    /// <param name="Rows">Complete set of rows from the source table at that version.</param>
    IReadOnlyList<GenericSourceRow> Rows);
