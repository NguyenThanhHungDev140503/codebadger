namespace Application.Features.CentralDbSync.Dtos;

public sealed record BootstrapResponseDto(
    Guid RequestId,
    string? HangfireJobId,
    string RuleName,
    string SourceTable,
    string Status,
    string? StatusUrl);
