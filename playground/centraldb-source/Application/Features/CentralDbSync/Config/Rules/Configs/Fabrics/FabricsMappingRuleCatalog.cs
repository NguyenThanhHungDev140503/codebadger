using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Fabrics;

public sealed class FabricsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Fabrics",
            sourceTable: "ERP.Configs.Fabrics",
            sourcePrimaryKey: "FabricId",
            targetTable: "fabrics",
            targetPrimaryKey: "fabrics_id",
            filterCompany: true,
            columns:
            [
                MapPk("fabrics_id", "integer", "t0.FabricId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("units_id", "integer", "t0.UnitId"),
                Map("fabric_kinds_id", "integer", "t0.KindOfFabricId"),
                Map("fabric_weave_structures_id", "integer", "t0.FabricStructureId"),
                Map("fabric_weave_parameters_id", "integer", "t0.WeaveParameterId"),
                Map("fabric_yarns_id", "integer", "t0.FabricYarnId"),
                Map("fabric_yarn_compositions_id", "integer", "t0.FabricCompositionId"),
                Map("fabric_colors_id", "integer", "t0.FabricColorId"),
                Map("weight", "integer", "t0.Weight"),
                Map("remark", "text", "t0.Remark"),
                Map("is_activate", "boolean", "t0.IsActivate"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("version", "integer", "t0.Version"),
                Map("hand_feel_id", "integer", "t0.HandFeelId")
            ])
    ];
}

