using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.PurchaseOrders;

public sealed class PoStyleFabricsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.PurchaseOrders.Styles.Fabrics",
            sourceTable: "ERP.PurchaseOrders.Styles.Fabrics",
            sourcePrimaryKey: "StyleFabricId",
            targetTable: "po_style_fabrics",
            targetPrimaryKey: "po_style_fabrics_id",
            syncTier: "Hot",
            filterCompany: true,
            columns:
            [
                MapPk("po_style_fabrics_id", "integer", "t0.StyleFabricId"),
                Map("name", "text", "t0.Name"),
                Map("fabric_types_id", "integer", "t0.FabricTypeId"),
                Map("fabric_nomination_id", "integer", "t0.FabricNominationId"),
                Map("po_styles_id", "integer", "t0.StyleId"),
                Map("po_id", "integer", "t0.PurchaseOrderId"),
                Map("style_random_id", "integer", "t0.StyleRandomId"),
                Map("has_treatment_strike_off", "boolean", "t0.HasTreatmentStrikeOff"),
                Map("style_fabric_lab_dip_id", "integer", "t0.StyleFabricLabDipId"),
                Map("pur_status", "integer", "t0.PurStatus"),
                Map("purchasing_state", "integer", "t0.PurchasingState"),
                Map("color_ext", "text", "t0.ColorExt"),
                Map("pantone", "text", "t0.Pantone"),
                Map("has_substitute", "boolean", "t0.HasSubstitute"),
                Map("approved_swatch", "boolean", "t0.ApprovedSwatch"),
                Map("approved_lab_dip", "boolean", "t0.ApprovedLabDip"),
                Map("has_customer_cancelled", "boolean", "t0.HasCustomerCancelled"),
                Map("has_pur_rejected", "boolean", "t0.HasPurRejected"),
                Map("fabric_master_id", "integer", "t0.FabricSupplierId"),
                Map("fabrics_supplier_request_id", "integer", "t0.FabricSupplierRequestId"),
                Map("cut_width", "numeric", "t0.CutWidth"),
                Map("item_code", "text", "t0.ItemCode"),
                Map("fabrics_id", "integer", "t0.FabricId"),
                Map("rating", "numeric", "t0.Rating"),
                Map("eff", "numeric", "t0.Eff"),
                Map("cut_width_ext", "text", "t0.CutWidthExt"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("supplier_id", "integer", "t0.PartnerId"),
                Map("swatch_approved_date", "timestamp", "t0.SwatchApprovedDate"),
                Map("lab_dip_approved_date", "timestamp", "t0.LabDipApprovedDate"),
                Map("must_developed", "boolean", "t0.MustDeveloped"),
                Map("must_lab_diped", "boolean", "t0.MustLabDiped"),
                Map("shadeband_id", "integer", "t0.ShadebandId"),
                Map("fabric_color_option", "integer", "t0.FabricColorOption"),
                Map("request_type", "integer", "t0.RequestType"),
                Map("pur_received", "boolean", "t0.PurReceived"),
                Map("pur_received_lab_dip", "boolean", "t0.PurReceivedLabDip"),
                Map("pantone_of_supplier", "text", "t0.PantoneOfSupplier"),
                Map("shade_band_name", "text", "t0.ShadeBandName"),
                Map("total_of_swatch", "integer", "t0.TotalOfSwatch"),
                Map("total_of_develop", "integer", "t0.TotalOfDevelop"),
                Map("total_of_lab_dip", "integer", "t0.TotalOfLabDip"),
                Map("sent_to_bom", "boolean", "t0.SentToBOM"),
                Map("approved_rating", "boolean", "t0.ApprovedRating"),
                Map("allow_resent_rating", "boolean", "t0.AllowResentRating"),
                Map("style_technical_rating_id", "integer", "t0.StyleTechnicalRatingId"),
                Map("version", "integer", "t0.Version"),
                Map("random_id", "integer", "t0.RandomId"),
                Map("stt", "integer", "t0.Stt"),
                Map("original_id", "integer", "t0.OriginalId"),
                Map("ref_style_id", "integer", "t0.Ref_StyleId"),
                Map("ref_style_fabric_id", "integer", "t0.Ref_StyleFabricId"),
                Map("ref_fabric_type_id", "integer", "t0.Ref_FabricTypeId"),
                Map("ref_style_fabric_lab_dip_id", "integer", "t0.Ref_StyleFabricLabDipId"),
                Map("ref_ld_code", "text", "t0.Ref_LDCode"),
                Map("ref_ld_shadeband", "text", "t0.Ref_LDShadeband"),
                Map("deactived", "boolean", "t0.Deactived")
            ])
            with { UseScalableBootstrap = true }
    ];
}

