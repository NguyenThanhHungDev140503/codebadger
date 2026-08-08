using Domain.Common;
using Domain.Entities.Warehouse;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fabrics.Inspections
{
    public class FabricAppearanceTesting : BaseEntity<int>, IOrgEntity, IStockInQty
    {
        public int CompanyId { get; set; }
        /// <summary>
        /// IPOFabricMasterGroupInspectionRequestId
        /// </summary>
        public int FabricMasterGroupInspectionRequestId { get; set; }
        /// <summary>
        /// LotReceivingId
        /// </summary>
        public int FabricMasterGroupReceivingId { get; set; }
        public int MachineId { get; set; }
        public int? QCTesterUserId { get; set; }
        public int? TestingUserId { get; set; }
        public string? Note { get; set; }
        public string? RollNo { get; set; }
        public bool CutTestFromWH { get; set; }
        public bool ColorShade { get; set; }
        public bool Blanket { get; set; }
        public bool Swatch { get; set; }
        public decimal? TotalMeter { get; set; }
        public decimal? StartMeter { get; set; }
        public decimal? MiddleMeter { get; set; }
        public decimal? EndMeter { get; set; }
        public decimal? ActualQuantity { get; set; }
        public decimal? TotalWeight { get; set; }
        public string? PalletNo { get; set; }
        public int PalletId { get; set; }
        public bool SampleError { get; set; }
        public int? SkipNumber { get; set; }
        public bool FinalResult { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? TotalDefectMajor { get; set; }
        public int? TotalDefectMinor { get; set; }
        public decimal? AvgDefectMajor { get; set; }
        public decimal? AvgDefectMinor { get; set; }
        public decimal? PercentMajor { get; set; }
        public decimal? QuantityMajor { get; set; }
        public decimal? NetM { get; set; }
        public byte? ColorShadeType { get; set; }
        public string? IdQRCode { get; set; }
        public string? PathQRCode { get; set; }
        public int? WarehouseLocationId { get; set; }
        public DateTime? DateFillWarehouse { get; set; }
        public bool? Printed { get; set; }
        public string? ErrorWhenPrint { get; set; }
        public string? TransferReason { get; set; }
        public bool? ReturnRollToSupplier { get; set; }
        public bool? CommercialRollPass { get; set; }
        public string? CommercialRollPassRemark { get; set; }
        public byte? RollState { get; set; }
        public int? FabricRequestReturnSupplierId { get; set; }

        public WarehousePallet? Pallet { get; set; }
        public WarehouseLocation? BinLocation { get; set; }
        public decimal? StockInQty { get; set; }
        public decimal? StockOutQty { get; set; }
        public decimal? BalanceQty { get; set; }
    }

    public interface IStockInQty
    {
        decimal? StockInQty { get; set; }
        decimal? StockOutQty { get; set; }
        decimal? BalanceQty { get; set; }
    }
}
