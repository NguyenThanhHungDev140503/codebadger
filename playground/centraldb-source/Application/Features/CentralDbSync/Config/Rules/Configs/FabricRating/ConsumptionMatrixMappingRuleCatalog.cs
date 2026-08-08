using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.FabricRating;

public sealed class ConsumptionMatrixMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.FabricRatingDetail",
            sourceTable: "ERP.Configs.FabricRatingDetail",
            sourcePrimaryKey: "FabricRatingDetailId",
            targetTable: "consumption_matrix",
            targetPrimaryKey: "consumption_matrix_id",
            columns:
            [
                MapPk("consumption_matrix_id", "integer", "t0.FabricRatingDetailId"),
                Map("fabric_consumption_id", "integer", "t0.FabricRatingId"),
                Map("group_fabric_type_id", "integer", "t0.GroupFabricTypeId"),
                Map("fabric_color_option", "integer", "t0.FabricColorOption"),
                Map("fabric_rating", "numeric", "t0.FabricRating"),
                Map("mark_cut_width_bigger_standard", "boolean", "t0.MarkCutWidthBiggerStandard"),
                Map("mark_cut_width_smaller_standard", "boolean", "t0.MarkCutWidthSmallerStandard")
            ])
    ];
}
