namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Reads CT changes from SQL Server in (C0, C1] and applies them to the
/// dynamic staging table. Inserts/updates are staged via
/// <see cref="ITypedBootstrapStagingStore.StageBatchAsync"/>;
/// deletes and filter-excluded rows are removed via
/// <see cref="ITypedBootstrapStagingStore.DeleteStageRowsAsync"/>.
/// </summary>
public sealed class BootstrapCtCatchUpService(
    IStagedBootstrapSourceReader sourceReader,
    ITypedBootstrapStagingStore stagingStore,
    ILogger<BootstrapCtCatchUpService> logger) : IBootstrapCtCatchUpService
{
    public async Task<CtCatchUpResult> CatchUpAsync(
        TableMappingRule rule,
        long baselineVersion,
        long watermarkVersion,
        string stagingSchema,
        string stagingTableName,
        CancellationToken ct)
    {
        try
        {
            // Validate CT history hasn't expired
            var preflight = await sourceReader.ValidateAndCaptureBaselineAsync(rule, ct);
            if (!preflight.IsValid)
            {
                return CtCatchUpResult.Fail("CtHistoryExpired",
                    $"CT history from baseline {baselineVersion} to watermark {watermarkVersion} " +
                    $"is no longer valid: {preflight.ErrorMessage}");
            }

            // Read CT changes in (C0, C1] using CHANGETABLE(CHANGES ...)
            var delta = await sourceReader.ReadCtDeltaAsync(
                rule, baselineVersion, watermarkVersion, ct);

            if (!delta.IsValid)
            {
                return CtCatchUpResult.Fail(
                    delta.ErrorCode!, delta.ErrorMessage!);
            }

            // Apply inserts/updates to staging
            if (delta.Upserts.Count > 0)
            {
                await stagingStore.StageBatchAsync(
                    rule, stagingSchema, stagingTableName, delta.Upserts, ct);
            }

            // Remove source-deleted and filter-excluded rows from staging.
            // DeletedPrimaryKeys entries are object?[] arrays from the CT reader.
            // Now passed directly as tuples for composite PK support.
            if (delta.DeletedPrimaryKeys.Count > 0)
            {
                await stagingStore.DeleteStageRowsAsync(
                    rule, stagingSchema, stagingTableName, delta.DeletedPrimaryKeys, ct);
            }

            var totalChanges = delta.Upserts.Count + delta.DeletedPrimaryKeys.Count;
            logger.LogInformation(
                "CT catch-up for {RuleName} in ({C0}, {C1}]: " +
                "{Upserts} upserts, {Deletes} deletes",
                rule.RuleName, baselineVersion, watermarkVersion,
                delta.Upserts.Count, delta.DeletedPrimaryKeys.Count);

            return CtCatchUpResult.Success(totalChanges);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "CT catch-up failed for {RuleName} ({C0}, {C1}]",
                rule.RuleName, baselineVersion, watermarkVersion);
            return CtCatchUpResult.Fail("CtCatchUpFailed",
                BootstrapDiagnosticSanitizer.Sanitize(ex.Message) ?? "CT catch-up failed.");
        }
    }
}
