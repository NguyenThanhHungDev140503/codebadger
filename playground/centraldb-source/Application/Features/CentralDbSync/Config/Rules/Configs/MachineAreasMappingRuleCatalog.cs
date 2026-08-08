using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class MachineAreasMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Machines.Area",
            sourceTable: "ERP.Configs.Machines.Area",
            sourcePrimaryKey: "MachineAreaId",
            targetTable: "machine_areas",
            targetPrimaryKey: "machine_area_id",
            filterCompany: true,
            columns:
            [
                MapPk("machine_area_id", "integer", "t0.MachineAreaId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
