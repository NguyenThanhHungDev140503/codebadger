using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Costing;

public sealed class WastageMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Costing.Wastages.Details",
            sourceTable: "ERP.Costing.Wastages.Details",
            sourcePrimaryKey: "WastageDetailId",
            targetTable: "wastage",
            targetPrimaryKey: "wastage_id",
            columns:
            [
                MapPk("wastage_id", "integer", "t0.WastageDetailId"),
                Map("wastage_master_id", "integer", "t0.WastageId"),
                Map("from_number", "integer", "t0.FromNumber"),
                Map("to_number", "integer", "t0.ToNumber"),
                Map("shrinkage_rate", "numeric", "t0.ShrinkageRate"),
                Map("wastage_defect", "numeric", "t0.WastageDefect")
            ])
    ];
}
