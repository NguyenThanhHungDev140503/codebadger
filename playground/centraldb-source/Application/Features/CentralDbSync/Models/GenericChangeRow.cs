namespace Application.Features.CentralDbSync.Models;

public sealed record GenericChangeRow(
    string Operation,
    long ChangeVersion,
    IReadOnlyList<object?> PrimaryKey,
    GenericSourceRow? CurrentValues);
