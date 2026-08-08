using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.PurchaseOrders;

public sealed class PoStylesFabricDevelopmentMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.PurchaseOrders.Styles.Fabrics.Developments",
            sourceTable: "ERP.PurchaseOrders.Styles.Fabrics.Developments",
            sourcePrimaryKey: "StyleFabricDevelopmentId",
            targetTable: "po_styles_fabric_development",
            targetPrimaryKey: "po_styles_fabric_development_id",
            syncTier: "Hot",
            filterCompany: true,
            columns:
            [
                MapPk("po_styles_fabric_development_id", "integer", "t0.StyleFabricDevelopmentId"),
                Map("request_code", "text", "t0.RequestCode"),
                Map("po_style_fabrics_id", "integer", "t0.StyleFabricId"),
                Map("po_styles_id", "integer", "t0.StyleId"),
                Map("po_id", "integer", "t0.PurchaseOrderId"),
                Map("sent_pur_date", "timestamp", "t0.SentPurDate"),
                Map("sent_to_pur_by_user_id", "integer", "t0.SentToPurByUserId"),
                Map("fabric_master_id", "integer", "t0.FabricSupplierId"),
                Map("fabrics_id", "integer", "t0.FabricId"),
                Map("supplier_id", "integer", "t0.PartnerId"),
                Map("approved", "boolean", "t0.Approved"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("expect_receive_date", "timestamp", "t0.ExpectReceiveDate"),
                Map("receive_date_of_pur", "timestamp", "t0.ReceiveDateOfPur"),
                Map("state", "integer", "t0.State"),
                Map("style_fabric_random_id", "integer", "t0.StyleFabricRandomId"),
                Map("stt", "integer", "t0.Stt"),
                Map("pur_sent_mer_date", "timestamp", "t0.PurSentMerDate"),
                Map("sent_to_mer_by_user_id", "integer", "t0.SentToMerByUserId"),
                Map("sent_customer_date", "timestamp", "t0.SentCustomerDate"),
                Map("customer_note", "text", "t0.CustomerNote"),
                Map("customer_status", "integer", "t0.CustomerStatus"),
                Map("customer_approved_date", "timestamp", "t0.CustomerApprovedDate"),
                Map("name", "text", "t0.Name"),
                Map("color_ext", "text", "t0.ColorExt"),
                Map("pantone", "text", "t0.Pantone"),
                Map("fabric_types_id", "integer", "t0.FabricTypeId"),
                Map("sent_again", "boolean", "t0.SentAgain"),
                Map("version", "integer", "t0.Version"),
                Map("random_id", "integer", "t0.RandomId"),
                Map("recommend_fabric_master_id", "integer", "t0.RecommendFabricSupplierId"),
                Map("original_id", "integer", "t0.OriginalId"),
                Map("cut_width_ext", "text", "t0.CutWidthExt"),
                Map("fabric_color_option", "integer", "t0.FabricColorOption"),
                Map("fabrics_supplier_request_id", "integer", "t0.FabricSupplierRequestId"),
                Map("pur_rejected_reason", "text", "t0.PurRejectedReason"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("remark_of_mer", "text", "t0.RemarkOfMer"),
                Map("remark_of_pur", "text", "t0.RemarkOfPur"),
                Map("received_by_user_id", "integer", "t0.ReceivedByUserId"),
                Map("rejected_by_user_id", "integer", "t0.RejectedByUserId"),
                Map("rejected_date", "timestamp", "t0.RejectedDate"),
                Map("customer_rejected_date", "timestamp", "t0.CustomerRejectedDate"),
                Map("customer_canceled_date", "timestamp", "t0.CustomerCanceledDate"),
                Map("customer_approved_by_user_id", "integer", "t0.CustomerApprovedByUserId"),
                Map("customer_canceled_by_user_id", "integer", "t0.CustomerCanceledByUserId"),
                Map("customer_rejected_by_user_id", "integer", "t0.CustomerRejectedByUserId")
            ])
            with { UseScalableBootstrap = true }
    ];
}

