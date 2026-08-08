using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.CMP;

public sealed class CmpGateMatrixMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.CMP.Templates",
            sourceTable: "ERP.Configs.CMP.Templates",
            sourcePrimaryKey: "CmpTemplateId",
            targetTable: "cmp_gate_matrix",
            targetPrimaryKey: "cmp_gate_matrix_id",
            filterCompany: true,
            columns:
            [
                MapPk("cmp_gate_matrix_id", "integer", "t0.CmpTemplateId"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("name", "text", "t0.Name"),
                Map("style_categories_id", "integer", "t0.StyleCategoryId"),
                Map("activated", "boolean", "t0.Activated"),
                Map("description", "text", "t0.Description"),
                Map("version", "integer", "t0.Version"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("total_cmp", "numeric", "t0.TotalCMP"),
                Map("total_cutting", "numeric", "t0.TotalCutting"),
                Map("total_numbering", "numeric", "t0.TotalNumbering"),
                Map("total_prepare", "numeric", "t0.TotalPrepare"),
                Map("total_sewing", "numeric", "t0.TotalSewing"),
                Map("total_end_line", "numeric", "t0.TotalEndLine"),
                Map("total_pressing", "numeric", "t0.TotalPressing"),
                Map("total_finishing", "numeric", "t0.TotalFinishing"),
                Map("total_poly_bag", "numeric", "t0.TotalPolyBag"),
                Map("total_operation", "numeric", "t0.TotalOperation"),
                Map("total_operator", "numeric", "t0.TotalOperator"),
                Map("total_rating_of_productivity", "numeric", "t0.TotalRatingOfProductivity"),
                Map("total_bartack", "numeric", "t0.TotalBartack"),
                Map("total_cmp_consumption", "numeric", "t0.TotalCMPConsumption"),
                Map("total_cutting_consumption", "numeric", "t0.TotalCuttingConsumption"),
                Map("total_numbering_consumption", "numeric", "t0.TotalNumberingConsumption"),
                Map("total_prepare_consumption", "numeric", "t0.TotalPrepareConsumption"),
                Map("total_sewing_consumption", "numeric", "t0.TotalSewingConsumption"),
                Map("total_end_line_consumption", "numeric", "t0.TotalEndLineConsumption"),
                Map("total_pressing_consumption", "numeric", "t0.TotalPressingConsumption"),
                Map("total_finishing_consumption", "numeric", "t0.TotalFinishingConsumption"),
                Map("total_poly_bag_consumption", "numeric", "t0.TotalPolyBagConsumption"),
                Map("total_bartack_consumption", "numeric", "t0.TotalBartackConsumption")
            ])
    ];
}

