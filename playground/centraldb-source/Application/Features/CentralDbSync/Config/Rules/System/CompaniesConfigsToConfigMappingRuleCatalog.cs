using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.System;

public sealed class CompaniesConfigsToConfigMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "Companies.Configs-to-config",
            sourceTable: "Companies.Configs",
            sourcePrimaryKey: "CompanyConfigId",
            targetTable: "config",
            targetPrimaryKey: "config_id",
            filterCompany: true,
            columns:
            [
                MapPk("config_id", "integer", "t0.CompanyConfigId"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("key_config", "text", "t0.KeyConfig"),
                Map("value", "text", "t0.Value")
            ])
    ];
}

