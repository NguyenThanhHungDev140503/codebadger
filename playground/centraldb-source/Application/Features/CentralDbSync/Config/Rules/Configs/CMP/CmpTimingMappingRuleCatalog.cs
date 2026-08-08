using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.CMP;

public sealed class CmpTimingMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.CMP.Operations.Timings",
            sourceTable: "ERP.Configs.CMP.Operations.Timings",
            sourcePrimaryKey: "OperationTimingId",
            targetTable: "cmp_timing",
            targetPrimaryKey: "cmp_timing_id",
            filterCompany: true,
            columns:
            [
                MapPk("cmp_timing_id", "integer", "t0.OperationTimingId"),
                Map("code", "text", "t0.Code"),
                Map("name", "text", "t0.Name"),
                Map("frequency", "numeric", "t0.Frequency"),
                Map("qty_of_timing", "numeric", "t0.QtyOfTiming"),
                Map("consumption", "numeric", "t0.Consumption"),
                Map("remark", "text", "t0.Remark"),
                Map("cmp_operations_id", "integer", "t0.CmpOperationId"),
                Map("cmp_feature_id", "integer", "t0.CmpFeatureId"),
                Map("machines_id", "integer", "t0.MachineId"),
                Map("activated", "boolean", "t0.Activated"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("version", "integer", "t0.Version"),
                Map("cmp_product_group_id", "integer", "t0.CmpProductGroupId")
            ])
    ];
}

