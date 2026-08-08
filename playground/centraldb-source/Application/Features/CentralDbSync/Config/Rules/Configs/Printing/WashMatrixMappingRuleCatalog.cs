using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Printing;

public sealed class WashMatrixMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Printing.Price.Detail-to-wash_matrix",
            sourceTable: "ERP.Configs.Printing.Price.Detail",
            sourcePrimaryKey: "PrintingPriceDetailId",
            targetTable: "wash_matrix",
            targetPrimaryKey: "wash_matrix_id",
            readFilterSql: "EXISTS (SELECT 1 FROM [dbo].[ERP.Configs.Printing.Price] AS [h] WITH (READPAST) WHERE [h].[PrintingPriceId] = [t0].[PrintingPriceId] AND [h].[PrintingPriceType] = 3)",
            columns:
            [
                MapPk("wash_matrix_id", "integer", "t0.PrintingPriceDetailId"),
                Map("wash_header_id", "integer", "t0.PrintingPriceId"),
                Map("from_qty", "integer", "t0.FromQty"),
                Map("to_qty", "integer", "t0.ToQty"),
                Map("price", "numeric", "t0.Price"),
                Map("print_area_bands_id", "integer", "t0.SizeAreaId"),
                Map("style_categories_id", "integer", "t0.StyleCategoryId"),
                Map("remark", "text", "t0.Remark"),
                Map("stt", "integer", "t0.Stt"),
                Map("number_of_color", "integer", "t0.NumberOfColor")
            ])
    ];
}
