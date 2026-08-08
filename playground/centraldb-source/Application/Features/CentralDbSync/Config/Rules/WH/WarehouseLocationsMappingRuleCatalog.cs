using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.WH;

public sealed class WarehouseLocationsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "WH.Warehouses.Locations",
            sourceTable: "WH.Warehouses.Locations",
            sourcePrimaryKey: "WarehouseLocationId",
            targetTable: "warehouses_locations",
            targetPrimaryKey: "warehouse_location_id",
            filterCompany: true,
            columns:
            [
                MapPk("warehouse_location_id", "integer", "t0.WarehouseLocationId"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("bin_code", "text", "t0.BinCode"),
                Map("bin_number", "integer", "t0.BinNumber"),
                Map("shelf", "integer", "t0.Shelf"),
                Map("rack", "text", "t0.Rack"),
                Map("aisle", "text", "t0.Aisle"),
                Map("warehouse_id", "integer", "t0.WarehouseId"),
                Map("zone", "text", "t0.Zone"),
                Map("description", "text", "t0.Description"),
                Map("remark", "text", "t0.Remark"),
                Map("activated", "boolean", "t0.Activated"),
                Map("qr_code_path", "text", "t0.QrCodePath"),
                Map("version", "integer", "t0.Version"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
