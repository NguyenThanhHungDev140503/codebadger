namespace Application.Features.CentralDbSync.Models;

public sealed record GenericSourceRow(IReadOnlyDictionary<string, object?> Values)
{
    public object? GetValueOrDefault(string key)
    {
        if (Values.TryGetValue(key, out var value))
            return value;

        var unqualifiedKey = key.Contains('.')
            ? key[(key.LastIndexOf('.') + 1)..]
            : key;

        return Values.TryGetValue(unqualifiedKey, out value) ? value : null;
    }
}
