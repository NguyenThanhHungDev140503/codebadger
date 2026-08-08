using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class TrimGroupsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.TrimGroups",
            sourceTable: "ERP.Configs.TrimGroups",
            sourcePrimaryKey: "TrimGroupId",
            targetTable: "trim_groups",
            targetPrimaryKey: "trim_group_id",
            filterCompany: true,
            columns:
            [
                MapPk("trim_group_id", "integer", "t0.TrimGroupId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("display_order", "integer", "t0.DisplayOrder"),
                Map("is_active", "boolean", "t0.Activated"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
