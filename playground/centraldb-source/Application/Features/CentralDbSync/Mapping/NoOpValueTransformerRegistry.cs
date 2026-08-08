namespace Application.Features.CentralDbSync.Mapping;

public sealed class NoOpValueTransformerRegistry : IValueTransformerRegistry
{
    public IValueTransformer Resolve(string name)
        => throw new InvalidOperationException($"Value transformer '{name}' is not registered.");
}
