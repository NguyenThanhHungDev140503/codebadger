using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.Configs.Trims;

public sealed class TrimsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "ERP.Configs.Trims",
            sourceTable: "ERP.Configs.Trims",
            sourcePrimaryKey: "TrimId",
            targetTable: "trims",
            targetPrimaryKey: "trims_id",
            filterCompany: true,
            columns:
            [
                MapPk("trims_id", "integer", "t0.TrimId"),
                Map("code", "text", "t0.Code"),
                Map("units_id", "integer", "t0.UnitId"),
                Map("trim_kinds_id", "integer", "t0.KindOfTrimId"),
                Map("colours_id", "integer", "t0.ColorId"),
                Map("trim_types_id", "integer", "t0.TypeOfTrimId"),
                Map("remark", "text", "t0.Remark"),
                Map("trims_compositions_id", "integer", "t0.TrimCompositionId"),
                Map("supplier_id", "integer", "t0.PartnerId"),
                Map("is_all_partner", "boolean", "t0.IsAllPartner"),
                Map("length", "numeric", "t0.Length"),
                Map("width", "numeric", "t0.Width"),
                Map("height", "numeric", "t0.Height"),
                Map("is_activate", "boolean", "t0.IsActivate"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("version", "integer", "t0.Version"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("desc_detail", "text", "t0.DescDetail"),
                Map("weight_from", "numeric", "t0.WeightFrom"),
                Map("weight_to", "numeric", "t0.WeightTo"),
                Map("lx_wx_h_units_id", "integer", "t0.LxWxHUnitId"),
                Map("weight_units_id", "integer", "t0.WeightUnitId"),
                Map("thickness", "numeric", "t0.Thickness"),
                Map("thickness_units_id", "integer", "t0.ThicknessUnitId"),
                Map("photo", "text", "t0.Photo"),
                Map("dimension", "text", "t0.Dimension"),
                Map("photo_first", "text", "t0.PhotoFirst")
            ])
    ];
}

