using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class TrimKindsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Trims.Kinds",
            sourceTable: "ERP.Configs.Trims.Kinds",
            sourcePrimaryKey: "KindOfTrimId",
            targetTable: "trim_kinds",
            targetPrimaryKey: "trim_kind_id",
            filterCompany: true,
            columns:
            [
                MapPk("trim_kind_id", "integer", "t0.KindOfTrimId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("is_active", "boolean", "t0.IsActivate"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
