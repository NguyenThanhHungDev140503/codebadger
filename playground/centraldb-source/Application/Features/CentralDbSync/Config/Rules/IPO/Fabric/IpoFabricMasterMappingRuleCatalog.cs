using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.IPO.Fabric;

public sealed class IpoFabricMasterMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.IPO.Fabrics.Masters",
            sourceTable: "ERP.IPO.Fabrics.Masters",
            sourcePrimaryKey: "IPOFabricMasterId",
            targetTable: "ipo_fabrics_masters",
            targetPrimaryKey: "ipo_fabric_master_id",
            syncTier: "Hot",
            filterCompany: true,
            columns:
            [
                MapPk("ipo_fabric_master_id", "integer", "t0.IPOFabricMasterId"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("work_type", "integer", "t0.WorkType"),
                Map("ipo_fabric_master_code", "text", "t0.IPOFabricMasterCode"),
                Map("ipo_fabric_item_code_combined", "text", "t0.IPOFabricItemCodeCombined"),
                Map("supplier_partner_ids", "text", "t0.SupplierPartnerIds"),
                Map("supplier_id", "integer", "t0.SupplierPartnerId"),
                Map("customer_id", "integer", "t0.CustomerPartnerId"),
                Map("partner_id", "integer", "t0.PartnerId"),
                Map("total_order_qty", "numeric", "t0.TotalOrderQty"),
                Map("actual_total_qty", "numeric", "t0.ActualTotalQty"),
                Map("total_surcharge", "numeric", "t0.TotalSurcharge"),
                Map("actual_total_amount", "numeric", "t0.ActualTotalAmount"),
                Map("total_pre_arrival", "numeric", "t0.TotalPreArrival"),
                Map("remark", "text", "t0.Remark"),
                Map("ipo_fabric_master_status", "integer", "t0.IPOFabricMasterStatus"),
                Map("combined_date", "timestamp", "t0.CombinedDate"),
                Map("combined_by_user_id", "integer", "t0.CombinedByUserId"),
                Map("acepted_date", "timestamp", "t0.AceptedDate"),
                Map("acepted_by_user_id", "integer", "t0.AceptedByUserId"),
                Map("approved_date", "timestamp", "t0.ApprovedDate"),
                Map("approved_by_user_id", "integer", "t0.ApprovedByUserId"),
                Map("rejected_date", "timestamp", "t0.RejectedDate"),
                Map("rejected_by_user_id", "integer", "t0.RejectedByUserId"),
                Map("rejected_ipo_reason", "text", "t0.RejectedIPOReason"),
                Map("remove_combined_date", "timestamp", "t0.RemoveCombinedDate"),
                Map("remove_combined_by_user_id", "integer", "t0.RemoveCombinedByUserId"),
                Map("remove_combine_reason", "text", "t0.RemoveCombineReason"),
                Map("sent_finance_by_user_id", "integer", "t0.SentFinanceByUserId"),
                Map("sent_finance_date", "timestamp", "t0.SentFinanceDate"),
                Map("sent_purch_manager_by_user_id", "integer", "t0.SentPurchManagerByUserId"),
                Map("sent_purch_manager_date", "timestamp", "t0.SentPurchManagerDate"),
                Map("commercial_reject_date", "timestamp", "t0.CommercialRejectDate"),
                Map("commercial_reject_reason", "text", "t0.CommercialRejectReason"),
                Map("commercial_reject_by_user_id", "integer", "t0.CommercialRejectByUserId"),
                Map("commercial_approved_date", "timestamp", "t0.CommercialApprovedDate"),
                Map("commercial_approved_by_user_id", "integer", "t0.CommercialApprovedByUserId"),
                Map("version", "integer", "t0.Version"),
                Map("clone_ok", "boolean", "t0.CloneOk"),
                Map("ipo_fabric_master_id_parent", "integer", "t0.IPOFabricMasterIdParent"),
                Map("currencies_id", "integer", "t0.CurrencyId"),
                Map("season_codes", "text", "t0.SeasonCodes"),
                Map("season_ids", "text", "t0.SeasonIds"),
                Map("drop_names", "text", "t0.DropNames"),
                Map("drop_ids", "text", "t0.DropIds"),
                Map("job_numbers", "text", "t0.JobNumbers"),
                Map("sent_pur_by_user_ids", "text", "t0.SentPurByUserIds"),
                Map("sent_pur_by_user_names", "text", "t0.SentPurByUserNames"),
                Map("process_type_ids", "text", "t0.ProcessTypeIds"),
                Map("process_type_names", "text", "t0.ProcessTypeNames"),
                Map("canceled_reasons", "text", "t0.CanceledReasons"),
                Map("po_type", "integer", "t0.POType"),
                Map("deposit", "boolean", "t0.Deposit"),
                Map("deposit_date", "timestamp", "t0.DepositDate"),
                Map("payment_date", "timestamp", "t0.PaymentDate"),
                Map("paid_percent", "numeric", "t0.PaidPercent"),
                Map("total_amount_before_vat", "numeric", "t0.TotalAmountBeforeVAT"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
