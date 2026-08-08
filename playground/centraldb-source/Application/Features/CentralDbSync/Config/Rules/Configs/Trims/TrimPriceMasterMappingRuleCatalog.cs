using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Trims;

public sealed class TrimPriceMasterMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Trims.Suppliers.Details",
            sourceTable: "ERP.Configs.Trims.Suppliers.Details",
            sourcePrimaryKey: "TrimSupplierDetailId",
            targetTable: "trim_price_master",
            targetPrimaryKey: "trim_price_master_id",
            filterCompany: true,
            columns:
            [
                MapPk("trim_price_master_id", "integer", "t0.TrimSupplierDetailId"),
                Map("trims_master_id", "integer", "t0.TrimSupplierId"),
                Map("moq_from", "numeric", "t0.MOQFrom"),
                Map("moq_to", "numeric", "t0.MOQTo"),
                Map("units_id", "integer", "t0.UnitId"),
                Map("sample_price", "numeric", "t0.SamplePrice"),
                Map("sample_currencies_id", "integer", "t0.SampleCurrencyId"),
                Map("bulk_price", "numeric", "t0.BulkPrice"),
                Map("bulk_currencies_id", "integer", "t0.BulkCurrencyId"),
                Map("version", "integer", "t0.Version"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("shipping_type", "integer", "t0.ShippingType"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("stt", "integer", "t0.Stt")
            ])
    ];
}

