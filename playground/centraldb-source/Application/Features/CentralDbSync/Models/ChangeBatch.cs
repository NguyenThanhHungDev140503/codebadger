namespace Application.Features.CentralDbSync.Models;

public sealed record ChangeBatch(
    long PreviousCheckpoint,
    long UpperWatermark,
    IReadOnlyList<GenericChangeRow> Rows);
