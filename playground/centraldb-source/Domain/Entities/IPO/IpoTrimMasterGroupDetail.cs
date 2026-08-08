using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.CRM;
using Domain.Entities.Identity;
using Domain.Entities.Orders;
using Domain.Entities.Trims;

namespace Domain.Entities.IPO;

public class IpoTrimMasterGroupDetail : BaseOrgAuditEntity<int>
{
    #region Properties

    public int IpoTrimMasterGroupId { get; set; }
    public int IpoTrimMasterId { get; set; }
    public int IpoTrimItemId { get; set; }
    public int? StyleId { get; set; }
    public int PurchaseOrderId { get; set; }

    public int? StyleCategoryId { get; set; }
    public int? StyleColorId { get; set; }
    public int? StyleTrimId { get; set; }
    public int? TrimSupplierId { get; set; }
    public int? Nominated { get; set; }
    public int? SeasonId { get; set; }
    public int? CustomerPartnerId { get; set; }
    public int? SupplierPartnerId { get; set; }
    public int? IpoReasonId { get; set; }
    public int? SentPurByUserId { get; set; }
    public int? KindOfTrimId { get; set; }
    public int? TypeOfTrimId { get; set; }
    public int? TrimCompositionId { get; set; }
    public int? ColorId { get; set; }
    public int? UnitId { get; set; }
    public int? SizeId { get; set; }
    public int? FabricTypeId { get; set; }
    public int? GroupFabricTypeId { get; set; }
    public int? TrimTechnicalRatingId { get; set; }
    public int? TrimId { get; set; }
    public int? CostingSheetId { get; set; }
    public int? TrimCostingSheetId { get; set; }
    public int? StyleTrimLabDipId { get; set; }
    public int? Ref_StyleFabricLabDipId { get; set; }
    public int? DimensionUnitIdOfTechnical { get; set; }
    public int? DropId { get; set; }
    public short? ProcessTypeId { get; set; }
    public int? LastReasonRevisedStockInDateId { get; set; }

    public string? IpoTrimItemCode { get; set; }
    public string? TrimSupplierCode { get; set; }
    public string? TrimSupplierName { get; set; }
    public string? ItemCode { get; set; }
    public string? IpoTrimMasterCode { get; set; }
    public string? Pantone { get; set; }
    public string? RemarkOfPur { get; set; }
    public string? RemarkOfMer { get; set; }
    public string? MerCancelRemark { get; set; }
    public string? DimensionOfTechnical { get; set; }
    public string? DimensionOfSupplier { get; set; }
    public string? DimensionOfTrim { get; set; }
    public string? LDCode { get; set; }
    public string? LDShadeband { get; set; }
    public string? SwatchApproval { get; set; }
    public string? LDApproval { get; set; }
    public string? ColorExt { get; set; }
    public string? FabricColorExt { get; set; }

    public decimal? Qty { get; set; }
    public decimal? OrderQty { get; set; }
    public decimal? QtyAfterRating { get; set; }
    public decimal? PreArrival { get; set; }
    public decimal? RatingValue { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? CSPrice { get; set; }
    public decimal? StyleTotalFinalQty { get; set; }
    public decimal? DimensionValueOfTechnical { get; set; }

    public bool? Logo { get; set; }
    public bool? DyeToMatch { get; set; }
    public bool? Swatch { get; set; }
    public bool? Labdip { get; set; }

    public byte? WorkType { get; set; }
    public byte? IPOTrimStatus { get; set; }
    public byte? CSShipMode { get; set; }
    public byte? ActualShipMode { get; set; }
    public byte? POType { get; set; }

    public DateTime? SentPurDate { get; set; }
    public DateTime? ExpectStockinDate { get; set; }
    public DateTime? ConfirmExpectStockInDate { get; set; }
    public DateTime? ShipDate { get; set; }
    public DateTime? RevisedStockInDate { get; set; }
    public DateTime? ShipmentDateMax { get; set; }
    public DateTime? ShipmentInternalDateMax { get; set; }

    #endregion
    
    #region Navigation Properties
    [ForeignKey(nameof(IpoTrimMasterGroupId))]
    public required IpoTrimMasterGroup Group { get; set; }

    [ForeignKey(nameof(StyleId))]
    public Style? Style { get; set; }

    [ForeignKey(nameof(KindOfTrimId))]
    public TrimsKinds? KindOfTrim { get; set; }

    [ForeignKey(nameof(TypeOfTrimId))]
    public TrimsTypes? TypeOfTrim { get; set; }

    [ForeignKey(nameof(TrimCompositionId))]
    public TrimComposition? TrimComposition { get; set; }

    [ForeignKey(nameof(ColorId))]
    public Color? Color { get; set; }

    [ForeignKey(nameof(DropId))]
    public Drop? Drop { get; set; }

    [ForeignKey(nameof(ProcessTypeId))]
    public ProcessType? ProcessType { get; set; }

    [ForeignKey(nameof(Nominated))]
    public TrimNomination? TrimNomination { get; set; }

    [ForeignKey(nameof(CustomerPartnerId))]
    public Partner? CustomerPartner { get; set; }

    [ForeignKey(nameof(SupplierPartnerId))]
    public Partner? SupplierPartner { get; set; }

    [ForeignKey(nameof(IpoReasonId))]
    public IpoReason? IpoReason { get; set; }

    [ForeignKey(nameof(SeasonId))]
    public Season? Season { get; set; }

    [ForeignKey(nameof(SentPurByUserId))]
    public AppUser? SentPurByUser { get; set; }

    [ForeignKey(nameof(FabricTypeId))]
    public FabricType? FabricType { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
    
    [ForeignKey(nameof(UnitId))]
    public Unit? Unit { get; set; }
    
    [ForeignKey(nameof(SizeId))]
    public Size? Size { get; set; }
    #endregion
}
