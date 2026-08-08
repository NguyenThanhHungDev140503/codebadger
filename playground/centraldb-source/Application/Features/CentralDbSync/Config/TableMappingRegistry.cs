using Application.Features.CentralDbSync.Mapping;

namespace Application.Features.CentralDbSync.Config;

public sealed class TableMappingRegistry : IMappingRuleProvider
{
    private readonly IReadOnlyList<TableMappingRule> _rules;
    private readonly Dictionary<string, TableMappingRule> _rulesByName;

    public TableMappingRegistry(
        IEnumerable<ITableMappingRuleCatalog> catalogs,
        MappingRuleValidator validator)
    {
        _rules = catalogs.SelectMany(catalog => catalog.GetRules()).ToArray();
        validator.ValidateAll(_rules);
        EnsureUniqueRuleNames(_rules);
        _rulesByName = _rules.ToDictionary(rule => rule.RuleName, StringComparer.OrdinalIgnoreCase);
    }

    public TableMappingRule Get(string ruleName)
        => TryGet(ruleName, out var rule)
            ? rule
            : throw new KeyNotFoundException($"Central DB sync mapping rule '{ruleName}' is not registered.");

    public IReadOnlyList<TableMappingRule> GetAll() => _rules;

    public bool TryGet(string ruleName, out TableMappingRule rule)
        => _rulesByName.TryGetValue(ruleName, out rule!);

    private static void EnsureUniqueRuleNames(IEnumerable<TableMappingRule> rules)
    {
        var duplicates = rules
            .GroupBy(rule => rule.RuleName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length != 0)
        {
            throw new InvalidOperationException(
                "Duplicate central DB sync mapping rules: " + string.Join(", ", duplicates) + ".");
        }
    }
}
