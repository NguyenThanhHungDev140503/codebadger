using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.FabricRating;

public sealed class FabricConsumptionMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.FabricRating",
            sourceTable: "ERP.Configs.FabricRating",
            sourcePrimaryKey: "FabricRatingId",
            targetTable: "fabric_consumption",
            targetPrimaryKey: "fabric_consumption_id",
            filterCompany: true,
            columns:
            [
                MapPk("fabric_consumption_id", "integer", "t0.FabricRatingId"),
                Map("style_categories_id", "integer", "t0.StyleCategoryId"),
                Map("code", "text", "t0.Code"),
                Map("name", "text", "t0.Name"),
                Map("standard_cutting_width", "numeric", "t0.StandardCuttingWidth"),
                Map("standard_qty", "integer", "t0.StandardQty"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

