namespace Application.Features.CentralDbSync.Dtos;

public sealed record BootstrapDiagnosticEventDto
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string? FromStatus { get; init; }
    public string? ToStatus { get; init; }
    public string? HangfireJobId { get; init; }
    public string? FencingTokenHash { get; init; }
    public int? ChildSequence { get; init; }
    public long? RowsAffected { get; init; }
    public string DiagnosticCode { get; init; } = string.Empty;
    public string? SanitizedMessage { get; init; }
    public string InitiatedBy { get; init; } = "system";
    public long SequenceNo { get; init; }
}
