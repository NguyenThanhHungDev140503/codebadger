using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Fabrics;

public sealed class FabricsSupplierRequestMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Fabrics.Suppliers.Requests",
            sourceTable: "ERP.Configs.Fabrics.Suppliers.Requests",
            sourcePrimaryKey: "FabricSupplierRequestId",
            targetTable: "fabrics_supplier_request",
            targetPrimaryKey: "fabrics_supplier_request_id",
            filterCompany: true,
            columns:
            [
                MapPk("fabrics_supplier_request_id", "integer", "t0.FabricSupplierRequestId"),
                Map("fabric_master_id", "integer", "t0.FabricSupplierId"),
                Map("fabrics_supplier_request_code", "text", "t0.FabricSupplierRequestCode"),
                Map("state", "integer", "t0.State"),
                Map("sent_to_finance_date", "timestamp", "t0.SentToFinanceDate"),
                Map("finance_approved_date", "timestamp", "t0.FinanceApprovedDate"),
                Map("finance_approved_by_user_id", "integer", "t0.FinanceApprovedByUserId"),
                Map("finance_rejected_date", "timestamp", "t0.FinanceRejectedDate"),
                Map("finance_rejected_by_user_id", "integer", "t0.FinanceRejectedByUserId"),
                Map("remark_of_finance", "text", "t0.RemarkOfFinance"),
                Map("finance_rejected_reason", "text", "t0.FinanceRejectedReason"),
                Map("remark_of_pur", "text", "t0.RemarkOfPur"),
                Map("columns_change", "text", "t0.ColumnsChange"),
                Map("version", "integer", "t0.Version"),
                Map("fabrics_id", "integer", "t0.FabricId"),
                Map("supplier_id", "integer", "t0.PartnerId"),
                Map("nationality_id", "integer", "t0.NationalityId"),
                Map("fabric_master_code", "text", "t0.FabricSupplierCode"),
                Map("name", "text", "t0.Name"),
                Map("item_code", "text", "t0.ItemCode"),
                Map("cut_width", "numeric", "t0.CutWidth"),
                Map("moc", "numeric", "t0.MOC"),
                Map("convert_rate", "numeric", "t0.ConvertRate"),
                Map("shrinkage", "numeric", "t0.Shrinkage"),
                // Map("lead_time_no_greige", "text", "t0.LeadTimeNoGreige"),
                // Map("lead_time_no_greige_type", "integer", "t0.LeadTimeNoGreigeType"),
                // Map("lead_time_with_greige_available", "integer", "t0.LeadTimeWithGreigeAvailable"),
                // Map("lead_time_with_greige_type", "integer", "t0.LeadTimeWithGreigeType"),
                Map("price_quoted_date", "timestamp", "t0.PriceQuotedDate"),
                Map("price_expiry_date", "timestamp", "t0.PriceExpiryDate"),
                Map("surcharge", "numeric", "t0.Surcharge"),
                Map("surcharge_currencies_id", "integer", "t0.SurchargeCurrencyId"),
                Map("local_fee_price", "numeric", "t0.LocalFeePrice"),
                Map("local_fee_currencies_id", "integer", "t0.LocalFeeCurrencyId"),
                Map("mold_fee_price", "numeric", "t0.MoldFeePrice"),
                Map("mold_fee_currencies_id", "integer", "t0.MoldFeeCurrencyId"),
                Map("vat_fee_price", "numeric", "t0.VATFeePrice"),
                Map("vat_fee_currencies_id", "integer", "t0.VATFeeCurrencyId"),
                Map("bank_fee_price", "numeric", "t0.BankFeePrice"),
                Map("bank_fee_currencies_id", "integer", "t0.BankFeeCurrencyId"),
                Map("fabric_other_fee_types_id", "integer", "t0.OtherFeeTypeId"),
                Map("other_fee_price", "numeric", "t0.OtherFeePrice"),
                Map("other_fee_currencies_id", "integer", "t0.OtherFeeCurrencyId"),
                Map("price_remark", "text", "t0.PriceRemark"),
                Map("activated", "boolean", "t0.Activated"),
                Map("po_styles_fabric_development_id", "integer", "t0.StyleFabricDevelopmentId"),
                Map("skip_approved_by_user_id", "integer", "t0.SkipApprovedByUserId"),
                Map("skip_approved_date", "timestamp", "t0.SkipApprovedDate"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

