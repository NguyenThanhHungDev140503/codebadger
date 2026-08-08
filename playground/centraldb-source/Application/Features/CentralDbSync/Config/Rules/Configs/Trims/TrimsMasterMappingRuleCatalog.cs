using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Trims;

public sealed class TrimsMasterMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Trims.Suppliers",
            sourceTable: "ERP.Configs.Trims.Suppliers",
            sourcePrimaryKey: "TrimSupplierId",
            targetTable: "trims_master",
            targetPrimaryKey: "trims_master_id",
            filterCompany: true,
            columns:
            [
                MapPk("trims_master_id", "integer", "t0.TrimSupplierId"),
                Map("name", "text", "t0.Name"),
                Map("supplier_id", "integer", "t0.PartnerId"),
                Map("trims_id", "integer", "t0.TrimId"),
                Map("nationality_id", "integer", "t0.NationalityId"),
                Map("nominated", "boolean", "t0.Nominated"),
                Map("moc", "numeric", "t0.MOC"),
                Map("price_quoted_date", "timestamp", "t0.PriceQuotedDate"),
                Map("price_expiry_date", "timestamp", "t0.PriceExpiryDate"),
                Map("surcharge", "numeric", "t0.Surcharge"),
                Map("surcharge_currencies_id", "integer", "t0.SurchargeCurrencyId"),
                Map("fabric_other_fee_types_id", "integer", "t0.OtherFeeTypeId"),
                Map("other_fee_price", "numeric", "t0.OtherFeePrice"),
                Map("other_fee_currencies_id", "integer", "t0.OtherFeeCurrencyId"),
                Map("price_remark", "text", "t0.PriceRemark"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("version", "integer", "t0.Version"),
                // Map("lead_time_with_greige_available", "integer", "t0.LeadTimeWithGreigeAvailable"),
                // Map("lead_time_with_greige_type", "integer", "t0.LeadTimeWithGreigeType"),
                Map("po_styles_trims_id", "integer", "t0.StyleTrimId"),
                Map("style_trim_development_id", "integer", "t0.StyleTrimDevelopmentId"),
                Map("moc_units_id", "integer", "t0.MOCUnitId"),
                Map("local_fee_price", "numeric", "t0.LocalFeePrice"),
                Map("local_fee_currencies_id", "integer", "t0.LocalFeeCurrencyId"),
                Map("mold_fee_price", "numeric", "t0.MoldFeePrice"),
                Map("mold_fee_currencies_id", "integer", "t0.MoldFeeCurrencyId"),
                Map("vat_fee_price", "numeric", "t0.VATFeePrice"),
                Map("vat_fee_currencies_id", "integer", "t0.VATFeeCurrencyId"),
                Map("bank_fee_price", "numeric", "t0.BankFeePrice"),
                Map("bank_fee_currencies_id", "integer", "t0.BankFeeCurrencyId"),
                Map("trim_code_by_supplier", "text", "t0.TrimCodeBySupplier"),
                Map("activated", "boolean", "t0.Activated"),
                Map("is_draft", "boolean", "t0.IsDraft"),
                Map("processing_request_id", "integer", "t0.ProcessingRequestId"),
                Map("processing_request_code", "text", "t0.ProcessingRequestCode"),
                Map("last_approved_request_id", "integer", "t0.LastApprovedRequestId"),
                Map("last_approved_request_code", "text", "t0.LastApprovedRequestCode"),
                Map("customer_id", "integer", "t0.CustomerId"),
                Map("is_all_customer", "boolean", "t0.IsAllCustomer"),
                Map("remark_of_pur", "text", "t0.RemarkOfPur"),
                Map("skip_approved_by_user_id", "integer", "t0.SkipApprovedByUserId"),
                Map("skip_approved_date", "timestamp", "t0.SkipApprovedDate"),
                Map("first_development_request_code", "text", "t0.FirstDevelopmentRequestCode"),
                Map("mark_as_logo", "boolean", "t0.MarkAsLogo"),
                Map("swatch_printed_date", "timestamp", "t0.SwatchPrintedDate"),
                Map("swatch_print_expire_date", "timestamp", "t0.SwatchPrintExpireDate")
            ])
    ];
}

