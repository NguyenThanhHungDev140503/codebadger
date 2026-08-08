namespace Application.Features.CentralDbSync.Mapping;

public sealed record TargetSpec
{
    public string Schema { get; init; } = "report";
    public required string Table { get; init; }
    public required IReadOnlyList<string> PrimaryKey { get; init; }
}
