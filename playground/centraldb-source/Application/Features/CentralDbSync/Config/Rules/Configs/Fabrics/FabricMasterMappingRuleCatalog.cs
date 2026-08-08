using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Fabrics;

public sealed class FabricMasterMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Fabrics.Suppliers",
            sourceTable: "ERP.Configs.Fabrics.Suppliers",
            sourcePrimaryKey: "FabricSupplierId",
            targetTable: "fabric_master",
            targetPrimaryKey: "fabric_master_id",
            filterCompany: true,
            columns:
            [
                MapPk("fabric_master_id", "integer", "t0.FabricSupplierId"),
                Map("fabrics_id", "integer", "t0.FabricId"),
                Map("supplier_id", "integer", "t0.PartnerId"),
                Map("nationality_id", "integer", "t0.NationalityId"),
                Map("convert_rate", "numeric", "t0.ConvertRate"),
                Map("item_code", "text", "t0.ItemCode"),
                Map("cut_width", "numeric", "t0.CutWidth"),
                Map("shrinkage", "numeric", "t0.Shrinkage"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("version", "integer", "t0.Version"),
                Map("surcharge", "numeric", "t0.Surcharge"),
                Map("price_remark", "text", "t0.PriceRemark"),
                Map("fabric_other_fee_types_id", "integer", "t0.OtherFeeTypeId"),
                Map("price_quoted_date", "timestamp", "t0.PriceQuotedDate"),
                Map("other_fee_price", "numeric", "t0.OtherFeePrice"),
                Map("price_expiry_date", "timestamp", "t0.PriceExpiryDate"),
                Map("surcharge_currencies_id", "integer", "t0.SurchargeCurrencyId"),
                Map("other_fee_currencies_id", "integer", "t0.OtherFeeCurrencyId"),
                Map("name", "text", "t0.Name"),
                // Map("lead_time_with_greige_available", "integer", "t0.LeadTimeWithGreigeAvailable"),
                // Map("lead_time_with_greige_type", "integer", "t0.LeadTimeWithGreigeType"),
                // Map("lead_time_no_greige", "integer", "t0.LeadTimeNoGreige"),
                // Map("lead_time_no_greige_type", "integer", "t0.LeadTimeNoGreigeType"),
                Map("po_styles_fabric_development_id", "integer", "t0.StyleFabricDevelopmentId"),
                Map("local_fee_price", "numeric", "t0.LocalFeePrice"),
                Map("local_fee_currencies_id", "integer", "t0.LocalFeeCurrencyId"),
                Map("mold_fee_price", "numeric", "t0.MoldFeePrice"),
                Map("mold_fee_currencies_id", "integer", "t0.MoldFeeCurrencyId"),
                Map("vat_fee_price", "numeric", "t0.VATFeePrice"),
                Map("vat_fee_currencies_id", "integer", "t0.VATFeeCurrencyId"),
                Map("bank_fee_price", "numeric", "t0.BankFeePrice"),
                Map("bank_fee_currencies_id", "integer", "t0.BankFeeCurrencyId"),
                Map("fabric_master_code", "text", "t0.FabricSupplierCode"),
                Map("activated", "boolean", "t0.Activated"),
                Map("is_draft", "boolean", "t0.IsDraft"),
                Map("processing_request_id", "integer", "t0.ProcessingRequestId"),
                Map("last_approved_request_id", "integer", "t0.LastApprovedRequestId"),
                Map("processing_request_code", "text", "t0.ProcessingRequestCode"),
                Map("last_approved_request_code", "text", "t0.LastApprovedRequestCode"),
                Map("remark_of_pur", "text", "t0.RemarkOfPur"),
                Map("skip_approved_by_user_id", "integer", "t0.SkipApprovedByUserId"),
                Map("skip_approved_date", "timestamp", "t0.SkipApprovedDate"),
                Map("swatch_printed_date", "timestamp", "t0.SwatchPrintedDate"),
                Map("swatch_print_expire_date", "timestamp", "t0.SwatchPrintExpireDate"),
                Map("greige_generation_id", "integer", "t0.GreigeGenerationId"),
                Map("yarn_generation_id", "integer", "t0.YarnGenerationId"),
                Map("greige_generation_code", "text", "t0.GreigeGenerationCode"),
                Map("yarn_generation_code", "text", "t0.YarnGenerationCode"),
                Map("greige_generation_name", "text", "t0.GreigeGenerationName"),
                Map("yarn_generation_name", "text", "t0.YarnGenerationName")
            ])
    ];
}

