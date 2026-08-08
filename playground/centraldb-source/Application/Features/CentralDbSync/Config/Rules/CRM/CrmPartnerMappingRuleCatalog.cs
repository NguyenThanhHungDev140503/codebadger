using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.CRM;

public sealed class CrmPartnerMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "CRM.Partners-to-customer",
            sourceTable: "CRM.Partners",
            sourcePrimaryKey: "PartnerId",
            targetTable: "customer",
            targetPrimaryKey: "customer_id",
            filterCompany: true,
            activePredicate: [Eq("IsCustomer", true)],
            expectedSyncIntervalMinutes: 1,
            columns:
            [
                MapPk("customer_id", "integer", "t0.PartnerId"),
                Map("customer_code", "text", "t0.Code"),
                Map("name", "text", "t0.Name"),
                Map("payment_terms", "text", "t0.PaymentTerm"),
                Map("target_margin_pct", "numeric", "t0.CmpFactor"),
                Map("ga_factor", "numeric", "t0.SgaFactor"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ]),
        Create(
            ruleName: "CRM.Partners-to-supplier",
            sourceTable: "CRM.Partners",
            sourcePrimaryKey: "PartnerId",
            targetTable: "supplier",
            targetPrimaryKey: "supplier_id",
            filterCompany: true,
            activePredicate: [Eq("IsSupplier", true)],
            expectedSyncIntervalMinutes: 1,
            columns:
            [
                MapPk("supplier_id", "integer", "t0.PartnerId"),
                Map("supplier_code", "text", "t0.Code"),
                Map("name", "text", "t0.Name"),
                Map("wastage_pct", "numeric", "t0.WastagePercent"),
                Map("province", "text", "t0.Location"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
