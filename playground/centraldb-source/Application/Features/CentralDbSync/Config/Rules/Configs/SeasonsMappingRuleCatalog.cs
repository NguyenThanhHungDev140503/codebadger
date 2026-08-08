using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class SeasonsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Seasons",
            sourceTable: "ERP.Configs.Seasons",
            sourcePrimaryKey: "SeasonId",
            targetTable: "seasons",
            targetPrimaryKey: "season_id",
            filterCompany: true,
            columns:
            [
                MapPk("season_id", "integer", "t0.SeasonId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("description", "text", "t0.Description"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
