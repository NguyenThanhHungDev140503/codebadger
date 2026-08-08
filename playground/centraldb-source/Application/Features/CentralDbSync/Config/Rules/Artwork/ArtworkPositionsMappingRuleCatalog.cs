using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Artwork;

public sealed class ArtworkPositionsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Artwork.Position",
            sourceTable: "ERP.Artwork.Position",
            sourcePrimaryKey: "ArtworkPositionId",
            targetTable: "artwork_positions",
            targetPrimaryKey: "artwork_position_id",
            filterCompany: true,
            columns:
            [
                MapPk("artwork_position_id", "integer", "t0.ArtworkPositionId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("is_active", "boolean", "t0.Activated"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("name_eng", "text", "t0.NameEng"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
