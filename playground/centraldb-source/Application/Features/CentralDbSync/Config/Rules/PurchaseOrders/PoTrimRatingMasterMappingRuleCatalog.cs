using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.PurchaseOrders;

public sealed class PoTrimRatingMasterMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.PurchaseOrders.Trims.Technicals.Rating",
            sourceTable: "ERP.PurchaseOrders.Trims.Technicals.Rating",
            sourcePrimaryKey: "TrimTechnicalRatingId",
            targetTable: "po_trim_rating_master",
            targetPrimaryKey: "po_trim_rating_master_id",
            syncTier: "Hot",
            filterCompany: true,
            columns:
            [
                MapPk("po_trim_rating_master_id", "integer", "t0.TrimTechnicalRatingId"),
                Map("trim_technical_id", "integer", "t0.TrimTechnicalId"),
                Map("original_id", "integer", "t0.OriginalId"),
                Map("po_styles_trims_id", "integer", "t0.StyleTrimId"),
                Map("po_styles_id", "integer", "t0.StyleId"),
                Map("po_id", "integer", "t0.PurchaseOrderId"),
                Map("trim_group_id", "integer", "t0.TrimGroupId"),
                Map("trims_id", "integer", "t0.TrimId"),
                Map("trims_master_id", "integer", "t0.TrimSupplierId"),
                Map("supplier_id", "integer", "t0.PartnerId"),
                Map("temporary", "boolean", "t0.Temporary"),
                Map("sent_again", "boolean", "t0.SentAgain"),
                Map("version", "integer", "t0.Version"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("flow_by_size", "boolean", "t0.FlowBySize"),
                Map("trim_technical_rating_random_id", "integer", "t0.TrimTechnicalRatingRandomId")
            ])
            with { UseScalableBootstrap = true }
    ];
}

