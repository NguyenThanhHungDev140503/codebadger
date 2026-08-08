namespace Application.Features.CentralDbSync.Mapping;

public enum JoinKind
{
    Inner,
    Left
}

public sealed record JoinSpec
{
    public required string Table { get; init; }
    public required string Alias { get; init; }
    public JoinKind Kind { get; init; } = JoinKind.Left;
    public required string OnCondition { get; init; }
}
