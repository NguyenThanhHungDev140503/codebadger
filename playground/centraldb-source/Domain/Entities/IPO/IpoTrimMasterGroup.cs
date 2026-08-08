using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Entities.Configs;
using Domain.Entities.CRM;
using Domain.Entities.Trims;

namespace Domain.Entities.IPO
{
    public class IpoTrimMasterGroup : BaseEntity<int>, IOrgEntity
    {
        #region Properties
        public int IpoTrimMasterId { get; set; }
        public int CompanyId { get; set; }
        public string? IpoTrimMasterGroupCode { get; set; }
        public string? IpoTrimItemCodeCombined { get; set; }
        public string? Pantone { get; set; }
        public string? LDCode { get; set; }
        public string? LDShadeband { get; set; }
        public int? SupplierPartnerId { get; set; }
        public int? CustomerPartnerId { get; set; }
        public int? TrimSupplierId { get; set; }
        public int? SizeId { get; set; }
        public string? DimensionOfTechnical { get; set; }
        public string? DimensionOfTrim { get; set; }
        public bool? Logo { get; set; }
        public string? TrimSupplierCode { get; set; }
        public string? TrimSupplierName { get; set; }
        public int? UnitId { get; set; }
        public bool? Received { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public bool? PhysicalInspection { get; set; }
        public bool? MetaDetection { get; set; }
        public bool? TestingForStockin { get; set; }
        public string? MerRemark { get; set; }
        public string? PurRemark { get; set; }
        public int? TrimInspectionMatrixId { get; set; }
        public decimal? PreArrival { get; set; }
        public DateTime? ConfirmExpectStockInDate { get; set; }
        public DateTime? ETATrimReceivedDate { get; set; }
        public decimal? TotalOrderQty { get; set; }
        public decimal? Inventory { get; set; }
        public decimal? ActualOrderQty { get; set; }
        public double? ActualUnitPrice { get; set; }
        public decimal? ActualAmount { get; set; }
        public decimal? Surcharge { get; set; }
        public decimal? AvgUnitPrice { get; set; }
        public DateTime? MinExpectStockInDate { get; set; }
        public DateTime? MinShipDate { get; set; }
        public decimal? Test { get; set; }
        public decimal? PhysicalInspectionSampleSize { get; set; }
        public decimal? MetaDetectionSampleSize { get; set; }
        public decimal? TestingForStockinSampleSize { get; set; }
        public byte? WorkType { get; set; }
        public int? PhysicalAqlDetailId { get; set; }
        public int? PhysicalSampleSizeDetailId { get; set; }
        public decimal? PhysicalStandard { get; set; }
        public int? MetalAqlDetailId { get; set; }
        public int? MetalSampleSizeDetailId { get; set; }
        public decimal? MetalStandard { get; set; }
        public int? TestingAqlDetailId { get; set; }
        public int? TestingSampleSizeDetailId { get; set; }
        public decimal? TestingStandard { get; set; }
        public DateTime? ActualReceivedDate { get; set; }
        public decimal? RawActualOrderQty { get; set; }
        public decimal? BuyQty { get; set; }
        public string? ItemCode { get; set; }
        public string? SeasonCodes { get; set; }
        public string? DropCodes { get; set; }
        public string? ProcessTypeNames { get; set; }
        public string? SeasonIds { get; set; }
        public string? DropIds { get; set; }
        public string? ProcessTypeIds { get; set; }
        public int? ReceivedUserId { get; set; }
        public DateTime? RevisedStockInDate { get; set; }
        public string? CommercialRemark { get; set; }
        public string? ColorExts { get; set; }
        public string? StyleInternals { get; set; }
        public int? InspPercentageId { get; set; }
        public int? InspPercentageDetailId { get; set; }
        public int? PhysicalInspectionSampleSize2 { get; set; }
        public int? MetaDetectionSampleSize2 { get; set; }
        public int? TestingForStockinSampleSize2 { get; set; }
        public string? NominatedIds { get; set; }
        public string? NominatedNames { get; set; }
        public DateTime? SendShippingSampleToPurDate { get; set; }
        public DateTime? PurReceivedShippingSampleDate { get; set; }
        public int? ParentId { get; set; }
        public int? ParentRootId { get; set; }
        public decimal? DeliveryQty { get; set; }
        public decimal? ReceivedQty { get; set; }
        public decimal? AdjustQty { get; set; }
        public decimal? ActualReceivedQty { get; set; }
        public decimal? FinalReceivedQty { get; set; }
        public decimal? FinalPercentReceived { get; set; }
        public string? FabricColorExts { get; set; }
        public decimal? MaxRating { get; set; }
        public decimal? AvgRating { get; set; }
        public int? ReturnRequestId { get; set; }
        public string? ReturnRequestCode { get; set; }
        public byte? POType { get; set; }
        public string? StyleNames { get; set; }
        public string? StyleIds { get; set; }
        public decimal? StockinQuantity { get; set; }
        public decimal? StockoutQuantity { get; set; }
        public decimal? StockAdjust { get; set; }
        public byte? POStatus { get; set; }
        public double? RawActualAmount { get; set; }
        public DateTime? LastStockinDate { get; set; }
        public DateTime? LastPhysicalInspectionCheckedDate { get; set; }
        public DateTime? LastMetaDetectionCheckedDate { get; set; }
        public DateTime? LastTestingForStockinCheckedDate { get; set; }
        public int? LastReasonRevisedStockInDateId { get; set; }
        public DateTime? ShipmentDateMax { get; set; }
        public DateTime? ShipmentInternalDateMax { get; set; }
        public string? SkuNoInternals { get; set; }
        public string? StyleColorNameCodes { get; set; }
        public string? StyleNoInternals { get; set; }
        public DateTime? FirstStockinDate { get; set; }
        public bool? IsPaid { get; set; }
        public DateTime? PaymentUpdatedDate { get; set; }
        public int? PaymentUpdatedUserId { get; set; }

        #endregion
        
        #region Navigation Properties
        [ForeignKey(nameof(IpoTrimMasterId))]
        public IpoTrimMaster? POTrimMaster { get; set; }

        [ForeignKey(nameof(TrimSupplierId))]
        public TrimSupplier? TrimSupplier { get; set; }

        [ForeignKey(nameof(CustomerPartnerId))]
        public Partner? Customer { get; set; }

        [ForeignKey(nameof(SizeId))]
        public Size? Size { get; set; }

        [ForeignKey(nameof(UnitId))]
        public Unit? Unit { get; set; }

        public ICollection<IpoTrimMasterGroupDetail> Details { get; set; } = [];
        
        #endregion
    }
}