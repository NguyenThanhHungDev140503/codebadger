using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Trims;

public sealed class TrimsCompositionsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Trims.Compositions",
            sourceTable: "ERP.Configs.Trims.Compositions",
            sourcePrimaryKey: "TrimCompositionId",
            targetTable: "trims_compositions",
            targetPrimaryKey: "trims_compositions_id",
            filterCompany: true,
            columns:
            [
                MapPk("trims_compositions_id", "integer", "t0.TrimCompositionId"),
                Map("code", "text", "t0.Code"),
                Map("name", "text", "t0.Name"),
                Map("is_activate", "boolean", "t0.IsActivate"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

