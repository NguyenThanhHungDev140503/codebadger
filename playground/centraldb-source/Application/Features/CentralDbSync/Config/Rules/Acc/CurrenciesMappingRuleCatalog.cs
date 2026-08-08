using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Acc;

public sealed class CurrenciesMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "Acc.Currencies",
            sourceTable: "Acc.Currencies",
            sourcePrimaryKey: "CurrencyId",
            targetTable: "currencies",
            targetPrimaryKey: "currency_id",
            columns:
            [
                MapPk("currency_id", "integer", "t0.CurrencyId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("description", "text", "t0.Description"),
                Map("symbol", "text", "t0.Symbol"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
