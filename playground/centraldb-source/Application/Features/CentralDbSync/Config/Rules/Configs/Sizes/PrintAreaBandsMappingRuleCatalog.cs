using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Sizes;

public sealed class PrintAreaBandsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Sizes.Areas",
            sourceTable: "ERP.Configs.Sizes.Areas",
            sourcePrimaryKey: "SizeAreaId",
            targetTable: "print_area_bands",
            targetPrimaryKey: "print_area_bands_id",
            filterCompany: true,
            columns:
            [
                MapPk("print_area_bands_id", "integer", "t0.SizeAreaId"),
                Map("size_id", "integer", "t0.SizeId"),
                Map("area", "text", "t0.Area"),
                Map("from_qty", "integer", "t0.FromQty"),
                Map("to_qty", "integer", "t0.ToQty"),
                Map("display_order", "integer", "t0.DisplayOrder"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("version", "integer", "t0.Version"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

