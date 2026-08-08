using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class GarmentKindsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.POM.GarmentKinds",
            sourceTable: "ERP.Configs.POM.GarmentKinds",
            sourcePrimaryKey: "GarmentKindId",
            targetTable: "garment_kinds",
            targetPrimaryKey: "garment_kind_id",
            filterCompany: true,
            columns:
            [
                MapPk("garment_kind_id", "integer", "t0.GarmentKindId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("remark", "text", "t0.Remark"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
