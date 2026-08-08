using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Acc;

public sealed class ExchangeRatesMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "Acc.ExchangeRates",
            sourceTable: "Acc.ExchangeRates",
            sourcePrimaryKey: "ExchangeRateId",
            targetTable: "exchange_rates",
            targetPrimaryKey: "exchange_rate_id",
            filterCompany: true,
            columns:
            [
                MapPk("exchange_rate_id", "integer", "t0.ExchangeRateId"),
                Map("currency_id", "integer", "t0.CurrencyId"),
                Map("rate", "numeric", "t0.Rate"),
                Map("from_date", "timestamp", "t0.FromDate"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
