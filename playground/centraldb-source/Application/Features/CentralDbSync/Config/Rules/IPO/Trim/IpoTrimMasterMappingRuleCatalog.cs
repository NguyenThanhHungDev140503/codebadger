using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.IPO.Trim;

public sealed class IpoTrimMasterMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.IPO.Trims.Masters",
            sourceTable: "ERP.IPO.Trims.Masters",
            sourcePrimaryKey: "IPOTrimMasterId",
            targetTable: "ipo_trims_masters",
            targetPrimaryKey: "ipo_trim_master_id",
            syncTier: "Hot",
            filterCompany: true,
            columns:
            [
                MapPk("ipo_trim_master_id", "integer", "t0.IPOTrimMasterId"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("work_type", "integer", "t0.WorkType"),
                Map("ipo_trim_master_code", "text", "t0.IPOTrimMasterCode"),
                Map("ipo_trim_item_code_combined", "text", "t0.IPOTrimItemCodeCombined"),
                Map("supplier_id", "integer", "t0.SupplierPartnerId"),
                Map("currencies_id", "integer", "t0.CurrencyId"),
                Map("supplier_partner_ids", "text", "t0.SupplierPartnerIds"),
                Map("customer_id", "integer", "t0.CustomerPartnerId"),
                Map("trim_types_id", "integer", "t0.TypeOfTrimId"),
                Map("trim_kinds_id", "integer", "t0.KindOfTrimId"),
                Map("trims_compositions_id", "integer", "t0.TrimCompositionId"),
                Map("ipo_trim_master_status", "integer", "t0.IPOTrimMasterStatus"),
                Map("combined_date", "timestamp", "t0.CombinedDate"),
                Map("combined_by_user_id", "integer", "t0.CombinedByUserId"),
                Map("approved_date", "timestamp", "t0.ApprovedDate"),
                Map("approved_by_user_id", "integer", "t0.ApprovedByUserId"),
                Map("rejected_date", "timestamp", "t0.RejectedDate"),
                Map("rejected_by_user_id", "integer", "t0.RejectedByUserId"),
                Map("rejected_ipo_reason", "text", "t0.RejectedIPOReason"),
                Map("remove_combine_reason", "text", "t0.RemoveCombineReason"),
                Map("remove_combined_date", "timestamp", "t0.RemoveCombinedDate"),
                Map("remove_combined_by_user_id", "integer", "t0.RemoveCombinedByUserId"),
                Map("acepted_date", "timestamp", "t0.AceptedDate"),
                Map("acepted_by_user_id", "integer", "t0.AceptedByUserId"),
                Map("version", "integer", "t0.Version"),
                Map("clone_ok", "boolean", "t0.CloneOk"),
                Map("ipo_trim_master_id_parent", "integer", "t0.IPOTrimMasterIdParent"),
                Map("eta_trim_receiving_date", "timestamp", "t0.ETATrimReceivingDate"),
                Map("remark", "text", "t0.Remark"),
                Map("total_order_qty", "numeric", "t0.TotalOrderQty"),
                Map("actual_total_qty", "numeric", "t0.ActualTotalQty"),
                Map("total_surcharge", "numeric", "t0.TotalSurcharge"),
                Map("actual_total_amount", "numeric", "t0.ActualTotalAmount"),
                Map("total_pre_arrival", "numeric", "t0.TotalPreArrival"),
                Map("total_buy_qty", "numeric", "t0.TotalBuyQty"),
                Map("sent_purch_manager_by_user_id", "integer", "t0.SentPurchManagerByUserId"),
                Map("sent_purch_manager_date", "timestamp", "t0.SentPurchManagerDate"),
                Map("po_id", "integer", "t0.PurchaseOrderId"),
                Map("purchase_order_created_date", "timestamp", "t0.PurchaseOrderCreatedDate"),
                Map("season_ids", "text", "t0.SeasonIds"),
                Map("season_codes", "text", "t0.SeasonCodes"),
                Map("drop_codes", "text", "t0.DropCodes"),
                Map("drop_ids", "text", "t0.DropIds"),
                Map("sent_pur_by_user_ids", "text", "t0.SentPurByUserIds"),
                Map("sent_pur_by_user_names", "text", "t0.SentPurByUserNames"),
                Map("process_type_ids", "text", "t0.ProcessTypeIds"),
                Map("process_type_names", "text", "t0.ProcessTypeNames"),
                Map("commercial_reject_reason", "text", "t0.CommercialRejectReason"),
                Map("commercial_reject_date", "timestamp", "t0.CommercialRejectDate"),
                Map("commercial_reject_by_user_id", "integer", "t0.CommercialRejectByUserId"),
                Map("commercial_approved_date", "timestamp", "t0.CommercialApprovedDate"),
                Map("commercial_approved_by_user_id", "integer", "t0.CommercialApprovedByUserId"),
                Map("nominated_ids", "text", "t0.NominatedIds"),
                Map("nominated_names", "text", "t0.NominatedNames"),
                Map("po_type", "integer", "t0.POType"),
                Map("supplier_order_state", "integer", "t0.SupplierOrderState"),
                Map("supplier_order_success_date", "timestamp", "t0.SupplierOrderSuccessDate"),
                Map("payment_date", "timestamp", "t0.PaymentDate"),
                Map("paid_percent", "numeric", "t0.PaidPercent"),
                Map("total_amount_before_vat", "numeric", "t0.TotalAmountBeforeVAT"),
                Map("job_numbers", "text", "t0.JobNumbers"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
            with { UseScalableBootstrap = true }
    ];
}
