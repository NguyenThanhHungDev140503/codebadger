using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Fabrics;

public sealed class FabricWeaveStructuresMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Fabrics.WeaveStructures",
            sourceTable: "ERP.Configs.Fabrics.WeaveStructures",
            sourcePrimaryKey: "FabricStructureId",
            targetTable: "fabric_weave_structures",
            targetPrimaryKey: "fabric_weave_structures_id",
            filterCompany: true,
            columns:
            [
                MapPk("fabric_weave_structures_id", "integer", "t0.FabricStructureId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("is_activate", "boolean", "t0.IsActivate"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("is_exclude_wip_daily", "boolean", "t0.IsExcludeWipDaily"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

