namespace Application.Features.CentralDbSync.Mapping;

public sealed record ColumnPredicate
{
    public required string Column { get; init; }
    public required PredicateOperator Operator { get; init; }
    public object? Value { get; init; }
}
