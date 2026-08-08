using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class ColoursMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Colors",
            sourceTable: "ERP.Configs.Colors",
            sourcePrimaryKey: "ColorId",
            targetTable: "colours",
            targetPrimaryKey: "colour_id",
            filterCompany: true,
            columns:
            [
                MapPk("colour_id", "integer", "t0.ColorId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("color_rgb", "text", "t0.ColorRGB"),
                Map("name_code", "text", "t0.NameCode"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
