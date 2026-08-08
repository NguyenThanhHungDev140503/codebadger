using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class UnitConversionsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.UnitConversions",
            sourceTable: "ERP.Configs.UnitConversions",
            sourcePrimaryKey: "UnitConversionId",
            targetTable: "unit_conversions",
            targetPrimaryKey: "unit_conversion_id",
            filterCompany: true,
            columns:
            [
                MapPk("unit_conversion_id", "integer", "t0.UnitConversionId"),
                Map("from_unit_id", "integer", "t0.FromUnitId"),
                Map("from_rate", "numeric", "t0.FromRate"),
                Map("to_unit_id", "integer", "t0.ToUnitId"),
                Map("to_rate", "numeric", "t0.ToRate"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
