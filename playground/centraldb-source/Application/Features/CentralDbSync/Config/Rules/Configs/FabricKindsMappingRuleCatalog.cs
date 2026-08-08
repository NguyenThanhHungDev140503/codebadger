using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class FabricKindsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Fabrics.Kinds",
            sourceTable: "ERP.Configs.Fabrics.Kinds",
            sourcePrimaryKey: "KindOfFabricId",
            targetTable: "fabric_kinds",
            targetPrimaryKey: "fabric_kind_id",
            filterCompany: true,
            columns:
            [
                MapPk("fabric_kind_id", "integer", "t0.KindOfFabricId"),
                Map("name", "text", "t0.Name"),
                Map("is_active", "boolean", "t0.IsActivate"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
