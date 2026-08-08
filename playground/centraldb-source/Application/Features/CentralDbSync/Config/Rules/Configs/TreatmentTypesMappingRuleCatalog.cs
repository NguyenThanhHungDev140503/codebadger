using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Config.Rules;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs;

public sealed class TreatmentTypesMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.TreatmentTypes",
            sourceTable: "ERP.Configs.TreatmentTypes",
            sourcePrimaryKey: "TreatmentTypeId",
            targetTable: "treatment_types",
            targetPrimaryKey: "treatment_type_id",
            filterCompany: true,
            columns:
            [
                MapPk("treatment_type_id", "integer", "t0.TreatmentTypeId"),
                Map("name", "text", "t0.Name"),
                Map("code", "text", "t0.Code"),
                Map("is_active", "boolean", "t0.Activated"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
