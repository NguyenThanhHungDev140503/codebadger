namespace Application.Features.CentralDbSync.Mapping;

public interface IMappingRuleProvider
{
    TableMappingRule Get(string ruleName);
    IReadOnlyList<TableMappingRule> GetAll();
    bool TryGet(string ruleName, out TableMappingRule rule);
}
