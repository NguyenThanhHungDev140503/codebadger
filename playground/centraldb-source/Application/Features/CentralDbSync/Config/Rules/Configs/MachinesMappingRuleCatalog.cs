using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class MachinesMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Machines",
            sourceTable: "ERP.Configs.Machines",
            sourcePrimaryKey: "MachineId",
            targetTable: "machines",
            targetPrimaryKey: "machine_id",
            filterCompany: true,
            columns:
            [
                MapPk("machine_id", "integer", "t0.MachineId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("machine_area_id", "integer", "t0.MachineAreaId"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
