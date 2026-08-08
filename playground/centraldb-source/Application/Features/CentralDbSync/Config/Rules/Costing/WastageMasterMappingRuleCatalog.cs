using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Costing;

public sealed class WastageMasterMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Costing.Wastages",
            sourceTable: "ERP.Costing.Wastages",
            sourcePrimaryKey: "WastageId",
            targetTable: "wastage_master",
            targetPrimaryKey: "wastage_master_id",
            filterCompany: true,
            columns:
            [
                MapPk("wastage_master_id", "integer", "t0.WastageId"),
                Map("type", "integer", "t0.Type"),
                Map("name", "text", "t0.Name"),
                Map("wastage_percentage", "numeric", "t0.WastagePercentage"),
                Map("shrinkage_rate_of_fabric", "numeric", "t0.ShrinkageRateOfFabric"),
                Map("kind_of_print", "integer", "t0.KindOfPrint"),
                Map("display_order", "integer", "t0.DisplayOrder"),
                Map("is_aop", "boolean", "t0.IsAOP"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

