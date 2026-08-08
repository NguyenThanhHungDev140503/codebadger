using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.System;

public sealed class GlobalConfigsToConfigMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "GlobalConfigs-to-config",
            sourceTable: "GlobalConfigs",
            sourcePrimaryKey: "GlobalConfigId",
            targetTable: "config",
            targetPrimaryKey: "config_id",
            ownershipScope: "erp:global-config",
            columns:
            [
                new ColumnMapping
                {
                    TargetColumn = "config_id",
                    TargetType = "integer",
                    SourceExpression = "0",
                    IsPrimaryKey = true
                },
                new ColumnMapping
                {
                    TargetColumn = "company_id",
                    TargetType = "integer",
                    SourceExpression = "0"
                },
                new ColumnMapping
                {
                    TargetColumn = "key_config",
                    TargetType = "text",
                    SourceExpression = "'VND_CurrencyId'"
                },
                new ColumnMapping
                {
                    TargetColumn = "value",
                    TargetType = "text",
                    SourceExpression = "CAST([t0].[VND_CurrencyId] AS nvarchar(100))"
                }
            ])
    ];
}
