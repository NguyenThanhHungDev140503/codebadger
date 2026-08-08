using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Costing;

public sealed class RateConfigMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Costing.Configurations",
            sourceTable: "ERP.Costing.Configurations",
            sourcePrimaryKey: "CostingConfigurationId",
            targetTable: "rate_config",
            targetPrimaryKey: "rate_config_id",
            filterCompany: true,
            columns:
            [
                MapPk("rate_config_id", "integer", "t0.CostingConfigurationId"),
                Map("code", "text", "t0.Code"),
                Map("name", "text", "t0.Name"),
                Map("costing_type_id", "integer", "t0.CostingTypeId"),
                Map("currencies_id", "integer", "t0.CurrencyId"),
                Map("cmp_factor", "numeric", "t0.CmpFactor"),
                Map("testing_percent", "numeric", "t0.TestingPercent"),
                Map("sga_factor", "numeric", "t0.SgaFactor"),
                Map("number_of_thread", "integer", "t0.NumberOfThread"),
                Map("artwork_positions_id", "integer", "t0.AllPartPositionId"),
                Map("treatment_on_id", "integer", "t0.TreatmentOnId"),
                Map("version", "integer", "t0.Version"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("standard_qty", "numeric", "t0.StandardQty"),
                Map("fabric_type_main_id", "integer", "t0.FabricTypeMainId"),
                Map("fabric_type_rib_id", "integer", "t0.FabricTypeRibId"),
                Map("costing_rates_id", "integer", "t0.CostingRateId"),
                Map("apply_date", "timestamp", "t0.ApplyDate"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

