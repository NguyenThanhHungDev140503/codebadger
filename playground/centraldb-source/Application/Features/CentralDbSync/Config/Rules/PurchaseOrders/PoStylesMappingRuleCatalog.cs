using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.PurchaseOrders;

public sealed class PoStylesMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.PurchaseOrders.Styles",
            sourceTable: "ERP.PurchaseOrders.Styles",
            sourcePrimaryKey: "StyleId",
            targetTable: "po_styles",
            targetPrimaryKey: "po_styles_id",
            syncTier: "Hot",
            filterCompany: true,
            columns:
            [
                MapPk("po_styles_id", "integer", "t0.StyleId"),
                Map("po_id", "integer", "t0.PurchaseOrderId"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("supplier_id", "integer", "t0.PartnerId"),
                Map("sku_no", "text", "t0.SkuNo"),
                Map("sku_no_internal", "text", "t0.SkuNoInternal"),
                Map("style_name", "text", "t0.StyleName"),
                Map("style_no_internal", "text", "t0.StyleNoInternal"),
                Map("colours_id", "integer", "t0.ColorId"),
                Map("size_type_id", "integer", "t0.SizeTypeId"),
                Map("stt", "integer", "t0.Stt"),
                Map("total", "numeric", "t0.Total"),
                Map("random_id", "integer", "t0.RandomId"),
                Map("version", "integer", "t0.Version"),
                Map("avatars", "text", "t0.Avatars"),
                Map("style_categories_id", "integer", "t0.StyleCategoryId"),
                Map("process_type_id", "integer", "t0.ProcessTypeId"),
                Map("target_price", "numeric", "t0.TargetPrice"),
                Map("ship_date", "timestamp", "t0.ShipDate"),
                Map("original_id", "integer", "t0.OriginalId"),
                Map("detail_information", "text", "t0.DetailInformation"),
                Map("style_no_internal_old", "text", "t0.StyleNoInternalOld"),
                Map("style_ref_id", "integer", "t0.StyleRefId"),
                Map("shipment_date_max", "timestamp", "t0.ShipmentDateMax"),
                Map("shipment_internal_date_max", "timestamp", "t0.ShipmentInternalDateMax"),
                Map("balance_trim_state", "integer", "t0.BalanceTrimState"),
                Map("order_code", "text", "t0.OrderCode"),
                Map("cfc", "text", "t0.CFC"),
                Map("seasons_id", "integer", "t0.SeasonId"),
                Map("drops_id", "integer", "t0.DropId"),
                Map("count_request", "integer", "t0.CountRequest"),
                Map("work_type", "integer", "t0.WorkType"),
                Map("approved_user_id", "integer", "t0.ApprovedUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("done_trim_po", "boolean", "t0.DoneTrimPO"),
                Map("done_fabric_po", "boolean", "t0.DoneFabricPO"),
                Map("total_extra", "numeric", "t0.TotalExtra"),
                Map("technical_received", "boolean", "t0.TechnicalReceived"),
                Map("technical_received_pom", "boolean", "t0.TechnicalReceivedPOM"),
                Map("sent_mockup_treatment_summary_date", "timestamp", "t0.SentMockupTreatmentSummaryDate"),
                Map("sent_mockup_treatment_summary", "boolean", "t0.SentMockupTreatmentSummary"),
                Map("enable_sent_mockup_treatment_summary", "boolean", "t0.EnableSentMockupTreatmentSummary"),
                Map("reuse_state", "integer", "t0.ReuseState"),
                Map("reuse_error", "text", "t0.ReuseError"),
                Map("amount", "numeric", "t0.Amount"),
                Map("mark_as_cmp_process", "boolean", "t0.MarkAsCmpProcess"),
                Map("mark_as_rfid_process", "boolean", "t0.MarkAsRfidProcess"),
                Map("cutting_docket_id", "integer", "t0.CuttingDocketId"),
                Map("process_default", "text", "t0.ProcessDefault"),
                Map("process_custom", "text", "t0.ProcessCustom"),
                Map("process_custom_name", "text", "t0.ProcessCustomName"),
                Map("process_updated_by_user_id", "integer", "t0.ProcessUpdatedByUserId"),
                Map("process_updated_date", "timestamp", "t0.ProcessUpdatedDate"),
                Map("style_ratio_id", "integer", "t0.StyleRatioId"),
                Map("total_actual_cd_qty", "numeric", "t0.TotalActualCDQty"),
                Map("total_style_qty", "numeric", "t0.TotalStyleQty"),
                Map("reuse_reuse_style_no_internal", "boolean", "t0.Reuse_ReuseStyleNoInternal"),
                Map("reuse_lock_for_repeat_style_no", "boolean", "t0.Reuse_LockForRepeatStyleNo"),
                Map("reuse_style_fabric_ids", "text", "t0.Reuse_StyleFabricIds"),
                Map("reuse_style_trim_ids", "text", "t0.Reuse_StyleTrimIds"),
                Map("reuse_cmp_ids", "text", "t0.Reuse_CmpIds"),
                Map("reuse_pom_ids", "text", "t0.Reuse_PomIds"),
                Map("reuse_mockup_treatment_summary_ids", "text", "t0.Reuse_MockupTreatmentSummaryIds"),
                Map("reuse_costing_sheet_id", "integer", "t0.Reuse_CostingSheetId"),
                Map("reuse_copy_request", "boolean", "t0.Reuse_CopyRequest"),
                Map("reuse_reuse_all", "boolean", "t0.Reuse_ReuseAll"),
                Map("reuse_style_fabric_ids_labdip", "text", "t0.Reuse_StyleFabricIds_Labdip"),
                Map("reuse_style_trim_ids_labdip", "text", "t0.Reuse_StyleTrimIds_Labdip"),
                Map("reuse_for_style_no_internal", "text", "t0.Reuse_ForStyleNoInternal"),
                Map("remove_lock_date", "timestamp", "t0.RemoveLockDate"),
                Map("remove_lock_by_user_id", "integer", "t0.RemoveLockByUserId")
            ])
            with { UseScalableBootstrap = true }
    ];
}

