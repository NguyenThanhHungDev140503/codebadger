using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Sizes;

public sealed class SizesMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Sizes",
            sourceTable: "ERP.Configs.Sizes",
            sourcePrimaryKey: "SizeId",
            targetTable: "sizes",
            targetPrimaryKey: "size_id",
            filterCompany: true,
            columns:
            [
                MapPk("size_id", "integer", "t0.SizeId"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("description", "text", "t0.Description"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("version", "integer", "t0.Version"),
                Map("size_type_id", "integer", "t0.SizeTypeId"),
                Map("stt", "integer", "t0.Stt")
            ])
    ];
}
