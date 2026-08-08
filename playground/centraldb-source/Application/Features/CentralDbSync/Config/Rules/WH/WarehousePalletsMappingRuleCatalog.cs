using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.WH;

public sealed class WarehousePalletsMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "WH.Warehouses.Pallets",
            sourceTable: "WH.Warehouses.Pallets",
            sourcePrimaryKey: "PalletId",
            targetTable: "warehouses_pallets",
            targetPrimaryKey: "warehouse_pallet_id",
            filterCompany: true,
            columns:
            [
                MapPk("warehouse_pallet_id", "integer", "t0.PalletId"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("warehouse_pallet_name", "text", "t0.PalletName"),
                Map("activated", "boolean", "t0.Activated"),
                Map("remark", "text", "t0.Remark"),
                Map("version", "integer", "t0.Version"),
                Map("qr_code_path", "text", "t0.QrCodePath"),
                Map("warehouse_location_id", "integer", "t0.WarehouseLocationId"),
                Map("total_rolls", "integer", "t0.TotalRolls"),
                Map("is_empty", "boolean", "t0.IsEmpty"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}
