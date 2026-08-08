namespace Application.Features.CentralDbSync.Mapping;

public interface IValueTransformerRegistry
{
    IValueTransformer Resolve(string name);
}
