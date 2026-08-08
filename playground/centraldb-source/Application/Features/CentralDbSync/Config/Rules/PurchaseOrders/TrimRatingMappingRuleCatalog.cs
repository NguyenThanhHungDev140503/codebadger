using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.PurchaseOrders;

public sealed class TrimRatingMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.PurchaseOrders.Trims.Technicals.Rating.Details",
            sourceTable: "ERP.PurchaseOrders.Trims.Technicals.Rating.Details",
            sourcePrimaryKey: "TrimTechnicalRatingDetailId",
            targetTable: "trim_rating",
            targetPrimaryKey: "trim_rating_id",
            syncTier: "Hot",
            columns:
            [
                MapPk("trim_rating_id", "integer", "t0.TrimTechnicalRatingDetailId"),
                Map("trim_technical_id", "integer", "t0.TrimTechnicalId"),
                Map("po_trim_rating_master_id", "integer", "t0.TrimTechnicalRatingId"),
                Map("rating_value", "numeric", "t0.RatingValue"),
                Map("size_id", "integer", "t0.SizeId"),
                Map("original_id", "integer", "t0.OriginalId"),
                Map("remark_by_technical", "text", "t0.RemarkByTechnical"),
                Map("remark_by_mer", "text", "t0.RemarkByMer"),
                Map("comsumption", "numeric", "t0.Comsumption"),
                Map("units_id", "integer", "t0.UnitId"),
                Map("dimension_by_technical", "text", "t0.DimensionByTechnical"),
                Map("dimension_value_by_technical", "numeric", "t0.DimensionValueByTechnical")
            ])
            with { UseScalableBootstrap = true }
    ];
}
