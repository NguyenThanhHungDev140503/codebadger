using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Printing;

public sealed class WashHeaderMappingRuleCatalog : ITableMappingRuleCatalog
{
    private const int DyewashPrintingPriceType = 3;

    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Printing.Price-to-wash_header",
            sourceTable: "ERP.Configs.Printing.Price",
            sourcePrimaryKey: "PrintingPriceId",
            targetTable: "wash_header",
            targetPrimaryKey: "wash_header_id",
            filterCompany: true,
            readFilter: [Eq("PrintingPriceType", DyewashPrintingPriceType)],
            columns:
            [
                MapPk("wash_header_id", "integer", "t0.PrintingPriceId"),
                Map("code", "text", "t0.Code"),
                Map("treatment_type_id", "integer", "t0.TreatmentTypeId"),
                Map("activated", "boolean", "t0.Activated"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("version", "integer", "t0.Version"),
                Map("price_expiry_date", "timestamp", "t0.PriceExpiryDate"),
                Map("print_outsource_group_id", "integer", "t0.PrintOutsourceGroupId"),
                Map("printing_price_type", "integer", "t0.PrintingPriceType"),
                Map("skip_approved_by_user_id", "integer", "t0.SkipApprovedByUserId"),
                Map("skip_approved_date", "timestamp", "t0.SkipApprovedDate"),
                Map("is_draft", "boolean", "t0.IsDraft"),
                Map("processing_request_id", "integer", "t0.ProcessingRequestId"),
                Map("last_approved_request_id", "integer", "t0.LastApprovedRequestId"),
                Map("processing_request_code", "text", "t0.ProcessingRequestCode"),
                Map("last_approved_request_code", "text", "t0.LastApprovedRequestCode"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

