namespace Application.Features.CentralDbSync.Dtos;

public sealed record CtStatusDto(
    string SourceTable,
    string? SchemaQualifiedName,
    bool IsCtEnabled,
    long? CurrentVersion,
    long? MinValidVersion);
