using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.CRM;
using Domain.Entities.Identity;
using Domain.Entities.Productions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Orders;

public class Style : BaseOrgAuditEntityVersion<int>
{
    public int? PurchaseOrderId { get; set; }
    public int? PartnerId { get; set; }
    public string? SkuNo { get; set; }
    public string? StyleNoInternal { get; set; }
    public string? SkuNoInternal { get; set; }
    public string? DetailInformation { get; set; }
    public string? StyleNoInternalOld { get; set; }
    public int? StyleRefId { get; set; }
    public string? StyleName { get; set; }
    public int? ColorId { get; set; }
    public int? SizeTypeId { get; set; }
    public int? Stt { get; set; }
    public decimal? Total { get; set; }
    public int? RandomId { get; set; }
    public int? StyleCategoryId { get; set; }
    public short? ProcessTypeId { get; set; }
    public decimal? TotalExtra { get; set; }
    public bool? TechnicalReceived { get; set; }
    public decimal? TargetPrice { get; set; }
    public string? Avatars { get; set; }
    public string? OrderCode { get; set; }
    public string? CFC { get; set; }
    public int? SeasonId { get; set; }
    public int? DropId { get; set; }
    public byte? WorkType { get; set; }
    public int? ApprovedUserId { get; set; }
    public bool? UnInspect { get; set; }
    public DateTime? ShipDate { get; set; }
    public bool? TechnicalReceivedPOM { get; set; }
    public DateTime? SentMockupTreatmentSummaryDate { get; set; }
    public bool? SentMockupTreatmentSummary { get; set; }
    public bool? EnableSentMockupTreatmentSummary { get; set; }
    public int? CountRequest { get; set; }
    public int? OriginalId { get; set; }
    public string? Reuse_StyleFabricIds { get; set; }
    public string? Reuse_StyleTrimIds { get; set; }
    public string? Reuse_CmpIds { get; set; }
    public string? Reuse_PomIds { get; set; }
    public string? Reuse_MockupTreatmentSummaryIds { get; set; }
    public int? Reuse_CostingSheetId { get; set; }
    public bool? Reuse_CopyRequest { get; set; }
    public bool? Reuse_ReuseAll { get; set; }
    public bool? Reuse_ReuseStyleNoInternal { get; set; }
    public string? Reuse_StyleFabricIds_Labdip { get; set; }
    public string? Reuse_StyleTrimIds_Labdip { get; set; }
    public bool? DoneFabricPO { get; set; }
    public bool? DoneTrimPO { get; set; }
    public byte? ReuseState { get; set; }
    public string? ReuseError { get; set; }
    public decimal? Amount { get; set; }
    public bool? Reuse_LockForRepeatStyleNo { get; set; }
    public string? Reuse_ForStyleNoInternal { get; set; }
    public DateTime? RemoveLockDate { get; set; }
    public int? RemoveLockByUserId { get; set; }
    public DateTime? ShipmentDateMax { get; set; }
    public DateTime? ShipmentDateMin { get; set; }
    public byte? BalanceTrimState { get; set; }
    public bool? MarkAsCmpProcess { get; set; }
    public bool? MarkAsRfidProcess { get; set; }
    public int? CuttingDocketId { get; set; }
    public string? ProcessDefault { get; set; }
    public string? ProcessCustom { get; set; }
    public int? ProcessUpdatedByUserId { get; set; }
    public DateTime? ShipmentInternalDateMax { get; set; }
    public int? StyleRatioId { get; set; }
    public DateTime? ProcessUpdatedDate { get; set; }
    public decimal? TotalStyleQty { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    [ForeignKey(nameof(ColorId))]
    public Color? Color { get; set; }

    [ForeignKey(nameof(StyleCategoryId))]
    public StyleCategory? StyleCategory { get; set; }

    [ForeignKey(nameof(ProcessTypeId))]
    public ProcessType? ProcessType { get; set; }

    [ForeignKey(nameof(SeasonId))]
    public Season? Season { get; set; }

    [ForeignKey(nameof(DropId))]
    public Drop? Drop { get; set; }

    [ForeignKey(nameof(ApprovedUserId))]
    public AppUser? ApprovedUser { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }

    public ICollection<StyleTechnicalPom> StyleTechnicalPoms { get; set; } = [];

    public ICollection<PoStyleSize> Sizes { get; set; } = [];

    /// <summary>
    ///  Master (WIP) theo StyleId -> Cutting Start Date. Shared-key LEFT join: Master.StyleId = Style.Id (col StyleId).
    ///  Nullable + Style là principal => EF sinh LEFT JOIN, style chưa có dòng Masters vẫn được trả về.
    /// </summary>
    public StyleMaster? Master { get; set; }

    /// <summary>
    /// CD approved cuối cùng của style. Join: CuttingDocket.Id = Style.CuttingDocketId.
    /// Dùng để lấy Approved Date (SentMerDate) và Sewing Timing (CMP.TotalSewing).
    /// </summary>
    [ForeignKey(nameof(CuttingDocketId))]
    public CuttingDocket? CuttingDocket { get; set; }
}