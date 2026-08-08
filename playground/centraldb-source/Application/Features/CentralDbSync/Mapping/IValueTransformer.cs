namespace Application.Features.CentralDbSync.Mapping;

public interface IValueTransformer
{
    string Name { get; }
    object? Transform(IReadOnlyDictionary<string, object?> sourceRow);
}
