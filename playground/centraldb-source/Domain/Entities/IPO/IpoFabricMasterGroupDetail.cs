using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.CRM;
using Domain.Entities.Fabrics;
using Domain.Entities.Identity;
using Domain.Entities.Orders;

namespace Domain.Entities.IPO;

public class IpoFabricMasterGroupDetail : BaseOrgAuditEntity<int>
{
    #region Basic Properties
    public int IPOFabricMasterGroupId { get; set; }
    public int? IPOFabricItemId { get; set; }
    public int? StyleId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public byte? WorkType { get; set; }
    public int? SeasonId { get; set; }
    public int? CustomerPartnerId { get; set; }
    public int? SupplierPartnerId { get; set; }
    public string? IPOFabricItemCode { get; set; }
    public int? StyleFabricId { get; set; }
    public int? FabricSupplierId { get; set; }
    public string? FabricSupplierCode { get; set; }
    public string? FabricSupplierName { get; set; }
    public string? ColorExt { get; set; }
    public int? FabricTypeId { get; set; }
    public int? GroupFabricTypeId { get; set; }
    public byte? FabricColorOption { get; set; }
    public string? Pantone { get; set; }
    public decimal? CutWidth { get; set; }
    public int? UnitId { get; set; }
    public int? FabricColorId { get; set; }
    public int? StyleFabricLabDipId { get; set; }
    public int? Ref_StyleFabricLabDipId { get; set; }
    public int? ShadebandId { get; set; }
    public string? LDCode { get; set; }
    public string? LDShadeband { get; set; }
    public string? TreatmentTypes { get; set; }
    public int? FabricCostingSheetId { get; set; }
    public int? CostingSheetId { get; set; }
    public byte? CSShipMode { get; set; }
    public decimal? CSPrice { get; set; }
    public byte? ActualShipMode { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? StyleTotalFinalQty { get; set; }
    public int? StyleTechnicalRatingId { get; set; }
    public int? FabricRatingDetailId { get; set; }
    public decimal? RatingValue { get; set; }
    public decimal? Qty { get; set; }
    public decimal? Test { get; set; }
    public decimal? TotalQty { get; set; }
    public decimal? PreArrival { get; set; }
    public bool? BlanketTest { get; set; }
    public int? IPOReasonId { get; set; }
    public byte? IPOFabricStatus { get; set; }
    public int? DropId { get; set; }
    public short? ProcessTypeId { get; set; }
    public DateTime? SentPurDate { get; set; }
    public int? SentPurByUserId { get; set; }
    public string? IPOFabricMasterCode { get; set; }
    public bool? ShippingSample { get; set; }
    public string? TreatmentTypeIds { get; set; }
    public DateTime? ExpectStockInDate { get; set; }
    public DateTime? ConfirmExpectStockInDate { get; set; }
    public bool? Deposit { get; set; }
    public int? IPOCancellationId { get; set; }
    public string? ItemCode { get; set; }
    public DateTime? RevisedStockInDate { get; set; }
    public string? LDCode_OptionActual { get; set; }
    public DateTime? PurchaseOrderCreatedDate { get; set; }
    public string? RemarkOfMer { get; set; }
    public string? MerCancelRemark { get; set; }
    public DateTime? ShipmentDateMin { get; set; }
    public DateTime? ShipmentDateMax { get; set; }
    public DateTime? StartReceivedDate { get; set; }
    public DateTime? StockInDate { get; set; }
    public byte? CDStatus { get; set; }
    public int? LastReasonRevisedStockInDateId { get; set; }
    public DateTime? ShipmentInternalDateMax { get; set; }
    #endregion

    #region Navigation Properties
    [ForeignKey(nameof(IPOFabricMasterGroupId))]
    public required IpoFabricMasterGroup Group { get; set; }
    
    [ForeignKey(nameof(IPOFabricItemId))]
    public FabricItem? FabricItem { get; set; }
    
    [ForeignKey(nameof(StyleId))]
    public Style? Style { get; set; }
    
    [ForeignKey(nameof(PurchaseOrderId))]
    public PurchaseOrder? PurchaseOrder { get; set; }
    
    [ForeignKey(nameof(SeasonId))]
    public Season? Season { get; set; }
    
    [ForeignKey(nameof(CustomerPartnerId))]
    public Partner? CustomerPartner { get; set; }
    
    [ForeignKey(nameof(SupplierPartnerId))]
    public Partner? SupplierPartner { get; set; }
    
    [ForeignKey(nameof(FabricTypeId))]
    public FabricType? FabricType { get; set; }
    
    [ForeignKey(nameof(GroupFabricTypeId))]
    public GroupFabricType? GroupFabricType { get; set; }
    
    [ForeignKey(nameof(UnitId))]
    public Unit? Unit { get; set; }
    
    [ForeignKey(nameof(FabricColorId))]
    public FabricColor? FabricColor { get; set; }
    
    [ForeignKey(nameof(DropId))]
    public Drop? Drop { get; set; }
    
    [ForeignKey(nameof(ProcessTypeId))]
    public ProcessType? ProcessType{ get; set; }
    
    [ForeignKey(nameof(IPOReasonId))]
    public IpoReason? IpoReason { get; set; }
    
    [ForeignKey(nameof(SentPurByUserId))]
    public AppUser? SentPurByUser { get; set; }
    
    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }
    
    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
    #endregion
}