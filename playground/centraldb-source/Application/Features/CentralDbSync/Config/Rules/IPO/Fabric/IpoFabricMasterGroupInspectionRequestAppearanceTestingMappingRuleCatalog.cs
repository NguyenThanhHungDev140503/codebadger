using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.IPO.Fabric;

public sealed class IpoFabricMasterGroupInspectionRequestAppearanceTestingMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName:           "ERP.IPO.Fabrics.Masters.Groups.InspectionRequests.AppearanceTestings",
            sourceTable:        "ERP.IPO.Fabrics.Masters.Groups.InspectionRequests.AppearanceTestings",
            sourcePrimaryKey:   "RollId",
            targetTable:        "ipo_fabric_group_insp_request_appearance_testings",
            targetPrimaryKey:   "ipo_fabric_group_insp_request_appearance_testing_id",
            syncTier:           "Hot",
            filterCompany:      true,
            columns:
            [
                MapPk("ipo_fabric_group_insp_request_appearance_testing_id", "integer", "t0.RollId"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("ipo_fabric_master_group_inspection_request_id", "integer", "t0.IPOFabricMasterGroupInspectionRequestId"),
                Map("ipo_fabric_master_group_id", "integer", "t0.IPOFabricMasterGroupId"),
                Map("ipo_fabric_master_group_receiving_id", "integer", "t0.LotReceivingId"),
                Map("machine_id", "integer", "t0.MachineId"),
                Map("qc_tester_user_id", "integer", "t0.QCTesterUserId"),
                Map("testing_user_id", "integer", "t0.TestingUserId"),
                Map("note", "text", "t0.Note"),
                Map("roll_no", "text", "t0.RollNo"),
                Map("cut_test_from_wh", "boolean", "t0.CutTestFromWH"),
                Map("color_shade", "boolean", "t0.ColorShade"),
                Map("blanket", "boolean", "t0.Blanket"),
                Map("swatch", "boolean", "t0.Swatch"),
                Map("total_meter", "numeric", "t0.TotalMeter"),
                Map("start_meter", "numeric", "t0.StartMeter"),
                Map("middle_meter", "numeric", "t0.MiddleMeter"),
                Map("end_meter", "numeric", "t0.EndMeter"),
                Map("actual_quantity", "numeric", "t0.ActualQuantity"),
                Map("total_weight", "numeric", "t0.TotalWeight"),
                Map("pallet_no", "text", "t0.PalletNo"),
                Map("sample_error", "boolean", "t0.SampleError"),
                Map("skip_number", "integer", "t0.SkipNumber"),
                Map("updated_date", "timestamp", "t0.UpdatedDate"),
                Map("total_defect_major", "integer", "t0.TotalDefectMajor"),
                Map("total_defect_minor", "integer", "t0.TotalDefectMinor"),
                Map("avg_defect_major", "numeric", "t0.AvgDefectMajor"),
                Map("avg_defect_minor", "numeric", "t0.AvgDefectMinor"),
                Map("percent_major", "numeric", "t0.PercentMajor"),
                Map("quantity_major", "numeric", "t0.QuantityMajor"),
                Map("net_m", "numeric", "t0.NetM"),
                Map("return_roll_to_supplier", "boolean", "t0.ReturnRollToSupplier"),
                Map("commercial_roll_pass", "boolean", "t0.CommercialRollPass"),
                Map("stock_adjust", "numeric", "t0.StockAdjust"),
                Map("return_qty", "numeric", "t0.ReturnQty"),
                Map("commercial_roll_pass_remark", "text", "t0.CommercialRollPassRemark"),
                Map("roll_state", "integer", "t0.RollState"),
                Map("fabric_request_return_supplier_id", "integer", "t0.FabricRequestReturnSupplierId"),
                Map("stock_in_qty", "numeric", "t0.StockInQty"),
                Map("stock_out_qty", "numeric", "t0.StockOutQty"),
                Map("balance_qty", "numeric", "t0.BalanceQty"),
                Map("end_of_roll", "numeric", "t0.EndOfRoll"),
                Map("actual_stock_adjust", "numeric", "t0.ActualStockAdjust"),
                Map("final_result", "boolean", "t0.FinalResult"),
                Map("date_fill_warehouse", "timestamp", "t0.DateFillWarehouse"),
                Map("match_with_shipping_sample", "integer", "t0.MatchWithShippingSample"),
                Map("match_with_ld", "integer", "t0.MatchWithLD"),
                Map("shading_color_top_to_end", "integer", "t0.ShadingColorTopToEnd"),
                Map("color_shade_roll_result", "integer", "t0.ColorShadeRollResult"),
                Map("all_final_result", "boolean", "t0.AllFinalResult"),
                Map("is_actual_cut", "boolean", "t0.IsActualCut"),
                Map("sample_error_qty", "integer", "t0.SampleErrorQty"),
                Map("combined_appearance_remark", "text", "t0.CombinedAppearanceRemark"),
                Map("warehouse_pallet_id", "integer", "t0.PalletId"),
                Map("warehouse_location_id", "integer", "t0.WarehouseLocationId"),
                Map("transfer_reason", "text", "t0.TransferReason"),
                Map("start_checking_date", "timestamp", "t0.StartCheckingDate"),
                Map("printed", "boolean", "t0.Printed"),
                Map("id_qr_code", "text", "t0.IdQRCode"),
                Map("path_qr_code", "text", "t0.PathQRCode"),
                Map("color_shade_type", "integer", "t0.ColorShadeType")
            ])
    ];
}
