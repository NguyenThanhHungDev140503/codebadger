using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Fabrics;

public sealed class FabricYarnCompositionsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Fabrics.YarnCompositions",
            sourceTable: "ERP.Configs.Fabrics.YarnCompositions",
            sourcePrimaryKey: "FabricCompositionId",
            targetTable: "fabric_yarn_compositions",
            targetPrimaryKey: "fabric_yarn_compositions_id",
            filterCompany: true,
            columns:
            [
                MapPk("fabric_yarn_compositions_id", "integer", "t0.FabricCompositionId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("is_activate", "boolean", "t0.IsActivate"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

