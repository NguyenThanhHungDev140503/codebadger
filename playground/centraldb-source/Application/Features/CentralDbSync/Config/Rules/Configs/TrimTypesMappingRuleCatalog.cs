using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class TrimTypesMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Trims.Types",
            sourceTable: "ERP.Configs.Trims.Types",
            sourcePrimaryKey: "TypeOfTrimId",
            targetTable: "trim_types",
            targetPrimaryKey: "trim_type_id",
            filterCompany: true,
            columns:
            [
                MapPk("trim_type_id", "integer", "t0.TypeOfTrimId"),
                Map("trim_kind_id", "integer", "t0.KindOfTrimId"),
                Map("code", "text", "t0.Code"),
                Map("name", "text", "t0.Name"),
                Map("is_active", "boolean", "t0.IsActivate"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("is_thread", "boolean", "t0.IsThread"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
