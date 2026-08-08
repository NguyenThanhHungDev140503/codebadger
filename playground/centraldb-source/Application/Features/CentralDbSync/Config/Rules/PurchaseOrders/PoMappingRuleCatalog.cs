using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.PurchaseOrders;

public sealed class PoMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.PurchaseOrders",
            sourceTable: "ERP.PurchaseOrders",
            sourcePrimaryKey: "PurchaseOrderId",
            targetTable: "po",
            targetPrimaryKey: "po_id",
            syncTier: "Hot",
            filterCompany: true,
            columns:
            [
                MapPk("po_id", "integer", "t0.PurchaseOrderId"),
                Map("code", "text", "t0.Code"),
                Map("cfc", "text", "t0.CFC"),
                Map("supplier_id", "integer", "t0.PartnerId"),
                Map("seasons_id", "integer", "t0.SeasonId"),
                Map("drops_id", "integer", "t0.DropId"),
                Map("work_type", "integer", "t0.WorkType"),
                Map("state", "integer", "t0.State"),
                Map("drop_ids", "text", "t0.DropIds"),
                Map("drop_codes", "text", "t0.DropCodes"),
                Map("approved_user_id", "integer", "t0.ApprovedUserId"),
                Map("style_categories_id", "integer", "t0.StyleCategoryId"),
                Map("assign_user_id", "integer", "t0.AssignUserId"),
                Map("approved_date", "timestamp", "t0.ApprovedDate"),
                Map("rejected_reason", "text", "t0.RejectedReason"),
                Map("rejected_date", "timestamp", "t0.RejectedDate"),
                Map("rejected_by_user_id", "integer", "t0.RejectedByUserId"),
                Map("receive_date", "timestamp", "t0.ReceiveDate"),
                Map("ship_date", "timestamp", "t0.ShipDate"),
                Map("collection", "text", "t0.Collection"),
                Map("description", "text", "t0.Description"),
                Map("techpack_file", "text", "t0.TechpackFile"),
                Map("has_techpack_file", "boolean", "t0.HasTechpackFile"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("sent_shipping_date", "timestamp", "t0.SentShippingDate"),
                Map("version", "integer", "t0.Version"),
                Map("mer_status", "integer", "t0.MerStatus"),
                Map("total_order_qty", "numeric", "t0.TotalOrderQty"),
                Map("job_number", "text", "t0.JobNumber"),
                Map("total_order_amount", "numeric", "t0.TotalOrderAmount"),
                Map("total_order_extra", "numeric", "t0.TotalOrderExtra"),
                Map("total_original_order_qty", "numeric", "t0.TotalOriginalOrderQty"),
                Map("total_sku", "integer", "t0.TotalSKU"),
                Map("role_type", "integer", "t0.RoleType"),
                Map("person_in_charge_user_id", "integer", "t0.PersonInChargeUserId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
            with { UseScalableBootstrap = true }
    ];
}

