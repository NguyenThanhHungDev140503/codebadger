using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.PurchaseOrders;

public sealed class PoStylesTrimsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.PurchaseOrders.Styles.Trims",
            sourceTable: "ERP.PurchaseOrders.Styles.Trims",
            sourcePrimaryKey: "StyleTrimId",
            targetTable: "po_styles_trims",
            targetPrimaryKey: "po_styles_trims_id",
            syncTier: "Hot",
            columns:
            [
                MapPk("po_styles_trims_id", "integer", "t0.StyleTrimId"),
                Map("po_styles_id", "integer", "t0.StyleId"),
                Map("po_id", "integer", "t0.PurchaseOrderId"),
                Map("trim_group_id", "integer", "t0.TrimGroupId"),
                Map("name", "text", "t0.Name"),
                Map("dimension_ext", "text", "t0.DimensionExt"),
                Map("color_ext", "text", "t0.ColorExt"),
                Map("pantone_ext", "text", "t0.PantoneExt"),
                Map("fabric_color_ext", "text", "t0.FabricColorExt"),
                Map("nomination_id", "integer", "t0.NominationId"),
                Map("trims_master_id", "integer", "t0.TrimSupplierId"),
                Map("trim_supplier_request_id", "integer", "t0.TrimSupplierRequestId"),
                Map("supplier_id", "integer", "t0.PartnerId"),
                Map("trims_id", "integer", "t0.TrimId"),
                Map("style_reference_id", "integer", "t0.StyleReferenceId"),
                Map("has_substitute", "boolean", "t0.HasSubstitute"),
                Map("approved_swatch", "boolean", "t0.ApprovedSwatch"),
                Map("approved_lab_dip", "boolean", "t0.ApprovedLabDip"),
                Map("dye_to_matched", "boolean", "t0.DyeToMatched"),
                Map("fabric_kinds_id", "integer", "t0.KindOfFabricId"),
                Map("fabric_shadeband_id", "integer", "t0.FabricShadebandId"),
                Map("pantone_of_fabric", "text", "t0.PantoneOfFabric"),
                Map("shade_band_name", "text", "t0.ShadeBandName"),
                Map("swatch_approved_date", "timestamp", "t0.SwatchApprovedDate"),
                Map("lab_dip_approved_date", "timestamp", "t0.LabDipApprovedDate"),
                Map("original_id", "integer", "t0.OriginalId"),
                Map("state", "integer", "t0.State"),
                Map("total_of_swatch", "integer", "t0.TotalOfSwatch"),
                Map("total_of_develop", "integer", "t0.TotalOfDevelop"),
                Map("total_of_lab_dip", "integer", "t0.TotalOfLabDip"),
                Map("pur_received", "boolean", "t0.PurReceived"),
                Map("pur_received_lab_dip", "boolean", "t0.PurReceivedLabDip"),
                Map("must_labdiped", "boolean", "t0.MustLabdiped"),
                Map("remark", "text", "t0.Remark"),
                Map("style_random_id", "integer", "t0.StyleRandomId"),
                // Map("version", "integer", "t0.Version"),
                Map("must_developed", "boolean", "t0.MustDeveloped"),
                Map("random_id", "integer", "t0.RandomId"),
                Map("fabric_types_id", "integer", "t0.FabricTypeId"),
                Map("ref_style_fabric_lab_dip_id", "integer", "t0.Ref_StyleFabricLabDipId"),
                Map("mark_as_logo", "boolean", "t0.MarkAsLogo"),
                Map("has_customer_cancelled", "boolean", "t0.HasCustomerCancelled"),
                Map("sent_to_bom", "boolean", "t0.SentToBOM"),
                Map("approved_rating", "boolean", "t0.ApprovedRating"),
                Map("dimension", "text", "t0.Dimension"),
                Map("po_trim_rating_master_id", "integer", "t0.TrimTechnicalRatingId"),
                Map("allow_resent_rating", "boolean", "t0.AllowResentRating"),
                Map("activate_file_layout", "boolean", "t0.ActivateFileLayout"),
                Map("style_trim_file_layout_id", "integer", "t0.StyleTrimFileLayoutId"),
                Map("mounting_specification", "text", "t0.MountingSpecification"),
                Map("item_code", "text", "t0.ItemCode"),
                Map("pur_status", "integer", "t0.PurStatus"),
                Map("deactived", "boolean", "t0.Deactived"),
                Map("stt", "integer", "t0.Stt")
            ])
            with { UseScalableBootstrap = true }
    ];
}

