namespace Application.Features.CentralDbSync.Dtos;

public sealed record BootstrapJobListItemDto(
    Guid RequestId,
    string RuleName,
    string Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    string? ErrorMessage);
