using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Fabrics;

public sealed class FabricYarnsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Fabrics.Yarns",
            sourceTable: "ERP.Configs.Fabrics.Yarns",
            sourcePrimaryKey: "FabricYarnId",
            targetTable: "fabric_yarns",
            targetPrimaryKey: "fabric_yarns_id",
            filterCompany: true,
            columns:
            [
                MapPk("fabric_yarns_id", "integer", "t0.FabricYarnId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

