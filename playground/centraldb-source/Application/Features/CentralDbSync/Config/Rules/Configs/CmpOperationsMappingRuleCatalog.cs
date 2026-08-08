using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class CmpOperationsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.CMP.Operations",
            sourceTable: "ERP.Configs.CMP.Operations",
            sourcePrimaryKey: "CmpOperationId",
            targetTable: "cmp_operations",
            targetPrimaryKey: "cmp_operation_id",
            filterCompany: true,
            columns:
            [
                MapPk("cmp_operation_id", "integer", "t0.CmpOperationId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("is_active", "boolean", "t0.Activated"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("icon", "text", "t0.Icon"),
                Map("can_edit_timing", "boolean", "t0.CanEditTiming"),
                Map("display_order", "integer", "t0.DisplayOrder"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
