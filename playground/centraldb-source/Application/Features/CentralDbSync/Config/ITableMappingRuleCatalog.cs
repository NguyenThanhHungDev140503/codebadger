using Application.Features.CentralDbSync.Mapping;

namespace Application.Features.CentralDbSync.Config;

public interface ITableMappingRuleCatalog
{
    IReadOnlyList<TableMappingRule> GetRules();
}
