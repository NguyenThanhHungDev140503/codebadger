using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Fabrics;

public sealed class FabricColorsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Fabrics.Colors",
            sourceTable: "ERP.Configs.Fabrics.Colors",
            sourcePrimaryKey: "FabricColorId",
            targetTable: "fabric_colors",
            targetPrimaryKey: "fabric_colors_id",
            filterCompany: true,
            columns:
            [
                MapPk("fabric_colors_id", "integer", "t0.FabricColorId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("color_rgb", "text", "t0.ColorRGB"),
                Map("name_code", "text", "t0.NameCode"),
                Map("is_use_for_gen_greige", "boolean", "t0.IsUseForGenGreige"),
                Map("is_use_for_gen_yarn", "boolean", "t0.IsUseForGenYarn"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("version", "integer", "t0.Version")
            ])
    ];
}

