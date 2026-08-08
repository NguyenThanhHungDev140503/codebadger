namespace Application.Features.CentralDbSync.Mapping;

public sealed record ColumnMapping
{
    public required string TargetColumn { get; init; }
    public required string TargetType { get; init; }
    public string? SourceColumn { get; init; }
    public string? SourceExpression { get; init; }
    public string? Transform { get; init; }
    public IReadOnlyList<string> TransformDependsOn { get; init; } = [];
    public bool IsPrimaryKey { get; init; }
    public bool IsActiveFlag { get; init; }
}
